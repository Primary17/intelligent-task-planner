using TaskPlanner.Maui.Api;
using TaskPlanner.Maui.Localization;

namespace TaskPlanner.Maui.Services;

public sealed record DeadlineReminder(int Id, string Title, string Body, DateTimeOffset When);

/// <summary>
/// Deadline reminders use the task's date and time. Study reminders use the weekly availability for the next week.
/// </summary>
public static class DeadlineReminderPlan
{
    public static readonly TimeSpan LeadTime = TimeSpan.FromHours(1);

    public static IReadOnlyList<DeadlineReminder> Build(
        IEnumerable<StudyTaskDto> tasks,
        IReadOnlyList<EffectiveDayDto> days,
        DateTimeOffset now,
        NotificationPreferences preferences)
    {
        var reminders = new List<DeadlineReminder>();
        AddDeadlines(reminders, tasks, now, preferences);
        AddStudyDays(reminders, days, now, preferences);
        return reminders;
    }

    private static void AddDeadlines(
        List<DeadlineReminder> reminders,
        IEnumerable<StudyTaskDto> tasks,
        DateTimeOffset now,
        NotificationPreferences preferences)
    {
        foreach (var task in tasks)
        {
            if (task.Status == StudyTaskStatus.Completed)
            {
                continue;
            }

            var deadline = AsUtc(task.DeadlineUtc);
            if (deadline <= now)
            {
                continue;
            }

            var body = $"{task.Title} · {task.DisciplineName}";
            var id = StableId(task.Id);
            if (preferences.RemindHourBefore)
            {
                var when = deadline - LeadTime;
                if (when > now)
                {
                    reminders.Add(new DeadlineReminder(id, Localizer.Current["Reminder_Soon"], body, when));
                }
            }

            if (preferences.RemindAtDeadline)
            {
                reminders.Add(new DeadlineReminder(AtDeadlineId(id), Localizer.Current["Reminder_Now"], body, deadline));
            }
        }
    }

    private static void AddStudyDays(
        List<DeadlineReminder> reminders,
        IReadOnlyList<EffectiveDayDto> days,
        DateTimeOffset now,
        NotificationPreferences preferences)
    {
        foreach (var day in days)
        {
            var intervals = (day.Intervals ?? [])
                .Where(interval => interval.EndTime > interval.StartTime)
                .OrderBy(interval => interval.StartTime)
                .ToList();

            if (preferences.MorningSummary)
            {
                var morning = Local(day.Date, TimeOnly.FromTimeSpan(preferences.MorningTime));
                if (morning > now)
                {
                    if (intervals.Count == 0)
                    {
                        reminders.Add(new DeadlineReminder(
                            Mix(day.Date.DayNumber, 0, 1),
                            Localizer.Current["Reminder_DayOffTitle"],
                            Localizer.Current["Reminder_DayOffBody"],
                            morning));
                    }
                    else
                    {
                        var ranges = string.Join(", ", intervals.Select(FormatInterval));
                        reminders.Add(new DeadlineReminder(
                            Mix(day.Date.DayNumber, 0, 1),
                            Localizer.Current["Reminder_MorningTitle"],
                            Localizer.Current.Format("Reminder_MorningBody", ranges),
                            morning));
                    }
                }
            }

            if (!preferences.RemindStudyStart)
            {
                continue;
            }

            for (var i = 0; i < intervals.Count; i++)
            {
                var start = Local(day.Date, intervals[i].StartTime);
                if (start <= now)
                {
                    continue;
                }

                reminders.Add(new DeadlineReminder(
                    Mix(day.Date.DayNumber, i + 1, 2),
                    Localizer.Current["Reminder_StudyStarts"],
                    FormatInterval(intervals[i]),
                    start));
            }
        }
    }

    private static string FormatInterval(TimeIntervalDto interval) =>
        $"{interval.StartTime:HH\\:mm}–{interval.EndTime:HH\\:mm}";

    private static DateTimeOffset Local(DateOnly date, TimeOnly time)
    {
        var local = DateTime.SpecifyKind(date.ToDateTime(time), DateTimeKind.Local);
        return new DateTimeOffset(local);
    }

    private static int Mix(int dayNumber, int slot, int kind)
    {
        unchecked
        {
            var hash = 17;
            hash = (hash * 31) + dayNumber;
            hash = (hash * 31) + slot;
            hash = (hash * 31) + kind;
            if (hash == int.MinValue)
            {
                return 5;
            }

            var id = Math.Abs(hash);
            return id == 0 ? 5 : id;
        }
    }

    public static int AtDeadlineId(int hourBeforeId)
    {
        var id = hourBeforeId ^ 0x40000000;
        return id is 0 or int.MinValue ? 3 : Math.Abs(id);
    }

    public static int StableId(Guid taskId)
    {
        var hash = BitConverter.ToInt32(taskId.ToByteArray(), 0);
        if (hash == int.MinValue)
        {
            return 1;
        }

        var id = Math.Abs(hash);
        return id == 0 ? 1 : id;
    }

    private static DateTimeOffset AsUtc(DateTime value)
    {
        var utc = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
        return new DateTimeOffset(utc);
    }
}
