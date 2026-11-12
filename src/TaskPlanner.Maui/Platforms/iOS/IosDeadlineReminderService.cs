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
            var seconds = (reminder.When - now).TotalSeconds;
            if (seconds < 1)
            {
                seconds = 1;
            }

            var content = new UNMutableNotificationContent
            {
                Title = reminder.Title,
                Body = reminder.Body
            };
            var trigger = UNTimeIntervalNotificationTrigger.CreateTrigger(seconds, false);
            var request = UNNotificationRequest.FromIdentifier(
                reminder.Id.ToString(),
                content,
                trigger);
            await center.AddNotificationRequestAsync(request);
        }
    }
}
