using Microsoft.Extensions.DependencyInjection;
using TaskPlanner.Maui.ViewModels;

namespace TaskPlanner.Maui.Controls;

public partial class ApiConnectionCard : ContentView
{
    public ApiConnectionCard()
    {
        InitializeComponent();
        if (IPlatformApplication.Current?.Services.GetService<ApiConnectionViewModel>() is { } viewModel)
        {
            BindingContext = viewModel;
        }
    }
}
