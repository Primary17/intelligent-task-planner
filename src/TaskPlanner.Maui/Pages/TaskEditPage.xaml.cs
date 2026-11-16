using TaskPlanner.Maui.ViewModels;

namespace TaskPlanner.Maui.Pages;

public partial class TaskEditPage : ContentPage
{
    public TaskEditPage(TaskEditViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is TaskEditViewModel vm)
        {
            await vm.AppearingCommand.ExecuteAsync(null);
        }
    }
}
