using Microsoft.EntityFrameworkCore;

namespace TaskPlanner.Infrastructure.Persistence;

public sealed class SqliteAppDbContext : AppDbContextBase
{
    public SqliteAppDbContext(DbContextOptions<SqliteAppDbContext> options) : base(options)
    {
    }
}
