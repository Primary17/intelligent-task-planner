namespace TaskPlanner.Application.DTOs.Planning;

public enum CapacityStatus
{
    Ok = 0,
    Shortfall = 1,
    Overdue = 2
}

public sealed record TaskCapacityDto(
    Guid TaskId,
    string Title,
    Guid DisciplineId,
    string DisciplineName,
    DateTime DeadlineUtc,
    int RemainingMinutes,
    int PlannedMinutes,
    int UnplannedMinutes,
    int AvailableMinutes,
    int RequiredMinutes,
    int ShortfallMinutes,
    CapacityStatus Status);

public sealed record CapacityReportDto(
    DateTime GeneratedAtUtc,
    int TotalShortfallMinutes,
    IReadOnlyList<TaskCapacityDto> Tasks);
