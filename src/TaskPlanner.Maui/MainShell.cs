using TaskPlanner.Maui.Localization;
using TaskPlanner.Maui.Pages;
using TaskPlanner.Maui.Services;

namespace TaskPlanner.Maui;

/// <summary>
/// Головна оболонка після входу: вкладки + маршрути для деталей/редагування.
/// </summary>
public sealed class MainShell : Shell
{
    public const string RouteDisciplineDetail = "discipline-detail";
    public const string RouteDisciplineEdit = "discipline-edit";
    public const string RouteTaskDetail = "task-detail";
    public const string RouteTaskEdit = "task-edit";
    public const string RouteSessionDetail = "session-detail";

    private readonly ReminderSyncService _reminderSync;
    private readonly ShellContent _disciplines;
    private readonly ShellContent _tasks;
    private readonly ShellContent _upcoming;
    private readonly ShellContent _settings;

    public MainShell(
        ReminderSyncService reminderSync,
        DisciplinesPage disciplinesPage,
        TasksPage tasksPage,
        UpcomingPage upcomingPage,
        SettingsPage settingsPage)
    {
        _reminderSync = reminderSync;
        FlyoutBehavior = FlyoutBehavior.Disabled;

        _disciplines = new ShellContent
        {
            Content = disciplinesPage,
            Route = "disciplines"
        };
        _tasks = new ShellContent
        {
            Content = tasksPage,
            Route = "tasks"
        };
        _upcoming = new ShellContent
        {
            Content = upcomingPage,
            Route = "upcoming"
        };
        _settings = new ShellContent
        {
            Content = settingsPage,
            Route = "settings"
        };

        var tabBar = new TabBar();
        tabBar.Items.Add(_disciplines);
        tabBar.Items.Add(_tasks);
        tabBar.Items.Add(_upcoming);
        tabBar.Items.Add(_settings);
        Items.Add(tabBar);

        Routing.RegisterRoute(RouteDisciplineDetail, typeof(DisciplineDetailPage));
        Routing.RegisterRoute(RouteDisciplineEdit, typeof(DisciplineEditPage));
        Routing.RegisterRoute(RouteTaskDetail, typeof(TaskDetailPage));
        Routing.RegisterRoute(RouteTaskEdit, typeof(TaskEditPage));
        Routing.RegisterRoute(RouteSessionDetail, typeof(SessionDetailPage));

        Localizer.Current.LanguageChanged += (_, _) => ApplyTitles();
        ApplyTitles();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = _reminderSync.SyncIfPossibleAsync();
    }

    private void ApplyTitles()
    {
        var text = Localizer.Current;
        _disciplines.Title = text["Nav_Disciplines"];
        _tasks.Title = text["Nav_Tasks"];
        _upcoming.Title = text["Nav_Upcoming"];
        _settings.Title = text["Nav_Settings"];
    }
}
