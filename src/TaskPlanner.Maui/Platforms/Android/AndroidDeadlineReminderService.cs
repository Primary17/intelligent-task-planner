using Android.App;
using Android.Content;
using TaskPlanner.Maui.Api;
using TaskPlanner.Maui.Services;
using Application = Android.App.Application;

namespace TaskPlanner.Maui;

public sealed class AndroidDeadlineReminderService : IDeadlineReminderService
{
    private const string StoredIdsKey = "deadline-reminder-ids";
    private readonly NotificationPreferences _preferences;

    public AndroidDeadlineReminderService(NotificationPreferences preferences)
    {
        _preferences = preferences;
    }

    public async Task SyncAsync(
        IReadOnlyList<StudyTaskDto> tasks,
        IReadOnlyList<EffectiveDayDto> days,
        CancellationToken cancellationToken = default)
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            var permission = await Permissions.RequestAsync<Permissions.PostNotifications>();
            if (permission != PermissionStatus.Granted)
            {
                return;
            }
        }

        var context = Application.Context;
        var alarm = (AlarmManager?)context.GetSystemService(Context.AlarmService);
        if (alarm is null)
        {
            return;
        }

        CancelStored(context, alarm);

        var reminders = DeadlineReminderPlan.Build(tasks, days, DateTimeOffset.UtcNow, _preferences);
        foreach (var reminder in reminders)
        {
            var intent = new Intent(context, typeof(DeadlineAlarmReceiver));
            intent.PutExtra("id", reminder.Id);
            intent.PutExtra("title", reminder.Title);
            intent.PutExtra("body", reminder.Body);
            var pending = PendingIntent.GetBroadcast(
                context,
                reminder.Id,
                intent,
                PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);
            if (pending is null)
            {
                continue;
            }

            alarm.Set(AlarmType.RtcWakeup, reminder.When.ToUnixTimeMilliseconds(), pending);
        }

        Preferences.Set(StoredIdsKey, string.Join(',', reminders.Select(r => r.Id)));
    }

    private static void CancelStored(Context context, AlarmManager alarm)
    {
        var stored = Preferences.Get(StoredIdsKey, string.Empty);
        if (string.IsNullOrWhiteSpace(stored))
        {
            return;
        }

        foreach (var part in stored.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            if (!int.TryParse(part, out var id))
            {
                continue;
            }

            var intent = new Intent(context, typeof(DeadlineAlarmReceiver));
            var pending = PendingIntent.GetBroadcast(
                context,
                id,
                intent,
                PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);
            if (pending is not null)
            {
                alarm.Cancel(pending);
                pending.Cancel();
            }
        }
    }
}
