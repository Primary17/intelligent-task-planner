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
        var result = await PlanAsync(request, replan: false, cancellationToken);
        return new GenerateScheduleResponse(
            result.From,
            result.To,
            result.Created,
            result.Unscheduled,
            result.Conflicts);
    }

    public async Task<ReplanScheduleResponse> ReplanAsync(
        GenerateScheduleRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await PlanAsync(request, replan: true, cancellationToken);
        return new ReplanScheduleResponse(
            result.From,
            result.To,
            result.Stayed,
            result.Created,
            result.Removed,
            result.Unscheduled,
            result.Conflicts);
    }

    private async Task<ScheduleWriteResult> PlanAsync(
        GenerateScheduleRequest request,
        bool replan,
        CancellationToken cancellationToken)
    {
        if (!ScheduleHorizonRules.TryResolve(request, DateTime.UtcNow, out var from, out var to, out var horizonError))
        {
            throw new ServiceValidationException(horizonError!);
        }

        if (!ScheduleHorizonRules.TryResolveTimeZone(request.TimeZoneId, out var timeZone, out var zoneError))
        {
            throw new ServiceValidationException(zoneError!);
        }

        var utcNow = DateTime.UtcNow;
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
        var removed = sessions
            .Where(s => ShouldRemove(s, replan, from, to, utcNow, unfinishedIds))
            .ToList();

        if (removed.Count > 0)
        {
            _db.StudySessions.RemoveRange(removed);
        }

        var removedIds = removed.Select(s => s.Id).ToHashSet();
        var kept = sessions.Where(s => !removedIds.Contains(s.Id)).ToList();
        var keptPlanned = kept
            .Where(s => s.Status == SessionStatus.Planned && !s.IsLocked)
            .GroupBy(s => s.StudyTaskId)
            .ToDictionary(g => g.Key, g => g.Sum(s => s.PlannedMinutes));

        var schedulable = tasks
            .Select(task => ToSchedulable(task, keptPlanned.GetValueOrDefault(task.Id)))
            .ToList();

        var occupied = kept.Select(ToOccupied).ToList();
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

        return new ScheduleWriteResult(
            from,
            to,
            kept.Where(s => Overlaps(s, from, to))
                .OrderBy(s => s.StartAtUtc)
                .ThenBy(s => s.Id)
                .Select(Map)
                .ToList(),
            created.Select(Map).ToList(),
            removed.OrderBy(s => s.StartAtUtc).ThenBy(s => s.Id).Select(Map).ToList(),
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

    private static bool ShouldRemove(
        StudySession session,
        bool replan,
        DateTime from,
        DateTime to,
        DateTime utcNow,
        HashSet<Guid> unfinishedIds)
    {
        if (session.IsLocked || !unfinishedIds.Contains(session.StudyTaskId) || !Overlaps(session, from, to))
        {
            return false;
        }

        if (session.Status == SessionStatus.Planned)
        {
            return !replan || ScheduleHorizonRules.AsUtc(session.EndAtUtc) > utcNow;
        }

        return replan && session.Status == SessionStatus.Missed;
    }

    private static SchedulableTask ToSchedulable(StudyTask task, int keptPlannedMinutes) =>
        new(
            task.Id,
            task.DeadlineUtc,
            task.Complexity,
            task.Priority,
            Math.Max(0, task.EstimatedMinutes - keptPlannedMinutes),
            task.ActualMinutes,
            task.Status);

    private static OccupiedSession ToOccupied(StudySession session) =>
        new(
            session.Id,
            session.StudyTaskId,
            ScheduleHorizonRules.AsUtc(session.StartAtUtc),
            ScheduleHorizonRules.AsUtc(session.EndAtUtc),
            session.PlannedMinutes,
            session.ActualMinutes,
            session.Status,
            session.IsLocked);

    private static bool Overlaps(StudySession session, DateTime from, DateTime to)
    {
        var start = ScheduleHorizonRules.AsUtc(session.StartAtUtc);
        var end = ScheduleHorizonRules.AsUtc(session.EndAtUtc);
        return start < to && end > from;
    }

    private static GeneratedSessionDto Map(StudySession session) =>
        new(
            session.Id,
            session.StudyTaskId,
            ScheduleHorizonRules.AsUtc(session.StartAtUtc),
            ScheduleHorizonRules.AsUtc(session.EndAtUtc),
            session.PlannedMinutes,
            session.Status,
            session.IsLocked);

    private sealed record ScheduleWriteResult(
        DateTime From,
        DateTime To,
        IReadOnlyList<GeneratedSessionDto> Stayed,
        IReadOnlyList<GeneratedSessionDto> Created,
        IReadOnlyList<GeneratedSessionDto> Removed,
        IReadOnlyList<UnscheduledWorkDto> Unscheduled,
        IReadOnlyList<ScheduleConflictDto> Conflicts);
}
