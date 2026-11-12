using TaskPlanner.Maui.Localization;
using TaskPlanner.Maui.ViewModels;

namespace TaskPlanner.Maui.Pages;

public partial class LoginPage : ContentPage
{
    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        Localizer.Current.LanguageChanged += (_, _) => PaintLanguages();
        PaintLanguages();
    }

    private void PaintLanguages()
    {
        var ukrainian = Localizer.Current.Language == Localizer.Ukrainian;
        Paint(UkrainianButton, ukrainian);
        Paint(EnglishButton, !ukrainian);
    }

    private static void Paint(Button button, bool selected)
    {
        button.BackgroundColor = selected ? Color.FromArgb("#0F766E") : Colors.Transparent;
        button.TextColor = selected ? Colors.White : Color.FromArgb("#134E4A");
        button.BorderColor = Color.FromArgb("#0F766E");
        button.BorderWidth = selected ? 0 : 1.5;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is LoginViewModel vm)
        {
            await vm.TryNavigateIfAlreadyAuthenticatedAsync();
        }
    }

    private void OnLanguage(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: string code })
        {
            Localizer.Current.SetLanguage(code);
        }
    }
}
