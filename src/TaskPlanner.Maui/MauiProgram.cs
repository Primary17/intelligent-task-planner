using Microsoft.Extensions.Logging;
using TaskPlanner.Maui.Api;
using TaskPlanner.Maui.Pages;
using TaskPlanner.Maui.Services;
using TaskPlanner.Maui.ViewModels;

namespace TaskPlanner.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton<AppSettings>();
        builder.Services.AddSingleton<NotificationPreferences>();
        builder.Services.AddSingleton<IAuthTokenStore, AuthTokenStore>();
#if ANDROID
        builder.Services.AddSingleton<IDeadlineReminderService, AndroidDeadlineReminderService>();
#elif IOS
        builder.Services.AddSingleton<IDeadlineReminderService, IosDeadlineReminderService>();
#else
        builder.Services.AddSingleton<IDeadlineReminderService, NoOpDeadlineReminderService>();
#endif

        builder.Services.AddTransient<AuthHeaderHandler>();
        builder.Services.AddTransient<DynamicBaseAddressHandler>();
        builder.Services.AddHttpClient<TaskPlannerApiClient>((sp, client) =>
            {
                client.Timeout = TimeSpan.FromSeconds(60);
                // HttpClient відхиляє відносний URI до того, як спрацює DelegatingHandler.
                client.BaseAddress = new Uri(sp.GetRequiredService<AppSettings>().GetBaseUrl());
            })
            .AddHttpMessageHandler<AuthHeaderHandler>()
            .AddHttpMessageHandler<DynamicBaseAddressHandler>();

        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<DisciplinesViewModel>();
        builder.Services.AddTransient<DisciplineDetailViewModel>();
        builder.Services.AddTransient<DisciplineEditViewModel>();
        builder.Services.AddTransient<TasksViewModel>();
        builder.Services.AddTransient<TaskDetailViewModel>();
        builder.Services.AddTransient<TaskEditViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<ApiConnectionViewModel>();

        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<DisciplinesPage>();
        builder.Services.AddTransient<DisciplineDetailPage>();
        builder.Services.AddTransient<DisciplineEditPage>();
        builder.Services.AddTransient<TasksPage>();
        builder.Services.AddTransient<TaskDetailPage>();
        builder.Services.AddTransient<TaskEditPage>();
        builder.Services.AddTransient<SettingsPage>();

        builder.Services.AddTransient<MainShell>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
