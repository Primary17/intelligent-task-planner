using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaskPlanner.Maui.Api;
using TaskPlanner.Maui.Localization;
using TaskPlanner.Maui.Services;
using TaskPlanner.Maui;

namespace TaskPlanner.Maui.ViewModels;

public partial class TasksViewModel : ObservableObject
{
    private readonly TaskPlannerApiClient _api;
    private readonly ReminderSyncService _reminderSync;

    [ObservableProperty] private bool _isBusy;

    [ObservableProperty] private string? _errorMessage;

    [ObservableProperty] private bool _isEmpty = true;

    public ObservableCollection<StudyTaskDto> Items { get; } = new();

    public TasksViewModel(TaskPlannerApiClient api, ReminderSyncService reminderSync)
    {
        _api = api;
        _reminderSync = reminderSync;
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
            var list = await _api.GetTasksAsync();
            Items.Clear();
            foreach (var t in list.OrderBy(x => x.DeadlineUtc))
            {
                Items.Add(t);
            }

            _ = _reminderSync.SyncIfPossibleAsync();
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

    [RelayCommand]
    private async Task OpenDetailAsync(StudyTaskDto? item)
    {
        if (item is null)
        {
            return;
        }

        await Shell.Current.GoToAsync($"{MainShell.RouteTaskDetail}?TaskId={item.Id}");
    }

    [RelayCommand]
    private async Task CreateNewAsync()
    {
        await Shell.Current.GoToAsync(MainShell.RouteTaskEdit);
    }
}
