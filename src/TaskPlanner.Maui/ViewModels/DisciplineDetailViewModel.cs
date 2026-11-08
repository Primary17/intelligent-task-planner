using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaskPlanner.Maui.Api;
using TaskPlanner.Maui;

namespace TaskPlanner.Maui.ViewModels;

[QueryProperty(nameof(DisciplineId), "DisciplineId")]
public partial class DisciplineDetailViewModel : ObservableObject
{
    private readonly TaskPlannerApiClient _api;

    [ObservableProperty] private string? _disciplineId;

    [ObservableProperty] private DisciplineDto? _item;

    [ObservableProperty] private bool _isBusy;

    [ObservableProperty] private string? _errorMessage;

    public DisciplineDetailViewModel(TaskPlannerApiClient api)
    {
        _api = api;
    }

    partial void OnDisciplineIdChanged(string? value)
    {
        if (Guid.TryParse(value, out var id))
        {
            _ = LoadAsync(id);
        }
    }

    [RelayCommand]
    private async Task LoadAsync(Guid id)
    {
        ErrorMessage = null;
        IsBusy = true;
        try
        {
            Item = await _api.GetDisciplineAsync(id);
            if (Item is null)
            {
                ErrorMessage = "Дисципліну не знайдено.";
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

        await Shell.Current.GoToAsync($"{MainShell.RouteDisciplineEdit}?DisciplineId={Item.Id}");
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (Item is null)
        {
            return;
        }

        var confirm = await Shell.Current.DisplayAlert(
            "Видалити?",
            $"Видалити дисципліну «{Item.Name}»? Пов’язані завдання стануть недоступні.",
            "Видалити",
            "Скасувати");
        if (!confirm)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await _api.DeleteDisciplineAsync(Item.Id);
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
