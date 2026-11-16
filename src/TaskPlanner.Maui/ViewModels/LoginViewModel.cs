using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using TaskPlanner.Maui.Api;
using TaskPlanner.Maui.Localization;
using TaskPlanner.Maui.Services;

namespace TaskPlanner.Maui.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly TaskPlannerApiClient _api;
    private readonly IAuthTokenStore _tokens;
    private readonly ReminderSyncService _reminderSync;
    private readonly IServiceProvider _services;

    [ObservableProperty] private string _email = "";

    [ObservableProperty] private string _password = "";

    [ObservableProperty] private string _displayName = "";

    [ObservableProperty] private bool _isBusy;

    [ObservableProperty] private string? _errorMessage;

    public LoginViewModel(
        TaskPlannerApiClient api,
        IAuthTokenStore tokens,
        ReminderSyncService reminderSync,
        IServiceProvider services)
    {
        _api = api;
        _tokens = tokens;
        _reminderSync = reminderSync;
        _services = services;
    }

    public async Task TryNavigateIfAlreadyAuthenticatedAsync()
    {
        var existing = await _tokens.GetTokenAsync();
        if (string.IsNullOrWhiteSpace(existing))
        {
            return;
        }

        try
        {
            var me = await _api.MeAsync();
            if (me is not null)
            {
                OpenMainApp();
            }
        }
        catch
        {
            await _tokens.ClearAsync();
        }
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        await RunAuthAsync(
            async () =>
            {
                var response = await _api.LoginAsync(new LoginRequest(Email.Trim(), Password));
                return response!;
            });
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        await RunAuthAsync(
            async () =>
            {
                var response = await _api.RegisterAsync(new RegisterRequest(
                    Email.Trim(),
                    DisplayName.Trim(),
                    Password));
                return response!;
            });
    }

    private async Task RunAuthAsync(Func<Task<AuthResponse>> authCall)
    {
        ErrorMessage = null;
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = Localizer.Current["Login_NeedCredentials"];
            return;
        }

        IsBusy = true;
        try
        {
            var response = await authCall();
            await _tokens.SetTokenAsync(response.AccessToken);
            OpenMainApp();
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (HttpRequestException)
        {
            ErrorMessage = Localizer.Current["Login_NoConnection"];
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
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

    private void OpenMainApp()
    {
        if (Application.Current is null)
        {
            return;
        }

        Application.Current.MainPage = _services.GetRequiredService<MainShell>();
        _ = _reminderSync.SyncIfPossibleAsync();
    }
}
