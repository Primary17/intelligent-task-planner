using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaskPlanner.Maui.Api;
using TaskPlanner.Maui.Localization;
using TaskPlanner.Maui;

namespace TaskPlanner.Maui.ViewModels;

public partial class DisciplinesViewModel : ObservableObject
{
    private readonly TaskPlannerApiClient _api;

    [ObservableProperty] private bool _isBusy;

    [ObservableProperty] private string? _errorMessage;

    [ObservableProperty] private bool _isEmpty = true;

    public ObservableCollection<DisciplineDto> Items { get; } = new();

    public DisciplinesViewModel(TaskPlannerApiClient api)
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
            var list = await _api.GetDisciplinesAsync();
            Items.Clear();
            foreach (var d in list.OrderBy(x => x.Name))
            {
                Items.Add(d);
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

    [RelayCommand]
    private async Task OpenDetailAsync(DisciplineDto? item)
    {
        if (item is null)
        {
            return;
        }

        await Shell.Current.GoToAsync($"{MainShell.RouteDisciplineDetail}?DisciplineId={item.Id}");
    }

    [RelayCommand]
    private async Task CreateNewAsync()
    {
        await Shell.Current.GoToAsync(MainShell.RouteDisciplineEdit);
    }
}
