namespace TaskPlanner.Maui.Services;

public sealed class NotificationPreferences
{
    private const string HourKey = "notify_hour_before";
    private const string AtKey = "notify_at_deadline";
    private const string StudyStartKey = "notify_study_start";
    private const string MorningKey = "notify_morning_summary";
    private const string MorningMinutesKey = "notify_morning_minutes";

    public bool RemindHourBefore
    {
        get => Preferences.Default.Get(HourKey, true);
        set => Preferences.Default.Set(HourKey, value);
    }

    public bool RemindAtDeadline
    {
        get => Preferences.Default.Get(AtKey, true);
        set => Preferences.Default.Set(AtKey, value);
    }

    public bool RemindStudyStart
    {
        get => Preferences.Default.Get(StudyStartKey, true);
        set => Preferences.Default.Set(StudyStartKey, value);
    }

    public bool MorningSummary
    {
        get => Preferences.Default.Get(MorningKey, true);
        set => Preferences.Default.Set(MorningKey, value);
    }

    public TimeSpan MorningTime
    {
        get => TimeSpan.FromMinutes(Preferences.Default.Get(MorningMinutesKey, 8 * 60));
        set => Preferences.Default.Set(MorningMinutesKey, (int)value.TotalMinutes);
    }
}
