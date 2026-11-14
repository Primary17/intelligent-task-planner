using FluentValidation;
using TaskPlanner.Application.DTOs.Scheduling;

namespace TaskPlanner.Application.Validation;

public static class ScheduleHorizonRules
{
    public const int MaxDays = 92;

    public static bool TryResolve(
        GenerateScheduleRequest request,
        DateTime utcNow,
        out DateTime from,
        out DateTime to,
        out string? error)
    {
        var hasRange = request.From.HasValue || request.To.HasValue;
        var hasDays = request.Days.HasValue;

        if (hasRange == hasDays)
        {
            from = default;
            to = default;
            error = "Provide either from and to, or days.";
            return false;
        }

        if (hasDays)
        {
            if (request.Days is < 1 or > MaxDays)
            {
                from = default;
                to = default;
                error = $"days must be between 1 and {MaxDays}.";
                return false;
            }

            from = AsUtc(utcNow);
            to = from.AddDays(request.Days!.Value);
            error = null;
            return true;
        }

        if (request.From is null || request.To is null)
        {
            from = default;
            to = default;
            error = "Both from and to are required.";
            return false;
        }

        from = AsUtc(request.From.Value);
        to = AsUtc(request.To.Value);
        if (to <= from)
        {
            error = "to must be later than from.";
            return false;
        }

        if (to - from > TimeSpan.FromDays(MaxDays))
        {
            error = $"The range cannot exceed {MaxDays} days.";
            return false;
        }

        error = null;
        return true;
    }

    public static bool TryResolveTimeZone(string? timeZoneId, out TimeZoneInfo timeZone, out string? error)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            timeZone = TimeZoneInfo.Utc;
            error = null;
            return true;
        }

        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            error = null;
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            timeZone = TimeZoneInfo.Utc;
            error = "Unknown time zone.";
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            timeZone = TimeZoneInfo.Utc;
            error = "Unknown time zone.";
            return false;
        }
    }

    public static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}

public sealed class GenerateScheduleRequestValidator : AbstractValidator<GenerateScheduleRequest>
{
    public GenerateScheduleRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => ScheduleHorizonRules.TryResolve(x, DateTime.UtcNow, out _, out _, out _))
            .WithMessage(x =>
            {
                ScheduleHorizonRules.TryResolve(x, DateTime.UtcNow, out _, out _, out var error);
                return error ?? "The horizon is invalid.";
            });

        RuleFor(x => x.TimeZoneId)
            .Must(id => ScheduleHorizonRules.TryResolveTimeZone(id, out _, out _))
            .WithMessage("Unknown time zone.");
    }
}
