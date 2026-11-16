using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaskPlanner.Maui.Api;
using TaskPlanner.Maui.Localization;

namespace TaskPlanner.Maui.ViewModels;

[QueryProperty(nameof(DisciplineId), "DisciplineId")]
public partial class DisciplineEditViewModel : ObservableObject
{
    private readonly TaskPlannerApiClient _api;

    [ObservableProperty] private string? _disciplineId;

    [ObservableProperty] private string _name = "";

    [ObservableProperty] private string _description = "";

    [ObservableProperty] private string _color = "#369AE0";

    [ObservableProperty] private bool _isBusy;

    [ObservableProperty] private string? _errorMessage;

    [ObservableProperty] private string _title = "";

    private bool _editing;

    public DisciplineEditViewModel(TaskPlannerApiClient api)
    {
        _api = api;
        ApplyTitle();
        Localizer.Current.LanguageChanged += (_, _) => ApplyTitle();
    }

    private void ApplyTitle() => Title = Localizer.Current[_editing ? "Tasks_Editing" : "Disciplines_New"];

    partial void OnDisciplineIdChanged(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            _editing = false;
            ApplyTitle();
            Name = "";
            Description = "";
            Color = "#369AE0";
            return;
        }

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
            var d = await _api.GetDisciplineAsync(id);
            if (d is null)
            {
                ErrorMessage = Localizer.Current["Common_NotFound"];
                return;
            }

            _editing = true;
            ApplyTitle();
            Name = d.Name;
            Description = d.Description ?? "";
            Color = d.Color;
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

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = null;
        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = Localizer.Current["Common_NeedTitle"];
            return;
        }

        IsBusy = true;
        try
        {
            if (string.IsNullOrEmpty(DisciplineId) || !Guid.TryParse(DisciplineId, out var id))
            {
                var created = await _api.CreateDisciplineAsync(new CreateDisciplineRequest(
                    Name.Trim(),
                    string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
                    string.IsNullOrWhiteSpace(Color) ? "#369AE0" : Color.Trim()));
                await Shell.Current.GoToAsync($"..");
            }
            else
            {
                await _api.UpdateDisciplineAsync(
                    id,
                    new UpdateDisciplineRequest(
                        Name.Trim(),
                        string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
                        string.IsNullOrWhiteSpace(Color) ? "#369AE0" : Color.Trim()));
                await Shell.Current.GoToAsync($"..");
            }
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

    [RelayCommand]
    private Task CancelAsync() => Shell.Current.GoToAsync("..");
}
