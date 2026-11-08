using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskPlanner.Application.Services;
using TaskPlanner.Infrastructure.Persistence;
using TaskPlanner.Infrastructure.Services;
using TaskPlanner.Infrastructure.Security;

namespace TaskPlanner.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IPasswordHasherService, PasswordHasherService>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<ICurrentUserContext, CurrentUserContext>();
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }

    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["Database:Provider"] ?? "Sqlite";

        if (string.Equals(provider, "PostgreSQL", StringComparison.OrdinalIgnoreCase))
        {
            var connectionString = configuration.GetConnectionString("PostgreSQL")
                ?? throw new InvalidOperationException("Connection string 'PostgreSQL' is missing.");

            services.AddDbContext<AppDbContextBase, PostgresAppDbContext>(options =>
                options.UseNpgsql(connectionString));
        }
        else
        {
            var connectionString = configuration.GetConnectionString("Sqlite")
                ?? "Data Source=Data/taskplanner.db";
            EnsureSqliteDirectory(connectionString);

            services.AddDbContext<AppDbContextBase, SqliteAppDbContext>(options =>
                options.UseSqlite(connectionString));
        }

        services.AddScoped<IStudyTaskService, StudyTaskService>();
        services.AddScoped<IDisciplineService, DisciplineService>();
        services.AddScoped<IAvailabilityService, AvailabilityService>();
        services.AddScoped<IStudySessionService, StudySessionService>();
        services.AddScoped<IPlanningService, PlanningService>();
        return services;
    }

    private static void EnsureSqliteDirectory(string connectionString)
    {
        const string prefix = "Data Source=";
        var start = connectionString.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (start < 0)
        {
            return;
        }

        var path = connectionString[(start + prefix.Length)..].Trim().Trim('"');
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
