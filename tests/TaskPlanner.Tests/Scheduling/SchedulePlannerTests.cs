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

    private static SchedulableTask Task(DateTime deadline, int minutes, TaskPriority priority = TaskPriority.Medium) =>
        new(Guid.NewGuid(), deadline, Complexity: 3, priority, minutes, ActualMinutes: 0, StudyTaskStatus.NotStarted);

    private static AvailabilityWindow Window(DayOfWeek day, int startHour, int endHour) =>
        new()
        {
            Id = Guid.NewGuid(),
            DayOfWeek = day,
            StartTime = new TimeOnly(startHour, 0),
            EndTime = new TimeOnly(endHour, 0)
        };

    private static OccupiedSession Frozen(Guid taskId, DateTime start, DateTime end) =>
        new(Guid.NewGuid(), taskId, start, end, (int)(end - start).TotalMinutes, null, SessionStatus.Completed, IsLocked: false);
}
