using TaskPlanner.Maui.ViewModels;

namespace TaskPlanner.Maui.Pages;

public partial class UpcomingPage : ContentPage
{
    public UpcomingPage(UpcomingViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is UpcomingViewModel viewModel)
        {
            await viewModel.LoadCommand.ExecuteAsync(null);
        }
    }
}
