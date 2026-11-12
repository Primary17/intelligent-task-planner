using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaskPlanner.Maui.Api;
using TaskPlanner.Maui.Localization;

namespace TaskPlanner.Maui.ViewModels;

[QueryProperty(nameof(TaskId), "TaskId")]
public partial class TaskEditViewModel : ObservableObject
{
    private readonly TaskPlannerApiClient _api;

    public IReadOnlyList<TaskPriority> PriorityOptions { get; } = Enum.GetValues<TaskPriority>().ToArray();

    public IReadOnlyList<StudyTaskStatus> StatusOptions { get; } = Enum.GetValues<StudyTaskStatus>().ToArray();

    [ObservableProperty] private string? _taskId;

    [ObservableProperty] private Guid _disciplineId;

    [ObservableProperty] private DisciplineDto? _selectedDiscipline;

    [ObservableProperty] private string _title = "";

    [ObservableProperty] private DateTime _deadlineDate = DateTime.Today.AddDays(7);

    [ObservableProperty] private TimeSpan _deadlineTime = TimeSpan.FromHours(18);

    [ObservableProperty] private int _complexity = 3;

    [ObservableProperty] private TaskPriority _priority = TaskPriority.Medium;

    [ObservableProperty] private int _estimatedMinutes = 60;

    [ObservableProperty] private int _actualMinutes;

    [ObservableProperty] private StudyTaskStatus _status = StudyTaskStatus.NotStarted;

    [ObservableProperty] private bool _isBusy;

    [ObservableProperty] private string? _errorMessage;

    [ObservableProperty] private string _header = "";

    private bool _editing;

    [ObservableProperty] private IReadOnlyList<DisciplineDto> _disciplines = Array.Empty<DisciplineDto>();

    public TaskEditViewModel(TaskPlannerApiClient api)
    {
        _api = api;
        ApplyHeader();
        Localizer.Current.LanguageChanged += (_, _) => ApplyHeader();
    }

    private void ApplyHeader() => Header = Localizer.Current[_editing ? "Tasks_Editing" : "Tasks_New"];

    partial void OnSelectedDisciplineChanged(DisciplineDto? value)
    {
        if (value is not null)
        {
            DisciplineId = value.Id;
        }
    }

    partial void OnTaskIdChanged(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            _editing = false;
            ApplyHeader();
            return;
        }

        if (Guid.TryParse(value, out var id))
        {
            _ = LoadAsync(id);
        }
    }

    [RelayCommand]
    private async Task AppearingAsync()
    {
        ErrorMessage = null;
        IsBusy = true;
        try
        {
            var list = (await _api.GetDisciplinesAsync()).OrderBy(x => x.Name).ToList();
            Disciplines = list;

            SelectedDiscipline = DisciplineId != Guid.Empty
                ? list.FirstOrDefault(x => x.Id == DisciplineId)
                : list.FirstOrDefault();
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
    private async Task LoadAsync(Guid id)
    {
        await AppearingAsync();

        ErrorMessage = null;
        IsBusy = true;
        try
        {
            var t = await _api.GetTaskAsync(id);
            if (t is null)
            {
                ErrorMessage = Localizer.Current["Common_NotFound"];
                return;
            }

            _editing = true;
            ApplyHeader();
            DisciplineId = t.DisciplineId;
            SelectedDiscipline = Disciplines.FirstOrDefault(x => x.Id == t.DisciplineId);
            Title = t.Title;
            var localDeadline = t.DeadlineUtc.ToLocalTime();
            DeadlineDate = localDeadline.Date;
            DeadlineTime = localDeadline.TimeOfDay;
            Complexity = t.Complexity;
            Priority = t.Priority;
            EstimatedMinutes = t.EstimatedMinutes;
            ActualMinutes = t.ActualMinutes;
            Status = t.Status;
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
        if (string.IsNullOrWhiteSpace(Title))
        {
            ErrorMessage = Localizer.Current["Common_NeedTitle"];
            return;
        }

        if (DisciplineId == Guid.Empty)
        {
            ErrorMessage = Localizer.Current["Tasks_NeedDiscipline"];
            return;
        }

        var localDeadline = DeadlineDate.Date.Add(DeadlineTime);
        var deadlineUtc = DateTime.SpecifyKind(localDeadline, DateTimeKind.Local).ToUniversalTime();

        IsBusy = true;
        try
        {
            if (string.IsNullOrEmpty(TaskId) || !Guid.TryParse(TaskId, out var id))
            {
                await _api.CreateTaskAsync(new CreateStudyTaskRequest(
                    DisciplineId,
                    Title.Trim(),
                    deadlineUtc,
                    Complexity,
                    Priority,
                    EstimatedMinutes,
                    Status));
            }
            else
            {
                await _api.UpdateTaskAsync(
                    id,
                    new UpdateStudyTaskRequest(
                        Title.Trim(),
                        deadlineUtc,
                        Complexity,
                        Priority,
                        EstimatedMinutes,
                        ActualMinutes,
                        Status));
            }

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

    [RelayCommand]
    private Task CancelAsync() => Shell.Current.GoToAsync("..");
}
