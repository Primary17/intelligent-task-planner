using TaskPlanner.Maui.ViewModels;

namespace TaskPlanner.Maui.Pages;

public partial class DisciplineDetailPage : ContentPage
{
    private bool _firstAppear = true;

    public DisciplineDetailPage(DisciplineDetailViewModel viewModel)
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

        if (BindingContext is DisciplineDetailViewModel vm)
        {
            await vm.RefreshCommand.ExecuteAsync(null);
        }
    }
}
