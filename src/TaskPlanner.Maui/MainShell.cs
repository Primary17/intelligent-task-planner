using TaskPlanner.Maui.Localization;
using TaskPlanner.Maui.Pages;

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

    private readonly ShellContent _disciplines;
    private readonly ShellContent _tasks;
    private readonly ShellContent _settings;

    public MainShell(
        DisciplinesPage disciplinesPage,
        TasksPage tasksPage,
        SettingsPage settingsPage)
    {
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
        _settings = new ShellContent
        {
            Content = settingsPage,
            Route = "settings"
        };

        var tabBar = new TabBar();
        tabBar.Items.Add(_disciplines);
        tabBar.Items.Add(_tasks);
        tabBar.Items.Add(_settings);
        Items.Add(tabBar);

        Routing.RegisterRoute(RouteDisciplineDetail, typeof(DisciplineDetailPage));
        Routing.RegisterRoute(RouteDisciplineEdit, typeof(DisciplineEditPage));
        Routing.RegisterRoute(RouteTaskDetail, typeof(TaskDetailPage));
        Routing.RegisterRoute(RouteTaskEdit, typeof(TaskEditPage));

        Localizer.Current.LanguageChanged += (_, _) => ApplyTitles();
        ApplyTitles();
    }

    private void ApplyTitles()
    {
        var text = Localizer.Current;
        _disciplines.Title = text["Nav_Disciplines"];
        _tasks.Title = text["Nav_Tasks"];
        _settings.Title = text["Nav_Settings"];
    }
}
