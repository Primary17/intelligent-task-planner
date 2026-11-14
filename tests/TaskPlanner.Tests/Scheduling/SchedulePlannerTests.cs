using TaskPlanner.Application.Scheduling;
using TaskPlanner.Domain.Entities;
using TaskPlanner.Domain.Enums;

namespace TaskPlanner.Tests.Scheduling;

public sealed class SchedulePlannerTests
{
    private static readonly TimeZoneInfo Utc = TimeZoneInfo.Utc;
    private static readonly DateTime WeekStart = new(2026, 11, 9, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime WeekEnd = WeekStart.AddDays(7);

    private readonly SchedulePlanner _planner = new();

    [Fact]
    public void EarlierDeadline_OutranksHigherPriority()
    {
        var early = Task(deadline: WeekStart.AddDays(1), priority: TaskPriority.Low, minutes: 60);
        var later = Task(deadline: WeekStart.AddDays(4), priority: TaskPriority.Critical, minutes: 60);
        var window = Window(DayOfWeek.Monday, 17, 18);

        var plan = Plan(new[] { later, early }, new[] { window });

        var session = Assert.Single(plan.Sessions);
        Assert.Equal(early.Id, session.StudyTaskId);
        Assert.Equal(WeekStart.AddHours(17), session.StartAtUtc);
        Assert.DoesNotContain(plan.Unscheduled, item => item.StudyTaskId == early.Id);
        Assert.Contains(plan.Unscheduled, item => item.StudyTaskId == later.Id && item.ScheduledMinutes == 0);
    }

    [Fact]
    public void SameDeadline_HigherPriority_IsPlacedFirst()
    {
        var deadline = WeekStart.AddDays(2);
        var low = Task(deadline, minutes: 60, priority: TaskPriority.Low);
        var critical = Task(deadline, minutes: 60, priority: TaskPriority.Critical);

        var plan = Plan(new[] { low, critical }, new[] { Window(DayOfWeek.Monday, 17, 18) });

        var session = Assert.Single(plan.Sessions);
        Assert.Equal(critical.Id, session.StudyTaskId);
        Assert.Equal(60, session.PlannedMinutes);
        Assert.Contains(plan.Unscheduled, item => item.StudyTaskId == low.Id && item.ScheduledMinutes == 0);
        Assert.DoesNotContain(plan.Unscheduled, item => item.StudyTaskId == critical.Id);
    }

    [Fact]
    public void ShortFinalSession_IsAllowedBelowTheMinimum()
    {
        var task = Task(deadline: WeekStart.AddDays(3), minutes: 70);
        var windows = new[]
        {
            Window(DayOfWeek.Monday, 17, 18),
            Window(DayOfWeek.Tuesday, 17, 19)
        };

        var plan = Plan(new[] { task }, windows);

        Assert.Equal(2, plan.Sessions.Count);
        Assert.Equal(new[] { 60, 10 }, plan.Sessions.Select(session => session.PlannedMinutes));
        Assert.Equal(WeekStart.AddHours(17), plan.Sessions[0].StartAtUtc);
        Assert.Equal(WeekStart.AddDays(1).AddHours(17), plan.Sessions[1].StartAtUtc);
        Assert.Equal(WeekStart.AddDays(1).AddHours(17).AddMinutes(10), plan.Sessions[1].EndAtUtc);
        Assert.Empty(plan.Unscheduled);
    }

    [Fact]
    public void LongTask_IsSplitAcrossDays()
    {
        var task = Task(deadline: WeekStart.AddDays(14), minutes: 360);
        var windows = new[]
        {
            Window(DayOfWeek.Monday, 17, 19),
            Window(DayOfWeek.Tuesday, 17, 19),
            Window(DayOfWeek.Wednesday, 17, 19)
        };

        var plan = Plan(new[] { task }, windows);

        Assert.Equal(3, plan.Sessions.Count);
        Assert.All(plan.Sessions, session => Assert.Equal(120, session.PlannedMinutes));
        Assert.Equal(
            new[] { WeekStart.AddHours(17), WeekStart.AddDays(1).AddHours(17), WeekStart.AddDays(2).AddHours(17) },
            plan.Sessions.Select(session => session.StartAtUtc));
        Assert.Empty(plan.Unscheduled);
    }

    [Fact]
    public void UnavailableException_RemovesThatDay()
    {
        var task = Task(deadline: WeekStart.AddDays(2), minutes: 120);
        var window = Window(DayOfWeek.Monday, 17, 19);
        var exception = new AvailabilityException
        {
            Id = Guid.NewGuid(),
            Date = DateOnly.FromDateTime(WeekStart),
            Kind = AvailabilityExceptionKind.Unavailable
        };

        var plan = Plan(new[] { task }, new[] { window }, new[] { exception });

        Assert.Empty(plan.Sessions);
        var left = Assert.Single(plan.Unscheduled);
        Assert.Equal(120, left.RemainingMinutes);
        Assert.Equal(0, left.ScheduledMinutes);
    }

    [Fact]
    public void CustomHours_ReplaceTheWeeklyWindow()
    {
        var task = Task(deadline: WeekStart.AddDays(2), minutes: 60);
        var window = Window(DayOfWeek.Monday, 17, 19);
        var exception = new AvailabilityException
        {
            Id = Guid.NewGuid(),
            Date = DateOnly.FromDateTime(WeekStart),
            Kind = AvailabilityExceptionKind.CustomHours,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0)
        };

        var plan = Plan(new[] { task }, new[] { window }, new[] { exception });

        var session = Assert.Single(plan.Sessions);
        Assert.Equal(WeekStart.AddHours(10), session.StartAtUtc);
        Assert.Equal(WeekStart.AddHours(11), session.EndAtUtc);
    }

