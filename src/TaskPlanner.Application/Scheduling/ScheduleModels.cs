using TaskPlanner.Domain.Enums;

namespace TaskPlanner.Application.Scheduling;

public sealed record ScheduleHorizon(DateTime StartUtc, DateTime EndUtc, TimeZoneInfo TimeZone);

public sealed record ScheduleOptions
{
    public int MaxSessionMinutes { get; init; } = 120;

    public int MinSessionMinutes { get; init; } = 30;
}

public sealed record SchedulableTask(
    Guid Id,
    DateTime DeadlineUtc,
    int Complexity,
    TaskPriority Priority,
    int EstimatedMinutes,
    int ActualMinutes,
    StudyTaskStatus Status);

public sealed record OccupiedSession(
    Guid Id,
    Guid StudyTaskId,
    DateTime StartAtUtc,
    DateTime EndAtUtc,
    int PlannedMinutes,
    int? ActualMinutes,
    SessionStatus Status,
    bool IsLocked);

public sealed record PlannedSession(
    Guid StudyTaskId,
    DateTime StartAtUtc,
    DateTime EndAtUtc,
    int PlannedMinutes);

public sealed record UnscheduledWork(
    Guid StudyTaskId,
    int RemainingMinutes,
    int ScheduledMinutes);

public sealed record ScheduleConflict(
    Guid FirstSessionId,
    Guid SecondSessionId,
    DateTime OverlapStartUtc,
    DateTime OverlapEndUtc);

public sealed record SchedulePlan(
    IReadOnlyList<PlannedSession> Sessions,
    IReadOnlyList<UnscheduledWork> Unscheduled,
    IReadOnlyList<ScheduleConflict> Conflicts);
