using TaskPlanner.Domain.Entities;
using TaskPlanner.Domain.Enums;

namespace TaskPlanner.Application.Scheduling;

public sealed class SchedulePlanner
{
    public SchedulePlan Plan(
        ScheduleHorizon horizon,
        IReadOnlyList<AvailabilityWindow> windows,
        IReadOnlyList<AvailabilityException> exceptions,
        IReadOnlyList<SchedulableTask> tasks,
        IReadOnlyList<OccupiedSession> existingSessions,
        ScheduleOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(horizon);
        ArgumentNullException.ThrowIfNull(windows);
        ArgumentNullException.ThrowIfNull(exceptions);
        ArgumentNullException.ThrowIfNull(tasks);
        ArgumentNullException.ThrowIfNull(existingSessions);

        options ??= new ScheduleOptions();
        if (options.MinSessionMinutes < 1 || options.MaxSessionMinutes < options.MinSessionMinutes)
        {
            throw new ArgumentException("Session length bounds are invalid.", nameof(options));
        }

        var startUtc = AssumeUtc(horizon.StartUtc);
        var endUtc = AssumeUtc(horizon.EndUtc);
        if (endUtc <= startUtc)
        {
            throw new ArgumentException("Horizon end must be later than its start.", nameof(horizon));
        }

        var timeZone = horizon.TimeZone ?? TimeZoneInfo.Utc;
        var frozen = existingSessions.Where(IsFrozen).ToList();
        var conflicts = FindConflicts(frozen);
        var free = BuildFreeIntervals(startUtc, endUtc, timeZone, windows, exceptions);
        foreach (var session in frozen)
        {
            free = Subtract(free, AssumeUtc(session.StartAtUtc), AssumeUtc(session.EndAtUtc));
        }

        var remainingByTask = tasks.ToDictionary(t => t.Id, t => RemainingMinutes(t, frozen));
        var ordered = tasks
            .Where(t => t.Status != StudyTaskStatus.Completed && remainingByTask[t.Id] > 0)
            .OrderBy(t => AssumeUtc(t.DeadlineUtc))
            .ThenByDescending(t => t.Priority)
            .ThenByDescending(t => t.Complexity)
            .ThenByDescending(t => remainingByTask[t.Id])
            .ThenBy(t => t.Id)
            .ToList();

        var planned = new List<PlannedSession>();
        var scheduledByTask = new Dictionary<Guid, int>();

        foreach (var task in ordered)
        {
            var left = remainingByTask[task.Id];
            var deadline = AssumeUtc(task.DeadlineUtc);
            var placed = 0;

            while (left > 0)
            {
                var slotIndex = FindSlot(free, deadline, left, options);
                if (slotIndex < 0)
                {
                    break;
                }

                var slot = free[slotIndex];
                var usableEnd = slot.EndUtc < deadline ? slot.EndUtc : deadline;
                var usableMinutes = WholeMinutes(slot.StartUtc, usableEnd);
                var take = Math.Min(left, Math.Min(options.MaxSessionMinutes, usableMinutes));
                var sessionEnd = slot.StartUtc.AddMinutes(take);

                planned.Add(new PlannedSession(task.Id, slot.StartUtc, sessionEnd, take));
                free = ReplaceSlot(free, slotIndex, sessionEnd);
                left -= take;
                placed += take;
            }

            if (placed < remainingByTask[task.Id])
            {
                scheduledByTask[task.Id] = placed;
            }
        }

        var unscheduled = scheduledByTask
            .OrderBy(pair => pair.Key)
            .Select(pair => new UnscheduledWork(pair.Key, remainingByTask[pair.Key], pair.Value))
            .ToList();

        return new SchedulePlan(planned, unscheduled, conflicts);
    }

    public static bool IsFrozen(OccupiedSession session) =>
        session.Status == SessionStatus.Completed || session.IsLocked;

    public static int RemainingMinutes(SchedulableTask task, IEnumerable<OccupiedSession> sessions)
    {
        var covered = sessions
            .Where(s => s.StudyTaskId == task.Id && IsFrozen(s))
            .Sum(CoveredMinutes);

        return Math.Max(0, task.EstimatedMinutes - Math.Max(task.ActualMinutes, covered));
    }

    private static int FindSlot(IReadOnlyList<TimeSlot> free, DateTime deadline, int left, ScheduleOptions options)
    {
        for (var i = 0; i < free.Count; i++)
        {
            var slot = free[i];
            if (slot.StartUtc >= deadline)
            {
                continue;
            }

            var usableEnd = slot.EndUtc < deadline ? slot.EndUtc : deadline;
            var usableMinutes = WholeMinutes(slot.StartUtc, usableEnd);
            if (usableMinutes <= 0)
            {
                continue;
            }

            var take = Math.Min(left, Math.Min(options.MaxSessionMinutes, usableMinutes));
            if (left >= options.MinSessionMinutes && take < options.MinSessionMinutes)
            {
                continue;
            }

            return i;
        }

        return -1;
    }

    private static List<TimeSlot> ReplaceSlot(List<TimeSlot> free, int index, DateTime consumedUntil)
    {
        var slot = free[index];
        free.RemoveAt(index);
        if (consumedUntil < slot.EndUtc)
        {
            free.Insert(index, new TimeSlot(consumedUntil, slot.EndUtc));
        }

        return free;
    }

