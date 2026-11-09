using Microsoft.EntityFrameworkCore;
using TaskPlanner.Application;
using TaskPlanner.Application.DTOs.Scheduling;
using TaskPlanner.Application.Scheduling;
using TaskPlanner.Application.Services;
using TaskPlanner.Application.Validation;
using TaskPlanner.Domain.Entities;
using TaskPlanner.Domain.Enums;
using TaskPlanner.Infrastructure.Persistence;

namespace TaskPlanner.Infrastructure.Services;

public sealed class ScheduleService : IScheduleService
{
    private readonly AppDbContextBase _db;
    private readonly ICurrentUserContext _currentUser;
    private readonly SchedulePlanner _planner = new();

    public ScheduleService(AppDbContextBase db, ICurrentUserContext currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<GenerateScheduleResponse> GenerateAsync(
        GenerateScheduleRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ScheduleHorizonRules.TryResolve(request, DateTime.UtcNow, out var from, out var to, out var horizonError))
        {
            throw new ServiceValidationException(horizonError!);
        }

        if (!ScheduleHorizonRules.TryResolveTimeZone(request.TimeZoneId, out var timeZone, out var zoneError))
        {
            throw new ServiceValidationException(zoneError!);
        }

        var userId = _currentUser.UserId;

        var tasks = await _db.StudyTasks
            .AsNoTracking()
            .Where(t => t.Discipline!.UserId == userId && t.Status != StudyTaskStatus.Completed)
            .ToListAsync(cancellationToken);

        var windows = await _db.AvailabilityWindows
            .AsNoTracking()
            .Where(w => w.UserId == userId)
            .ToListAsync(cancellationToken);

        var exceptions = await _db.AvailabilityExceptions
            .AsNoTracking()
            .Where(e => e.UserId == userId)
            .ToListAsync(cancellationToken);

        var sessions = await _db.StudySessions
            .Where(s => s.StudyTask!.Discipline!.UserId == userId)
            .ToListAsync(cancellationToken);

        var unfinishedIds = tasks.Select(t => t.Id).ToHashSet();
        var replaced = sessions
            .Where(s => unfinishedIds.Contains(s.StudyTaskId)
                && s.Status == SessionStatus.Planned
                && !s.IsLocked
                && Overlaps(s, from, to))
            .ToList();

        if (replaced.Count > 0)
        {
            _db.StudySessions.RemoveRange(replaced);
        }

        var removedIds = replaced.Select(s => s.Id).ToHashSet();
        var kept = sessions.Where(s => !removedIds.Contains(s.Id)).ToList();
        var keptPlanned = kept
            .Where(s => s.Status == SessionStatus.Planned && !s.IsLocked)
            .GroupBy(s => s.StudyTaskId)
            .ToDictionary(g => g.Key, g => g.Sum(s => s.PlannedMinutes));

        var schedulable = tasks
            .Select(task => new SchedulableTask(
                task.Id,
                task.DeadlineUtc,
                task.Complexity,
                task.Priority,
                Math.Max(0, task.EstimatedMinutes - keptPlanned.GetValueOrDefault(task.Id)),
                task.ActualMinutes,
                task.Status))
            .ToList();

        var occupied = kept
            .Select(s => new OccupiedSession(
                s.Id,
                s.StudyTaskId,
                ScheduleHorizonRules.AsUtc(s.StartAtUtc),
                ScheduleHorizonRules.AsUtc(s.EndAtUtc),
                s.PlannedMinutes,
                s.ActualMinutes,
                s.Status,
                s.IsLocked))
            .ToList();

        var plan = _planner.Plan(
            new ScheduleHorizon(from, to, timeZone),
            windows,
            exceptions,
            schedulable,
            occupied);

        var created = plan.Sessions.Select(session => new StudySession
        {
            Id = Guid.NewGuid(),
            StudyTaskId = session.StudyTaskId,
            StartAtUtc = session.StartAtUtc,
            EndAtUtc = session.EndAtUtc,
            PlannedMinutes = session.PlannedMinutes,
            Status = SessionStatus.Planned,
            IsLocked = false
        }).ToList();

        _db.StudySessions.AddRange(created);
        await _db.SaveChangesAsync(cancellationToken);

        return new GenerateScheduleResponse(
            from,
            to,
            created.Select(s => new GeneratedSessionDto(
                s.Id,
                s.StudyTaskId,
                s.StartAtUtc,
                s.EndAtUtc,
                s.PlannedMinutes,
                s.Status)).ToList(),
            plan.Unscheduled.Select(item => new UnscheduledWorkDto(
                item.StudyTaskId,
                item.RemainingMinutes,
                item.ScheduledMinutes)).ToList(),
            plan.Conflicts.Select(conflict => new ScheduleConflictDto(
                conflict.FirstSessionId,
                conflict.SecondSessionId,
                conflict.OverlapStartUtc,
                conflict.OverlapEndUtc)).ToList());
    }

    private static bool Overlaps(StudySession session, DateTime from, DateTime to)
    {
        var start = ScheduleHorizonRules.AsUtc(session.StartAtUtc);
        var end = ScheduleHorizonRules.AsUtc(session.EndAtUtc);
        return start < to && end > from;
    }
}
