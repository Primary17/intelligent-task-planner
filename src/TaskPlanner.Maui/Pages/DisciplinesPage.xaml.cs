using TaskPlanner.Maui.ViewModels;

namespace TaskPlanner.Maui.Pages;

public partial class DisciplinesPage : ContentPage
{
    public DisciplinesPage(DisciplinesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is DisciplinesViewModel vm)
        {
            await vm.LoadCommand.ExecuteAsync(null);
        }
    }
}
