using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using TaskPlanner.Maui.Localization;
using TaskPlanner.Maui.Services;

namespace TaskPlanner.Maui.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IAuthTokenStore _tokens;
    private readonly NotificationPreferences _notifications;
    private readonly IServiceProvider _services;
    private bool _loading = true;

    [ObservableProperty] private bool _isUkrainian;

    [ObservableProperty] private bool _isEnglish;

    [ObservableProperty] private bool _remindHourBefore;

    [ObservableProperty] private bool _remindAtDeadline;

    [ObservableProperty] private bool _remindStudyStart;

    [ObservableProperty] private bool _morningSummary;

    [ObservableProperty] private TimeSpan _morningTime;

    public SettingsViewModel(
        IAuthTokenStore tokens,
        NotificationPreferences notifications,
        IServiceProvider services)
    {
        _tokens = tokens;
        _notifications = notifications;
        _services = services;
        RemindHourBefore = _notifications.RemindHourBefore;
        RemindAtDeadline = _notifications.RemindAtDeadline;
        RemindStudyStart = _notifications.RemindStudyStart;
        MorningSummary = _notifications.MorningSummary;
        MorningTime = _notifications.MorningTime;
        ApplyLanguageFlags();
        _loading = false;
        Localizer.Current.LanguageChanged += (_, _) => ApplyLanguageFlags();
    }

    partial void OnIsUkrainianChanged(bool value)
    {
        if (_loading || !value)
        {
            return;
        }

        Localizer.Current.SetLanguage(Localizer.Ukrainian);
    }

    partial void OnIsEnglishChanged(bool value)
    {
        if (_loading || !value)
        {
            return;
        }

        Localizer.Current.SetLanguage(Localizer.English);
    }

    partial void OnRemindHourBeforeChanged(bool value)
    {
        if (!_loading)
        {
            _notifications.RemindHourBefore = value;
        }
    }

    partial void OnRemindAtDeadlineChanged(bool value)
    {
        if (!_loading)
        {
            _notifications.RemindAtDeadline = value;
        }
    }

    partial void OnRemindStudyStartChanged(bool value)
    {
        if (!_loading)
        {
            _notifications.RemindStudyStart = value;
        }
    }

    partial void OnMorningSummaryChanged(bool value)
    {
        if (!_loading)
        {
            _notifications.MorningSummary = value;
        }
    }

    partial void OnMorningTimeChanged(TimeSpan value)
    {
        if (!_loading)
        {
            _notifications.MorningTime = value;
        }
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        await _tokens.ClearAsync();
        if (Application.Current is not null)
        {
            Application.Current.MainPage = _services.GetRequiredService<Pages.LoginPage>();
        }
    }

    private void ApplyLanguageFlags()
    {
        _loading = true;
        IsUkrainian = Localizer.Current.Language == Localizer.Ukrainian;
        IsEnglish = Localizer.Current.Language == Localizer.English;
        _loading = false;
    }
}
