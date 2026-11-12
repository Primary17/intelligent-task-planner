using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaskPlanner.Maui.Localization;
using TaskPlanner.Maui.Services;

namespace TaskPlanner.Maui.ViewModels;

public partial class ApiConnectionViewModel : ObservableObject
{
    private readonly AppSettings _settings;

    [ObservableProperty] private string _apiBaseUrl;

    [ObservableProperty] private string? _message;

    public ApiConnectionViewModel(AppSettings settings)
    {
        _settings = settings;
        _apiBaseUrl = settings.GetBaseUrl();
    }

    [RelayCommand]
    private void LoadDefaultsEmulator()
    {
        ApiBaseUrl = AppSettings.DefaultEmulatorBaseUrl;
        _settings.SetBaseUrl(ApiBaseUrl);
        Message = Localizer.Current["Settings_EmulatorSet"];
    }

    [RelayCommand]
    private void LoadDefaultsLocalhost()
    {
        ApiBaseUrl = AppSettings.DefaultLocalBaseUrl;
        _settings.SetBaseUrl(ApiBaseUrl);
        Message = Localizer.Current["Settings_LocalhostSet"];
    }

    [RelayCommand]
    private void Save()
    {
        _settings.SetBaseUrl(ApiBaseUrl);
        ApiBaseUrl = _settings.GetBaseUrl();
        Message = Localizer.Current["Settings_Saved"];
    }
}
