using TaskPlanner.Application.DTOs.Planning;
using TaskPlanner.Domain.Entities;
using TaskPlanner.Domain.Enums;

namespace TaskPlanner.Application.Planning;

public sealed record CapacityTaskInput(
    Guid Id,
    string Title,
    Guid DisciplineId,
    string DisciplineName,
    DateTime DeadlineUtc,
    int RemainingMinutes,
    int PlannedMinutes);

public static class CapacityCalculator
{
    public const int MaxHorizonDays = 366;

    private readonly record struct Span(DateTime Start, DateTime End);

    public static CapacityReportDto Calculate(
        DateTime nowUtc,
        int utcOffsetMinutes,
        IReadOnlyCollection<AvailabilityWindow> windows,
        IReadOnlyCollection<AvailabilityException> exceptions,
        IReadOnlyCollection<CapacityTaskInput> tasks)
    {
        var open = tasks.Where(t => t.RemainingMinutes > 0).ToList();
        var upcoming = open.Where(t => t.DeadlineUtc > nowUtc).ToList();
        var overdue = open.Where(t => t.DeadlineUtc <= nowUtc).ToList();

        var horizon = upcoming.Count == 0
            ? nowUtc
            : upcoming.Max(t => t.DeadlineUtc);
        var limit = nowUtc.AddDays(MaxHorizonDays);
        var spans = BuildSpans(nowUtc, horizon < limit ? horizon : limit, utcOffsetMinutes, windows, exceptions);

        var rows = new List<TaskCapacityDto>();

        foreach (var task in overdue.OrderBy(t => t.DeadlineUtc))
        {
            rows.Add(ToDto(task, available: 0, required: task.RemainingMinutes, CapacityStatus.Overdue));
        }

        foreach (var task in upcoming.OrderBy(t => t.DeadlineUtc).ThenBy(t => t.Title, StringComparer.OrdinalIgnoreCase))
        {
            var required = upcoming.Where(t => t.DeadlineUtc <= task.DeadlineUtc).Sum(t => t.RemainingMinutes);
            var available = AvailableUntil(spans, task.DeadlineUtc);
            var status = required > available ? CapacityStatus.Shortfall : CapacityStatus.Ok;
            rows.Add(ToDto(task, available, required, status));
        }

        var total = rows.Where(r => r.Status == CapacityStatus.Shortfall).Select(r => r.ShortfallMinutes).DefaultIfEmpty(0).Max();
        return new CapacityReportDto(nowUtc, total, rows);
    }

    private static TaskCapacityDto ToDto(CapacityTaskInput task, int available, int required, CapacityStatus status) => new(
        task.Id,
        task.Title,
        task.DisciplineId,
        task.DisciplineName,
        task.DeadlineUtc,
        task.RemainingMinutes,
        task.PlannedMinutes,
        Math.Max(0, task.RemainingMinutes - task.PlannedMinutes),
        available,
        required,
        status == CapacityStatus.Ok ? 0 : Math.Max(0, required - available),
        status);

    private static int AvailableUntil(List<Span> spans, DateTime deadline)
    {
        double minutes = 0;
        foreach (var span in spans)
        {
            var end = span.End < deadline ? span.End : deadline;
            if (end > span.Start)
            {
                minutes += (end - span.Start).TotalMinutes;
            }
        }

        return (int)Math.Floor(minutes);
    }

    private static List<Span> BuildSpans(
        DateTime nowUtc,
        DateTime horizonUtc,
        int utcOffsetMinutes,
        IReadOnlyCollection<AvailabilityWindow> windows,
        IReadOnlyCollection<AvailabilityException> exceptions)
    {
        var spans = new List<Span>();
        if (horizonUtc <= nowUtc)
        {
            return spans;
        }

        var offset = TimeSpan.FromMinutes(utcOffsetMinutes);
        var firstDay = DateOnly.FromDateTime(nowUtc + offset);
        var lastDay = DateOnly.FromDateTime(horizonUtc + offset);
        var byDate = exceptions.GroupBy(e => e.Date).ToDictionary(g => g.Key, g => g.First());

        for (var day = firstDay; day <= lastDay; day = day.AddDays(1))
        {
            foreach (var (start, end) in IntervalsOn(day, windows, byDate))
            {
                var startUtc = DateTime.SpecifyKind(day.ToDateTime(start) - offset, DateTimeKind.Utc);
                var endUtc = DateTime.SpecifyKind(day.ToDateTime(end) - offset, DateTimeKind.Utc);
                if (startUtc < nowUtc)
                {
                    startUtc = nowUtc;
                }

                if (endUtc > startUtc)
                {
                    spans.Add(new Span(startUtc, endUtc));
                }
            }
        }

        return spans;
    }

    private static IEnumerable<(TimeOnly Start, TimeOnly End)> IntervalsOn(
        DateOnly day,
        IReadOnlyCollection<AvailabilityWindow> windows,
        Dictionary<DateOnly, AvailabilityException> exceptions)
    {
        if (exceptions.TryGetValue(day, out var exception))
        {
            if (exception.Kind == AvailabilityExceptionKind.CustomHours
                && exception.StartTime is { } start && exception.EndTime is { } end)
            {
                yield return (start, end);
            }

            yield break;
        }

        foreach (var window in windows.Where(w => w.DayOfWeek == day.DayOfWeek))
        {
            yield return (window.StartTime, window.EndTime);
        }
    }
}