    private static List<TimeSlot> BuildFreeIntervals(
        DateTime startUtc,
        DateTime endUtc,
        TimeZoneInfo timeZone,
        IReadOnlyList<AvailabilityWindow> windows,
        IReadOnlyList<AvailabilityException> exceptions)
    {
        var exceptionByDate = exceptions
            .GroupBy(e => e.Date)
            .ToDictionary(g => g.Key, g => g.First());

        var startLocal = TimeZoneInfo.ConvertTimeFromUtc(startUtc, timeZone);
        var endLocal = TimeZoneInfo.ConvertTimeFromUtc(endUtc, timeZone);
        var firstDate = DateOnly.FromDateTime(startLocal);
        var lastDate = DateOnly.FromDateTime(endLocal.AddTicks(-1));

        var slots = new List<TimeSlot>();
        for (var date = firstDate; date <= lastDate; date = date.AddDays(1))
        {
            foreach (var (localStart, localEnd) in LocalIntervals(date, windows, exceptionByDate))
            {
                if (ToUtc(date, localStart, timeZone) is not DateTime slotStart
                    || ToUtc(date, localEnd, timeZone) is not DateTime slotEnd)
                {
                    continue;
                }

                if (slotStart < startUtc)
                {
                    slotStart = startUtc;
                }

                if (slotEnd > endUtc)
                {
                    slotEnd = endUtc;
                }

                if (slotEnd > slotStart)
                {
                    slots.Add(new TimeSlot(slotStart, slotEnd));
                }
            }
        }

        return Merge(slots);
    }

    private static IEnumerable<(TimeOnly Start, TimeOnly End)> LocalIntervals(
        DateOnly date,
        IReadOnlyList<AvailabilityWindow> windows,
        IReadOnlyDictionary<DateOnly, AvailabilityException> exceptionByDate)
    {
        if (exceptionByDate.TryGetValue(date, out var exception))
        {
            if (exception.Kind == AvailabilityExceptionKind.CustomHours
                && exception.StartTime is TimeOnly start
                && exception.EndTime is TimeOnly end
                && end > start)
            {
                yield return (start, end);
            }

            yield break;
        }

        foreach (var window in windows.Where(w => w.DayOfWeek == date.DayOfWeek && w.EndTime > w.StartTime))
        {
            yield return (window.StartTime, window.EndTime);
        }
    }

    private static List<TimeSlot> Subtract(List<TimeSlot> free, DateTime blockStart, DateTime blockEnd)
    {
        if (blockEnd <= blockStart)
        {
            return free;
        }

        var result = new List<TimeSlot>();
        foreach (var slot in free)
        {
            if (blockEnd <= slot.StartUtc || blockStart >= slot.EndUtc)
            {
                result.Add(slot);
                continue;
            }

            if (blockStart > slot.StartUtc)
            {
                result.Add(new TimeSlot(slot.StartUtc, blockStart));
            }

            if (blockEnd < slot.EndUtc)
            {
                result.Add(new TimeSlot(blockEnd, slot.EndUtc));
            }
        }

        return result;
    }

    private static List<TimeSlot> Merge(IEnumerable<TimeSlot> slots)
    {
        var merged = new List<TimeSlot>();
        foreach (var slot in slots.OrderBy(s => s.StartUtc))
        {
            if (merged.Count == 0 || merged[^1].EndUtc < slot.StartUtc)
            {
                merged.Add(slot);
                continue;
            }

            if (slot.EndUtc > merged[^1].EndUtc)
            {
                merged[^1] = new TimeSlot(merged[^1].StartUtc, slot.EndUtc);
            }
        }

        return merged;
    }

    private static IReadOnlyList<ScheduleConflict> FindConflicts(IReadOnlyList<OccupiedSession> frozen)
    {
        var conflicts = new List<ScheduleConflict>();
        for (var i = 0; i < frozen.Count; i++)
        {
            var firstStart = AssumeUtc(frozen[i].StartAtUtc);
            var firstEnd = AssumeUtc(frozen[i].EndAtUtc);
            for (var j = i + 1; j < frozen.Count; j++)
            {
                var secondStart = AssumeUtc(frozen[j].StartAtUtc);
                var secondEnd = AssumeUtc(frozen[j].EndAtUtc);
                var overlapStart = firstStart > secondStart ? firstStart : secondStart;
                var overlapEnd = firstEnd < secondEnd ? firstEnd : secondEnd;
                if (overlapEnd > overlapStart)
                {
                    conflicts.Add(new ScheduleConflict(frozen[i].Id, frozen[j].Id, overlapStart, overlapEnd));
                }
            }
        }

        return conflicts;
    }

    private static int CoveredMinutes(OccupiedSession session) =>
        session.Status == SessionStatus.Completed
            ? session.ActualMinutes ?? session.PlannedMinutes
            : session.PlannedMinutes;

    private static int WholeMinutes(DateTime start, DateTime end) =>
        (int)Math.Floor((end - start).TotalMinutes);

    private static DateTime AssumeUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private static DateTime? ToUtc(DateOnly date, TimeOnly time, TimeZoneInfo timeZone)
    {
        var local = DateTime.SpecifyKind(date.ToDateTime(time), DateTimeKind.Unspecified);
        if (timeZone.IsInvalidTime(local))
        {
            return null;
        }

        if (timeZone.IsAmbiguousTime(local))
        {
            var offset = timeZone.GetAmbiguousTimeOffsets(local).Min();
            return new DateTimeOffset(local, offset).UtcDateTime;
        }

        return TimeZoneInfo.ConvertTimeToUtc(local, timeZone);
    }

    private readonly record struct TimeSlot(DateTime StartUtc, DateTime EndUtc);
}
