using Microsoft.EntityFrameworkCore;
using TaskPlanner.Domain.Entities;

namespace TaskPlanner.Infrastructure.Persistence;

public abstract class AppDbContextBase : DbContext
{
    protected AppDbContextBase(DbContextOptions options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Discipline> Disciplines => Set<Discipline>();
    public DbSet<StudyTask> StudyTasks => Set<StudyTask>();
    public DbSet<AvailabilityWindow> AvailabilityWindows => Set<AvailabilityWindow>();
    public DbSet<AvailabilityException> AvailabilityExceptions => Set<AvailabilityException>();
    public DbSet<StudySession> StudySessions => Set<StudySession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContextBase).Assembly);
    }
}
