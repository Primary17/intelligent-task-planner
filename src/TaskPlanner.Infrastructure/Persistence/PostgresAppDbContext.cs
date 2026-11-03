using Microsoft.EntityFrameworkCore;

namespace TaskPlanner.Infrastructure.Persistence;

public sealed class PostgresAppDbContext : AppDbContextBase
{
    public PostgresAppDbContext(DbContextOptions<PostgresAppDbContext> options) : base(options)
    {
    }
}
