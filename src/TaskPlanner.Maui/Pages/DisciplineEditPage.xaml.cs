using TaskPlanner.Maui.ViewModels;

namespace TaskPlanner.Maui.Pages;

public partial class DisciplineEditPage : ContentPage
{
    public DisciplineEditPage(DisciplineEditViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
