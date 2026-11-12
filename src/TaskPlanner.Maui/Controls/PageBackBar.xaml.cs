namespace TaskPlanner.Maui.Controls;

public partial class PageBackBar : ContentView
{
    public PageBackBar()
    {
        InitializeComponent();
    }

    private async void OnBack(object? sender, EventArgs e)
    {
        if (Shell.Current is not null)
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
