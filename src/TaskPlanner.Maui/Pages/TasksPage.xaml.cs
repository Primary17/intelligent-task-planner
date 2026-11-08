using TaskPlanner.Maui.Api;
using TaskPlanner.Maui.ViewModels;

namespace TaskPlanner.Maui.Pages;

public partial class TasksPage : ContentPage
{
    public TasksPage(TasksViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is TasksViewModel vm)
        {
            await vm.LoadCommand.ExecuteAsync(null);
        }
    }

    private async void OnTaskSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not StudyTaskDto task || BindingContext is not TasksViewModel vm)
        {
            return;
        }

        TaskCollection.SelectedItem = null;
        await vm.OpenDetailCommand.ExecuteAsync(task);
    }
}
