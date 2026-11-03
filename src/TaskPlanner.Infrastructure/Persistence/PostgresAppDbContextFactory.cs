using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TaskPlanner.Infrastructure.Persistence;

public sealed class PostgresAppDbContextFactory : IDesignTimeDbContextFactory<PostgresAppDbContext>
{
    public PostgresAppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PostgresAppDbContext>()
            .UseNpgsql(ConfigurationHelper.GetConnectionString("PostgreSQL"))
            .Options;

        return new PostgresAppDbContext(options);
    }
}