    [Fact]
    public void InsufficientHours_AreReported()
    {
        var task = Task(deadline: WeekStart.AddDays(2), minutes: 180);
        var window = Window(DayOfWeek.Monday, 17, 18);

        var plan = Plan(new[] { task }, new[] { window });

        var session = Assert.Single(plan.Sessions);
        Assert.Equal(60, session.PlannedMinutes);
        var left = Assert.Single(plan.Unscheduled);
        Assert.Equal(180, left.RemainingMinutes);
        Assert.Equal(60, left.ScheduledMinutes);
    }

    [Fact]
    public void LockedSession_StaysInPlaceAndBlocksTheSlot()
    {
        var task = Task(deadline: WeekStart.AddDays(3), minutes: 240);
        var locked = new OccupiedSession(
            Guid.NewGuid(),
            task.Id,
            WeekStart.AddHours(17),
            WeekStart.AddHours(19),
            PlannedMinutes: 120,
            ActualMinutes: null,
            SessionStatus.Planned,
            IsLocked: true);
        var windows = new[]
        {
            Window(DayOfWeek.Monday, 17, 19),
            Window(DayOfWeek.Tuesday, 17, 19)
        };

        var plan = Plan(new[] { task }, windows, existing: new[] { locked });

        Assert.DoesNotContain(plan.Sessions, session => session.StartAtUtc == locked.StartAtUtc);
        var session = Assert.Single(plan.Sessions);
        Assert.Equal(WeekStart.AddDays(1).AddHours(17), session.StartAtUtc);
        Assert.Equal(120, session.PlannedMinutes);
        Assert.Empty(plan.Unscheduled);
    }

