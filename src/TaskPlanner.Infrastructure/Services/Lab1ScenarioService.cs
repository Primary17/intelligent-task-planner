using Microsoft.EntityFrameworkCore;
using TaskPlanner.Domain.Entities;
using TaskPlanner.Domain.Enums;
using TaskPlanner.Infrastructure.Persistence;
using TaskPlanner.Infrastructure.Security;

namespace TaskPlanner.Infrastructure.Services;

public sealed class Lab1ScenarioService
{
    public const string DemoEmail = "demo@taskplanner.local";
    public const string DemoPassword = "Demo1234!";

    private readonly AppDbContextBase _db;
    private readonly IPasswordHasherService _passwordHasher;

    public Lab1ScenarioService(AppDbContextBase db, IPasswordHasherService passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task<Lab1DemoReport> RunAsync(CancellationToken cancellationToken = default)
    {
        var provider = _db.Database.ProviderName ?? "unknown";
        await ResetDemoUserAsync(cancellationToken);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = DemoEmail,
            DisplayName = "Демонстраційний студент",
            Role = UserRole.User,
            CreatedAtUtc = DateTime.UtcNow
        };
        user.PasswordHash = _passwordHasher.Hash(user, DemoPassword);
        _db.Users.Add(user);

        var os = new Discipline
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Name = "Операційні системи",
            Description = "Процеси, потоки, файлові системи",
            Color = "#2563EB"
        };
        var mpp = new Discipline
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Name = "Мультиплатформне програмування",
            Description = "EF Core, ASP.NET Core, клієнти",
            Color = "#16A34A"
        };
        _db.Disciplines.AddRange(os, mpp);

        var lab1 = new StudyTask
        {
            Id = Guid.NewGuid(),
            DisciplineId = mpp.Id,
            Title = "Лабораторна робота №1",
            DeadlineUtc = DateTime.UtcNow.AddDays(5),
            Complexity = 3,
            Priority = TaskPriority.High,
            EstimatedMinutes = 360,
            ActualMinutes = 120,
            Status = StudyTaskStatus.InProgress
        };
        var lab2 = new StudyTask
        {
            Id = Guid.NewGuid(),
            DisciplineId = mpp.Id,
            Title = "Лабораторна робота №2",
            DeadlineUtc = DateTime.UtcNow.AddDays(12),
            Complexity = 4,
            Priority = TaskPriority.Medium,
            EstimatedMinutes = 300,
            Status = StudyTaskStatus.NotStarted
        };
        var kernel = new StudyTask
        {
            Id = Guid.NewGuid(),
            DisciplineId = os.Id,
            Title = "STEM-проєкт",
            DeadlineUtc = DateTime.UtcNow.AddDays(3),
            Complexity = 5,
            Priority = TaskPriority.Critical,
            EstimatedMinutes = 240,
            Status = StudyTaskStatus.NotStarted
        };
        _db.StudyTasks.AddRange(lab1, lab2, kernel);

        _db.AvailabilityWindows.AddRange(
            Window(user.Id, DayOfWeek.Monday, 17, 20),
            Window(user.Id, DayOfWeek.Tuesday, 18, 22),
            Window(user.Id, DayOfWeek.Wednesday, 16, 19),
            Window(user.Id, DayOfWeek.Thursday, 18, 21),
            Window(user.Id, DayOfWeek.Friday, 17, 20));

        _db.AvailabilityExceptions.Add(new AvailabilityException
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(2)),
            Kind = AvailabilityExceptionKind.Unavailable,
            Reason = "Додаткові заняття"
        });

        var session = new StudySession
        {
            Id = Guid.NewGuid(),
            StudyTaskId = lab1.Id,
            StartAtUtc = DateTime.UtcNow.Date.AddHours(17),
            EndAtUtc = DateTime.UtcNow.Date.AddHours(19),
            PlannedMinutes = 120,
            ActualMinutes = 120,
            Status = SessionStatus.Completed,
            IsLocked = true
        };
        var planned = new StudySession
        {
            Id = Guid.NewGuid(),
            StudyTaskId = kernel.Id,
            StartAtUtc = DateTime.UtcNow.Date.AddDays(1).AddHours(18),
            EndAtUtc = DateTime.UtcNow.Date.AddDays(1).AddHours(20),
            PlannedMinutes = 120,
            Status = SessionStatus.Planned
        };
        _db.StudySessions.AddRange(session, planned);
        await _db.SaveChangesAsync(cancellationToken);

        lab2.Priority = TaskPriority.High;
        lab2.Status = StudyTaskStatus.InProgress;
        await _db.SaveChangesAsync(cancellationToken);

        _db.StudySessions.Remove(planned);
        await _db.SaveChangesAsync(cancellationToken);

        var filtered = await _db.StudyTasks
            .AsNoTracking()
            .Where(t => t.Status != StudyTaskStatus.Completed && t.Priority >= TaskPriority.High)
            .ToListAsync(cancellationToken);

        var sorted = await _db.StudyTasks
            .AsNoTracking()
            .OrderBy(t => t.DeadlineUtc)
            .ThenByDescending(t => t.Priority)
            .Select(t => t.Title)
            .ToListAsync(cancellationToken);

        var projection = await _db.StudyTasks
            .AsNoTracking()
            .Select(t => new TaskProjection(t.Title, t.DeadlineUtc, t.EstimatedMinutes - t.ActualMinutes))
            .ToListAsync(cancellationToken);

        var related = await _db.Users
            .AsNoTracking()
            .Where(u => u.Email == DemoEmail)
            .Include(u => u.Disciplines)
                .ThenInclude(d => d.Tasks)
                    .ThenInclude(t => t.Sessions)
            .Include(u => u.AvailabilityWindows)
            .SingleAsync(cancellationToken);

        var aggregationRows = await (
            from task in _db.StudyTasks.AsNoTracking()
            join discipline in _db.Disciplines on task.DisciplineId equals discipline.Id
            group task by discipline.Name into g
            orderby g.Sum(t => t.EstimatedMinutes) descending
            select new { Name = g.Key, TaskCount = g.Count(), EstimatedMinutes = g.Sum(t => t.EstimatedMinutes) })
            .ToListAsync(cancellationToken);

        var aggregation = aggregationRows
            .Select(x => new DisciplineLoad(x.Name, x.TaskCount, x.EstimatedMinutes))
            .ToList();

        var remainingMinutes = await _db.StudyTasks
            .AsNoTracking()
            .Where(t => t.Status != StudyTaskStatus.Completed)
            .SumAsync(t => t.EstimatedMinutes - t.ActualMinutes, cancellationToken);

        return new Lab1DemoReport(
            provider,
            user.Id,
            related.Disciplines.Count,
            related.Disciplines.SelectMany(d => d.Tasks).Count(),
            filtered.Select(t => t.Title).ToList(),
            sorted,
            projection,
            aggregation,
            remainingMinutes,
            await _db.AvailabilityWindows.CountAsync(w => w.UserId == user.Id, cancellationToken),
            await (
                from studySession in _db.StudySessions
                join task in _db.StudyTasks on studySession.StudyTaskId equals task.Id
                join discipline in _db.Disciplines on task.DisciplineId equals discipline.Id
                where discipline.UserId == user.Id
                select studySession).CountAsync(cancellationToken));
    }

    private async Task ResetDemoUserAsync(CancellationToken cancellationToken)
    {
        var existing = await _db.Users
            .Include(u => u.Disciplines)
                .ThenInclude(d => d.Tasks)
                    .ThenInclude(t => t.Sessions)
            .Include(u => u.AvailabilityWindows)
            .Include(u => u.AvailabilityExceptions)
            .FirstOrDefaultAsync(u => u.Email == DemoEmail, cancellationToken);

        if (existing is not null)
        {
            _db.Users.Remove(existing);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private static AvailabilityWindow Window(Guid userId, DayOfWeek day, int startHour, int endHour) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        DayOfWeek = day,
        StartTime = new TimeOnly(startHour, 0),
        EndTime = new TimeOnly(endHour, 0)
    };
}

public sealed record TaskProjection(string Title, DateTime DeadlineUtc, int RemainingMinutes);

public sealed record DisciplineLoad(string Discipline, int TaskCount, int EstimatedMinutes);

public sealed record Lab1DemoReport(
    string Provider,
    Guid DemoUserId,
    int Disciplines,
    int Tasks,
    IReadOnlyList<string> HighPriorityOpenTasks,
    IReadOnlyList<string> TasksByDeadline,
    IReadOnlyList<TaskProjection> RemainingWork,
    IReadOnlyList<DisciplineLoad> LoadByDiscipline,
    int RemainingMinutes,
    int AvailabilityWindows,
    int RemainingSessions);
