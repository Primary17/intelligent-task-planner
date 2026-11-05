using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TaskPlanner.Domain.Entities;
using TaskPlanner.Domain.Enums;
using TaskPlanner.Infrastructure.Persistence;

namespace TaskPlanner.Tests.Database;

public sealed class DatabaseTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly SqliteAppDbContext _db;

    public DatabaseTests()
    {
        _connection.Open();
        _db = new SqliteAppDbContext(new DbContextOptionsBuilder<SqliteAppDbContext>().UseSqlite(_connection).Options);
        _db.Database.Migrate();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private User AddUser(string email = "a@test.local")
    {
        var user = new User { Id = Guid.NewGuid(), Email = email, DisplayName = "A", PasswordHash = "x", CreatedAtUtc = DateTime.UtcNow };
        _db.Users.Add(user);
        _db.SaveChanges();
        return user;
    }

    [Fact]
    public void Migrations_ApplyAndModelHasNoPendingChanges()
    {
        Assert.Empty(_db.Database.GetPendingMigrations());
    }

    [Fact]
    public void User_EmailIsUnique()
    {
        AddUser("same@test.local");

        Assert.Throws<DbUpdateException>(() => AddUser("same@test.local"));
    }

    [Fact]
    public void Discipline_NameIsUniquePerUserOnly()
    {
        var first = AddUser("one@test.local");
        var second = AddUser("two@test.local");
        _db.Disciplines.Add(new Discipline { Id = Guid.NewGuid(), UserId = first.Id, Name = "Math" });
        _db.Disciplines.Add(new Discipline { Id = Guid.NewGuid(), UserId = second.Id, Name = "Math" });
        _db.SaveChanges();

        _db.Disciplines.Add(new Discipline { Id = Guid.NewGuid(), UserId = first.Id, Name = "Math" });

        Assert.Throws<DbUpdateException>(() => _db.SaveChanges());
    }

    [Fact]
    public void AvailabilityException_OnePerUserAndDate()
    {
        var user = AddUser();
        var date = new DateOnly(2030, 1, 7);
        _db.AvailabilityExceptions.Add(new AvailabilityException { Id = Guid.NewGuid(), UserId = user.Id, Date = date, Kind = AvailabilityExceptionKind.Unavailable });
        _db.SaveChanges();

        _db.AvailabilityExceptions.Add(new AvailabilityException { Id = Guid.NewGuid(), UserId = user.Id, Date = date, Kind = AvailabilityExceptionKind.Unavailable });

        Assert.Throws<DbUpdateException>(() => _db.SaveChanges());
    }

    [Fact]
    public void DeletingUser_CascadesToAllOwnedData()
    {
        var user = AddUser();
        var discipline = new Discipline { Id = Guid.NewGuid(), UserId = user.Id, Name = "OS" };
        var task = new StudyTask { Id = Guid.NewGuid(), DisciplineId = discipline.Id, Title = "Lab", DeadlineUtc = DateTime.UtcNow.AddDays(1), EstimatedMinutes = 60 };
        _db.Disciplines.Add(discipline);
        _db.StudyTasks.Add(task);
        _db.StudySessions.Add(new StudySession { Id = Guid.NewGuid(), StudyTaskId = task.Id, StartAtUtc = DateTime.UtcNow, EndAtUtc = DateTime.UtcNow.AddHours(1), PlannedMinutes = 60 });
        _db.AvailabilityWindows.Add(new AvailabilityWindow { Id = Guid.NewGuid(), UserId = user.Id, DayOfWeek = DayOfWeek.Monday, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(10, 0) });
        _db.SaveChanges();

        _db.Users.Remove(user);
        _db.SaveChanges();

        Assert.Empty(_db.Disciplines);
        Assert.Empty(_db.StudyTasks);
        Assert.Empty(_db.StudySessions);
        Assert.Empty(_db.AvailabilityWindows);
    }

    [Fact]
    public void EnumsAndTimes_RoundTripThroughDatabase()
    {
        var user = AddUser();
        var discipline = new Discipline { Id = Guid.NewGuid(), UserId = user.Id, Name = "OS" };
        _db.Disciplines.Add(discipline);
        _db.StudyTasks.Add(new StudyTask { Id = Guid.NewGuid(), DisciplineId = discipline.Id, Title = "Lab", DeadlineUtc = DateTime.UtcNow.AddDays(1), EstimatedMinutes = 90, Priority = TaskPriority.Critical, Status = StudyTaskStatus.InProgress });
        _db.AvailabilityWindows.Add(new AvailabilityWindow { Id = Guid.NewGuid(), UserId = user.Id, DayOfWeek = DayOfWeek.Friday, StartTime = new TimeOnly(17, 30), EndTime = new TimeOnly(20, 0) });
        _db.SaveChanges();
        _db.ChangeTracker.Clear();

        var task = _db.StudyTasks.Single();
        var window = _db.AvailabilityWindows.Single();

        Assert.Equal(TaskPriority.Critical, task.Priority);
        Assert.Equal(StudyTaskStatus.InProgress, task.Status);
        Assert.Equal(new TimeOnly(17, 30), window.StartTime);
        Assert.Equal(DayOfWeek.Friday, window.DayOfWeek);
    }
}
