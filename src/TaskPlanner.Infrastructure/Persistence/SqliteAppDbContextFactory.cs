using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace TaskPlanner.Infrastructure.Persistence;

public sealed class SqliteAppDbContextFactory : IDesignTimeDbContextFactory<SqliteAppDbContext>
{
    public SqliteAppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SqliteAppDbContext>()
            .UseSqlite(ConfigurationHelper.GetConnectionString("Sqlite"))
            .Options;

        return new SqliteAppDbContext(options);
    }
}

internal static class ConfigurationHelper
{
    public static string GetConnectionString(string name)
    {
        var apiPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "TaskPlanner.Api"));
        if (!Directory.Exists(apiPath))
        {
            apiPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "src", "TaskPlanner.Api"));
        }

        var configuration = new ConfigurationBuilder()
            .SetBasePath(apiPath)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        return configuration.GetConnectionString(name)
               ?? throw new InvalidOperationException($"Connection string '{name}' is missing.");
    }
}
