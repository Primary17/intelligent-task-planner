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

    public MainShell(
        DisciplinesPage disciplinesPage,
        TasksPage tasksPage,
        SettingsPage settingsPage)
    {
        var tabBar = new TabBar();
        tabBar.Items.Add(new ShellContent
        {
            Title = "Дисципліни",
            Content = disciplinesPage,
            Route = "disciplines"
        });
        tabBar.Items.Add(new ShellContent
        {
            Title = "Завдання",
            Content = tasksPage,
            Route = "tasks"
        });
        tabBar.Items.Add(new ShellContent
        {
            Title = "Налаштування",
            Content = settingsPage,
            Route = "settings"
        });
        Items.Add(tabBar);

        Routing.RegisterRoute(RouteDisciplineDetail, typeof(DisciplineDetailPage));
        Routing.RegisterRoute(RouteDisciplineEdit, typeof(DisciplineEditPage));
        Routing.RegisterRoute(RouteTaskDetail, typeof(TaskDetailPage));
        Routing.RegisterRoute(RouteTaskEdit, typeof(TaskEditPage));
    }
}
