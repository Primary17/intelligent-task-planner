using TaskPlanner.Maui.Pages;
using TaskPlanner.Maui.Services;

namespace TaskPlanner.Maui;

public partial class App : Application
{
    private readonly IServiceProvider _services;

    public App(IServiceProvider services)
    {
        _services = services;
        InitializeComponent();
        MainPage = services.GetRequiredService<LoginPage>();
    }

    protected override void OnResume()
    {
        base.OnResume();
        if (MainPage is MainShell)
        {
            _ = _services.GetRequiredService<ReminderSyncService>().SyncIfPossibleAsync();
        }
    }
}