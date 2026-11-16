using TaskPlanner.Maui.ViewModels;

namespace TaskPlanner.Maui.Pages;

public partial class TaskDetailPage : ContentPage
{
    private bool _firstAppear = true;

    public TaskDetailPage(TaskDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_firstAppear)
        {
            _firstAppear = false;
            return;
        }

        if (BindingContext is TaskDetailViewModel vm)
        {
            await vm.RefreshCommand.ExecuteAsync(null);
        }
    }
}
