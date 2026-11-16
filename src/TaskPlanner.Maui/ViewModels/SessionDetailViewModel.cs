using CommunityToolkit.Mvvm.ComponentModel;
using TaskPlanner.Maui.Api;
using TaskPlanner.Maui.Localization;

namespace TaskPlanner.Maui.ViewModels;

[QueryProperty(nameof(SessionId), "SessionId")]
public partial class SessionDetailViewModel : ObservableObject
{
    private readonly TaskPlannerApiClient _api;

    [ObservableProperty] private string? _sessionId;

    [ObservableProperty] private StudySessionDto? _item;

    [ObservableProperty] private bool _isBusy;

    [ObservableProperty] private string? _errorMessage;

    public SessionDetailViewModel(TaskPlannerApiClient api)
    {
        _api = api;
    }

    partial void OnSessionIdChanged(string? value)
    {
        if (Guid.TryParse(value, out var id))
        {
            _ = LoadAsync(id);
        }
    }

    private async Task LoadAsync(Guid id)
    {
        ErrorMessage = null;
        IsBusy = true;
        try
        {
            Item = await _api.GetSessionAsync(id);
            if (Item is null)
            {
                ErrorMessage = Localizer.Current["Session_NotFound"];
            }
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
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
}