using Microsoft.Extensions.DependencyInjection;
using TaskPlanner.Maui.Pages;

namespace TaskPlanner.Maui;

public partial class App : Application
{
    public App(IServiceProvider services)
    {
        InitializeComponent();
        MainPage = services.GetRequiredService<LoginPage>();
    }
}
