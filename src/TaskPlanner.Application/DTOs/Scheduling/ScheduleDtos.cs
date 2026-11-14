using TaskPlanner.Domain.Enums;

namespace TaskPlanner.Application.DTOs.Scheduling;

public sealed record GenerateScheduleRequest(
    DateTime? From,
    DateTime? To,
    int? Days,
    string? TimeZoneId);

public sealed record GenerateScheduleResponse(
    DateTime From,
    DateTime To,
    IReadOnlyList<GeneratedSessionDto> Sessions,
    IReadOnlyList<UnscheduledWorkDto> Unscheduled,
    IReadOnlyList<ScheduleConflictDto> Conflicts);

public sealed record GeneratedSessionDto(
    Guid Id,
    Guid StudyTaskId,
    DateTime StartAtUtc,
    DateTime EndAtUtc,
    int PlannedMinutes,
    SessionStatus Status);

public sealed record UnscheduledWorkDto(
    Guid StudyTaskId,
    int RemainingMinutes,
    int ScheduledMinutes);

public sealed record ScheduleConflictDto(
    Guid FirstSessionId,
    Guid SecondSessionId,
    DateTime OverlapStartUtc,
    DateTime OverlapEndUtc);
