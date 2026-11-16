using Foundation;
using TaskPlanner.Maui.Api;
using TaskPlanner.Maui.Services;
using UserNotifications;

namespace TaskPlanner.Maui;

public sealed class IosDeadlineReminderService : IDeadlineReminderService
{
    private readonly NotificationPreferences _preferences;

    public IosDeadlineReminderService(NotificationPreferences preferences)
    {
        _preferences = preferences;
    }

    public async Task SyncAsync(
        IReadOnlyList<StudyTaskDto> tasks,
        IReadOnlyList<EffectiveDayDto> days,
        CancellationToken cancellationToken = default)
    {
        var center = UNUserNotificationCenter.Current;
        var (granted, _) = await center.RequestAuthorizationAsync(
            UNAuthorizationOptions.Alert | UNAuthorizationOptions.Sound);
        if (!granted)
        {
            return;
        }

        center.RemoveAllPendingNotificationRequests();
        var now = DateTimeOffset.UtcNow;
        foreach (var reminder in DeadlineReminderPlan.Build(tasks, days, now, _preferences))
        {
            if (reminder.When <= now)
            {
                continue;
            }

            var content = new UNMutableNotificationContent
            {
                Title = reminder.Title,
                Body = reminder.Body,
                Sound = UNNotificationSound.Default
            };

            var trigger = CreateWallClockTrigger(reminder.When);
            if (trigger is null)
            {
                continue;
            }

            var request = UNNotificationRequest.FromIdentifier(
                reminder.Id.ToString(),
                content,
                trigger);
            await center.AddNotificationRequestAsync(request);
        }
    }

    private static UNNotificationTrigger? CreateWallClockTrigger(DateTimeOffset when)
    {
        var local = when.ToLocalTime();
        var components = new NSDateComponents
        {
            Year = local.Year,
            Month = local.Month,
            Day = local.Day,
            Hour = local.Hour,
            Minute = local.Minute,
            Second = local.Second
        };

        if (components.Date is NSDate date && date.SecondsSinceReferenceDate <= NSDate.Now.SecondsSinceReferenceDate)
        {
            return null;
        }

        return UNCalendarNotificationTrigger.CreateTrigger(components, false);
    }
}