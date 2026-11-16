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

    private async void OnSessionSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not UpcomingSessionRow row || BindingContext is not UpcomingViewModel vm)
        {
            return;
        }

        SessionCollection.SelectedItem = null;
        await vm.OpenDetailCommand.ExecuteAsync(row);
    }
}