    [Fact]
    public void OverlappingFrozenSessions_AreReported()
    {
        var task = Task(deadline: WeekStart.AddDays(2), minutes: 30);
        var first = Frozen(task.Id, WeekStart.AddHours(17), WeekStart.AddHours(18));
        var second = Frozen(task.Id, WeekStart.AddHours(17).AddMinutes(30), WeekStart.AddHours(18).AddMinutes(30));

        var plan = Plan(new[] { task }, new[] { Window(DayOfWeek.Monday, 17, 20) }, existing: new[] { first, second });

        var conflict = Assert.Single(plan.Conflicts);
        Assert.Equal(first.Id, conflict.FirstSessionId);
        Assert.Equal(second.Id, conflict.SecondSessionId);
        Assert.Equal(30, (conflict.OverlapEndUtc - conflict.OverlapStartUtc).TotalMinutes);
    }

    [Fact]
    public void SameDeadlineAndPriority_HigherComplexity_IsPlacedFirst()
    {
        var deadline = WeekStart.AddDays(2);
        var simple = Task(deadline, minutes: 60, complexity: 1);
        var hard = Task(deadline, minutes: 60, complexity: 5);

        var plan = Plan(new[] { simple, hard }, new[] { Window(DayOfWeek.Monday, 17, 18) });

        Assert.Equal(hard.Id, Assert.Single(plan.Sessions).StudyTaskId);
        Assert.Contains(plan.Unscheduled, item => item.StudyTaskId == simple.Id && item.ScheduledMinutes == 0);
    }

    [Fact]
    public void Deadline_ClipsTheSlot_AndLeavesTheTailForALaterTask()
    {
        var early = Task(deadline: WeekStart.AddHours(18), minutes: 180);
        var later = Task(deadline: WeekStart.AddDays(2), minutes: 60, priority: TaskPriority.Low);

        var plan = Plan(new[] { later, early }, new[] { Window(DayOfWeek.Monday, 17, 20) });

        Assert.Equal(2, plan.Sessions.Count);
        Assert.Equal(early.Id, plan.Sessions[0].StudyTaskId);
        Assert.Equal(WeekStart.AddHours(17), plan.Sessions[0].StartAtUtc);
        Assert.Equal(WeekStart.AddHours(18), plan.Sessions[0].EndAtUtc);
        Assert.Equal(later.Id, plan.Sessions[1].StudyTaskId);
        Assert.Equal(WeekStart.AddHours(18), plan.Sessions[1].StartAtUtc);
        Assert.Equal(WeekStart.AddHours(19), plan.Sessions[1].EndAtUtc);
        var left = Assert.Single(plan.Unscheduled);
        Assert.Equal(early.Id, left.StudyTaskId);
        Assert.Equal(180, left.RemainingMinutes);
        Assert.Equal(60, left.ScheduledMinutes);
    }

    [Fact]
    public void ActualMinutesAndFrozenCoverage_AreNotAddedTogether()
    {
        var task = Task(deadline: WeekStart.AddDays(2), minutes: 180, actualMinutes: 60);
        var completed = new OccupiedSession(
            Guid.NewGuid(),
            task.Id,
            WeekStart.AddDays(-1).AddHours(17),
            WeekStart.AddDays(-1).AddHours(18),
            PlannedMinutes: 60,
            ActualMinutes: 60,
            SessionStatus.Completed,
            IsLocked: false);

        var plan = Plan(new[] { task }, new[] { Window(DayOfWeek.Monday, 17, 20) }, existing: new[] { completed });

        var session = Assert.Single(plan.Sessions);
        Assert.Equal(120, session.PlannedMinutes);
        Assert.Empty(plan.Unscheduled);
    }

    [Fact]
    public void UnlockedPlannedSession_DoesNotBlockOrReduceRemainingWork()
    {
        var task = Task(deadline: WeekStart.AddDays(2), minutes: 60);
        var planned = new OccupiedSession(
            Guid.NewGuid(),
            task.Id,
            WeekStart.AddHours(17),
            WeekStart.AddHours(18),
            PlannedMinutes: 60,
            ActualMinutes: null,
            SessionStatus.Planned,
            IsLocked: false);

        var plan = Plan(new[] { task }, new[] { Window(DayOfWeek.Monday, 17, 18) }, existing: new[] { planned });

        var session = Assert.Single(plan.Sessions);
        Assert.Equal(planned.StartAtUtc, session.StartAtUtc);
        Assert.Equal(60, session.PlannedMinutes);
        Assert.Empty(plan.Unscheduled);
        Assert.Empty(plan.Conflicts);
    }

