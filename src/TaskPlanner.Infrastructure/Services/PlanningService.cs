using Microsoft.EntityFrameworkCore;
using TaskPlanner.Application;
using TaskPlanner.Application.DTOs.Planning;
using TaskPlanner.Application.Planning;
using TaskPlanner.Application.Services;
using TaskPlanner.Domain.Enums;
using TaskPlanner.Infrastructure.Persistence;

namespace TaskPlanner.Infrastructure.Services;

public sealed class PlanningService : IPlanningService
{
    private const int MaxUtcOffsetMinutes = 14 * 60;

    private readonly AppDbContextBase _db;
    private readonly ICurrentUserContext _currentUser;

    public PlanningService(AppDbContextBase db, ICurrentUserContext currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CapacityReportDto> GetCapacityAsync(
        int utcOffsetMinutes,
        CancellationToken cancellationToken = default)
    {
        if (Math.Abs(utcOffsetMinutes) > MaxUtcOffsetMinutes)
        {
            throw new ServiceValidationException("utcOffsetMinutes must be between -840 and 840.");
        }

        var userId = _currentUser.UserId;
        var now = DateTime.UtcNow;

        var tasks = await _db.StudyTasks
            .AsNoTracking()
            .Include(t => t.Discipline)
            .Where(t => t.Discipline!.UserId == userId && t.Status != StudyTaskStatus.Completed)
            .ToListAsync(cancellationToken);

        var plannedByTask = (await _db.StudySessions
                .AsNoTracking()
                .Where(s => s.StudyTask!.Discipline!.UserId == userId
                    && s.Status == SessionStatus.Planned
                    && s.EndAtUtc > now)
                .ToListAsync(cancellationToken))
            .GroupBy(s => s.StudyTaskId)
            .ToDictionary(g => g.Key, g => g.Sum(s => s.PlannedMinutes));

        var windows = await _db.AvailabilityWindows
            .AsNoTracking()
            .Where(w => w.UserId == userId)
            .ToListAsync(cancellationToken);

        var earliest = DateOnly.FromDateTime(now.AddDays(-2));
        var exceptions = await _db.AvailabilityExceptions
            .AsNoTracking()
            .Where(e => e.UserId == userId && e.Date >= earliest)
            .ToListAsync(cancellationToken);

        var inputs = tasks
            .Select(t => new CapacityTaskInput(
                t.Id,
                t.Title,
                t.DisciplineId,
                t.Discipline?.Name ?? string.Empty,
                DateTime.SpecifyKind(t.DeadlineUtc, DateTimeKind.Utc),
                Math.Max(0, t.EstimatedMinutes - t.ActualMinutes),
                plannedByTask.GetValueOrDefault(t.Id)))
            .ToList();

        return CapacityCalculator.Calculate(now, utcOffsetMinutes, windows, exceptions, inputs);
    }
}
