using TaskPlanner.Domain.Enums;

namespace TaskPlanner.Application.DTOs.Availability;

public sealed record AvailabilityWindowDto(
    Guid Id,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime);

public sealed record AvailabilityWindowRequest(
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime);

public sealed record AvailabilityExceptionDto(
    Guid Id,
    DateOnly Date,
    AvailabilityExceptionKind Kind,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    string? Reason);

public sealed record AvailabilityExceptionRequest(
    DateOnly Date,
    AvailabilityExceptionKind Kind,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    string? Reason);

public sealed record TimeIntervalDto(TimeOnly StartTime, TimeOnly EndTime);

public sealed record EffectiveAvailabilityDayDto(
    DateOnly Date,
    string Source,
    IReadOnlyList<TimeIntervalDto> Intervals);