    [Fact]
    public void CompletedTask_IsSkippedEvenWhenMinutesRemain()
    {
        var task = Task(deadline: WeekStart.AddDays(2), minutes: 60, status: StudyTaskStatus.Completed);

        var plan = Plan(new[] { task }, new[] { Window(DayOfWeek.Monday, 17, 18) });

        Assert.Empty(plan.Sessions);
        Assert.Empty(plan.Unscheduled);
    }

    [Fact]
    public void SlotShorterThanMinimum_IsLeftWhenRemainingWorkIsLonger()
    {
        var task = Task(deadline: WeekStart.AddDays(3), minutes: 90);
        var windows = new[]
        {
            Window(DayOfWeek.Monday, new TimeOnly(17, 0), new TimeOnly(17, 20)),
            Window(DayOfWeek.Tuesday, new TimeOnly(17, 0), new TimeOnly(19, 0))
        };

        var plan = Plan(new[] { task }, windows);

        var session = Assert.Single(plan.Sessions);
        Assert.Equal(90, session.PlannedMinutes);
        Assert.Equal(WeekStart.AddDays(1).AddHours(17), session.StartAtUtc);
        Assert.Empty(plan.Unscheduled);
    }

    [Fact]
    public void PlacedSessions_DoNotOverlap()
    {
        var first = Task(deadline: WeekStart.AddDays(1), minutes: 60);
        var second = Task(deadline: WeekStart.AddDays(2), minutes: 60, priority: TaskPriority.Critical);

        var plan = Plan(new[] { second, first }, new[] { Window(DayOfWeek.Monday, 17, 19) });

        Assert.Equal(2, plan.Sessions.Count);
        Assert.Equal(first.Id, plan.Sessions[0].StudyTaskId);
        Assert.Equal(second.Id, plan.Sessions[1].StudyTaskId);
        Assert.Equal(plan.Sessions[0].EndAtUtc, plan.Sessions[1].StartAtUtc);
        Assert.Empty(plan.Unscheduled);
    }

    private SchedulePlan Plan(
        IReadOnlyList<SchedulableTask> tasks,
        IReadOnlyList<AvailabilityWindow> windows,
        IReadOnlyList<AvailabilityException>? exceptions = null,
        IReadOnlyList<OccupiedSession>? existing = null) =>
        _planner.Plan(
            new ScheduleHorizon(WeekStart, WeekEnd, Utc),
            windows,
            exceptions ?? Array.Empty<AvailabilityException>(),
            tasks,
            existing ?? Array.Empty<OccupiedSession>());

    private static SchedulableTask Task(
        DateTime deadline,
        int minutes,
        TaskPriority priority = TaskPriority.Medium,
        int complexity = 3,
        int actualMinutes = 0,
        StudyTaskStatus status = StudyTaskStatus.NotStarted) =>
        new(Guid.NewGuid(), deadline, complexity, priority, minutes, actualMinutes, status);

    private static AvailabilityWindow Window(DayOfWeek day, int startHour, int endHour) =>
        Window(day, new TimeOnly(startHour, 0), new TimeOnly(endHour, 0));

    private static AvailabilityWindow Window(DayOfWeek day, TimeOnly start, TimeOnly end) =>
        new()
        {
            Id = Guid.NewGuid(),
            DayOfWeek = day,
            StartTime = start,
            EndTime = end
        };

    private static OccupiedSession Frozen(Guid taskId, DateTime start, DateTime end) =>
        new(Guid.NewGuid(), taskId, start, end, (int)(end - start).TotalMinutes, null, SessionStatus.Completed, IsLocked: false);
}
