using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaskPlanner.Maui.Api;
using TaskPlanner.Maui.Localization;
using TaskPlanner.Maui;

namespace TaskPlanner.Maui.ViewModels;

[QueryProperty(nameof(TaskId), "TaskId")]
public partial class TaskDetailViewModel : ObservableObject
{
    private readonly TaskPlannerApiClient _api;

    [ObservableProperty] private string? _taskId;

    [ObservableProperty] private StudyTaskDto? _item;

    [ObservableProperty] private bool _isBusy;

    [ObservableProperty] private string? _errorMessage;

    public TaskDetailViewModel(TaskPlannerApiClient api)
    {
        _api = api;
    }

    partial void OnTaskIdChanged(string? value)
    {
        if (Guid.TryParse(value, out var id))
        {
            _ = ReloadAsync(id);
        }
    }

    [RelayCommand]
    private Task RefreshAsync()
    {
        if (Guid.TryParse(TaskId, out var id))
        {
            return ReloadAsync(id);
        }

        return Task.CompletedTask;
    }

    private async Task ReloadAsync(Guid id)
    {
        ErrorMessage = null;
        IsBusy = true;
        try
        {
            Item = await _api.GetTaskAsync(id);
            if (Item is null)
            {
                ErrorMessage = Localizer.Current["Tasks_NotFound"];
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

    [RelayCommand]
    private async Task EditAsync()
    {
        if (Item is null)
        {
            return;
        }

        await Shell.Current.GoToAsync($"{MainShell.RouteTaskEdit}?TaskId={Item.Id}");
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (Item is null)
        {
            return;
        }

        var text = Localizer.Current;
        var confirm = await Shell.Current.DisplayAlert(
            text["Common_DeleteTitle"],
            text.Format("Tasks_DeleteBody", Item.Title),
            text["Common_Delete"],
            text["Common_Cancel"]);
        if (!confirm)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await _api.DeleteTaskAsync(Item.Id);
            await Shell.Current.GoToAsync("..");
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
