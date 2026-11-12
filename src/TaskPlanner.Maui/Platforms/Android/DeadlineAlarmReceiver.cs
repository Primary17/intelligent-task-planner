using Android.App;
using Android.Content;
using AndroidX.Core.App;
using TaskPlanner.Maui.Localization;

namespace TaskPlanner.Maui;

[BroadcastReceiver(Enabled = true, Exported = false)]
public sealed class DeadlineAlarmReceiver : BroadcastReceiver
{
    public const string ChannelId = "deadlines";

    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context is null || intent is null)
        {
            return;
        }

        var id = intent.GetIntExtra("id", 0);
        var title = intent.GetStringExtra("title") ?? "Дедлайн";
        var body = intent.GetStringExtra("body") ?? string.Empty;
        var manager = (NotificationManager?)context.GetSystemService(Context.NotificationService);
        if (manager is null)
        {
            return;
        }

        if (OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            var channel = new NotificationChannel(ChannelId, Localizer.Current["Reminder_Channel"], NotificationImportance.Default);
            manager.CreateNotificationChannel(channel);
        }

        var notification = new NotificationCompat.Builder(context, ChannelId)
            .SetContentTitle(title)!
            .SetContentText(body)!
            .SetSmallIcon(context.ApplicationInfo!.Icon)!
            .SetAutoCancel(true)!
            .Build();
        manager.Notify(id, notification);
    }
}
