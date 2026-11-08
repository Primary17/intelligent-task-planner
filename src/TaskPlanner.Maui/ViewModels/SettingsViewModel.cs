using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaskPlanner.Maui.Services;
using Microsoft.Extensions.DependencyInjection;

namespace TaskPlanner.Maui.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IAuthTokenStore _tokens;
    private readonly AppSettings _settings;
    private readonly IServiceProvider _services;

    [ObservableProperty] private string _apiBaseUrl = "";

    [ObservableProperty] private string? _message;

    public SettingsViewModel(IAuthTokenStore tokens, AppSettings settings, IServiceProvider services)
    {
        _tokens = tokens;
        _settings = settings;
        _services = services;
        ApiBaseUrl = _settings.GetBaseUrl();
    }

    [RelayCommand]
    private void LoadDefaultsEmulator()
    {
        ApiBaseUrl = AppSettings.DefaultEmulatorBaseUrl;
        Message = "Встановлено URL для Emulator / VM (10.0.2.2).";
    }

    [RelayCommand]
    private void LoadDefaultsLocalhost()
    {
        ApiBaseUrl = AppSettings.DefaultLocalBaseUrl;
        Message = "Встановлено http://localhost:5109/ (Windows / physical device у тій же мережі).";
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        _settings.SetBaseUrl(ApiBaseUrl);
        Message = "Збережено. Наступний запит піде на цю адресу.";
        await Task.CompletedTask;
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
}
