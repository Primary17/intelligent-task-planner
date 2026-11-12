using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaskPlanner.Maui.Api;
using TaskPlanner.Maui.Localization;

namespace TaskPlanner.Maui.ViewModels;

public sealed record UpcomingSessionRow(string When, string Title, string DisciplineName, string Status);

public partial class UpcomingViewModel : ObservableObject
{
    private readonly TaskPlannerApiClient _api;

    [ObservableProperty] private bool _isBusy;

    [ObservableProperty] private string? _errorMessage;

    [ObservableProperty] private bool _isEmpty = true;

    public ObservableCollection<UpcomingSessionRow> Items { get; } = new();

    public UpcomingViewModel(TaskPlannerApiClient api)
    {
        _api = api;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        ErrorMessage = null;
        IsBusy = true;
        IsEmpty = false;
        try
        {
            var from = DateTime.UtcNow;
            var sessions = await _api.GetSessionsAsync(from, from.AddDays(7));
            Items.Clear();
            foreach (var session in sessions.OrderBy(item => item.StartAtUtc))
            {
                Items.Add(ToRow(session));
            }
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (HttpRequestException)
        {
            ErrorMessage = Localizer.Current["Common_NoConnection"];
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
            IsEmpty = Items.Count == 0 && string.IsNullOrEmpty(ErrorMessage);
        }
    }

    private static UpcomingSessionRow ToRow(StudySessionDto session)
    {
        var start = AsLocal(session.StartAtUtc);
        var end = AsLocal(session.EndAtUtc);
        var when = Localizer.Current.Format(
            "Upcoming_When",
            start.ToString("g", Localizer.Current.Culture),
            end.ToString("t", Localizer.Current.Culture));
        var statusKey = session.Status switch
        {
            SessionStatus.Completed => "Session_Completed",
            SessionStatus.Missed => "Session_Missed",
            _ => "Session_Planned"
        };
        return new UpcomingSessionRow(when, session.TaskTitle, session.DisciplineName, Localizer.Current[statusKey]);
    }

    private static DateTime AsLocal(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value.ToLocalTime(),
        DateTimeKind.Local => value,
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc).ToLocalTime()
    };
}
