using Microsoft.EntityFrameworkCore;
using TaskPlanner.Application;
using TaskPlanner.Application.DTOs.StudySessions;
using TaskPlanner.Application.Services;
using TaskPlanner.Domain.Entities;
using TaskPlanner.Domain.Enums;
using TaskPlanner.Infrastructure.Persistence;

namespace TaskPlanner.Infrastructure.Services;

public sealed class StudySessionService : IStudySessionService
{
    private readonly AppDbContextBase _db;
    private readonly ICurrentUserContext _currentUser;

    public StudySessionService(AppDbContextBase db, ICurrentUserContext currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<StudySessionDto>> GetAllAsync(
        StudySessionQuery query,
        CancellationToken cancellationToken = default)
    {
        var q = WithTask(_db.StudySessions.AsNoTracking());

        if (!_currentUser.IsAdmin)
        {
            q = q.Where(s => s.StudyTask!.Discipline!.UserId == _currentUser.UserId);
        }

        if (query.From.HasValue)
        {
            var from = AsUtc(query.From.Value);
            q = q.Where(s => s.EndAtUtc > from);
        }

        if (query.To.HasValue)
        {
            var to = AsUtc(query.To.Value);
            q = q.Where(s => s.StartAtUtc < to);
        }

        if (query.StudyTaskId.HasValue)
        {
            q = q.Where(s => s.StudyTaskId == query.StudyTaskId.Value);
        }

        if (query.Status.HasValue)
        {
            q = q.Where(s => s.Status == query.Status.Value);
        }

        var items = await q.OrderBy(s => s.StartAtUtc).ToListAsync(cancellationToken);
        return items.Select(Map).ToList();
    }

    public async Task<StudySessionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await WithTask(_db.StudySessions.AsNoTracking())
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (item is null)
        {
            return null;
        }

        EnsureCanAccess(item.StudyTask!);
        return Map(item);
    }

    public async Task<StudySessionDto> CreateAsync(
        CreateStudySessionRequest request,
        CancellationToken cancellationToken = default)
    {
        var task = await _db.StudyTasks
            .Include(t => t.Discipline)
            .FirstOrDefaultAsync(t => t.Id == request.StudyTaskId, cancellationToken)
            ?? throw new ServiceValidationException($"Task '{request.StudyTaskId}' was not found.");

        EnsureCanAccess(task);

        var start = AsUtc(request.StartAtUtc);
        var end = AsUtc(request.EndAtUtc);
        await EnsureNoOverlapAsync(task.Discipline!.UserId, null, start, end, cancellationToken);

        var entity = new StudySession
        {
            Id = Guid.NewGuid(),
            StudyTaskId = task.Id,
            StartAtUtc = start,
            EndAtUtc = end,
            PlannedMinutes = (int)(end - start).TotalMinutes,
            Status = SessionStatus.Planned,
            IsLocked = request.IsLocked
        };

        _db.StudySessions.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        entity.StudyTask = task;
        return Map(entity);
    }

    public async Task<StudySessionDto?> UpdateAsync(
        Guid id,
        UpdateStudySessionRequest request,
        CancellationToken cancellationToken = default)
    {
        var entity = await WithTask(_db.StudySessions)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        var task = entity.StudyTask!;
        EnsureCanAccess(task);

        var start = AsUtc(request.StartAtUtc);
        var end = AsUtc(request.EndAtUtc);
        await EnsureNoOverlapAsync(task.Discipline!.UserId, entity.Id, start, end, cancellationToken);

        var plannedMinutes = (int)(end - start).TotalMinutes;
        var previousActual = entity.ActualMinutes ?? 0;
        int? newActual = request.Status == SessionStatus.Completed
            ? request.ActualMinutes ?? plannedMinutes
            : null;

        entity.StartAtUtc = start;
        entity.EndAtUtc = end;
        entity.PlannedMinutes = plannedMinutes;
        entity.Status = request.Status;
        entity.ActualMinutes = newActual;
        entity.IsLocked = request.IsLocked;

        // Time logged on sessions is added to the task's total, so progress stays in one place.
        task.ActualMinutes = Math.Max(0, task.ActualMinutes + (newActual ?? 0) - previousActual);
        if (task.ActualMinutes > 0 && task.Status == StudyTaskStatus.NotStarted)
        {
            task.Status = StudyTaskStatus.InProgress;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await WithTask(_db.StudySessions)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        EnsureCanAccess(entity.StudyTask!);

        _db.StudySessions.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static IQueryable<StudySession> WithTask(IQueryable<StudySession> query) =>
        query.Include(s => s.StudyTask).ThenInclude(t => t!.Discipline);

    private async Task EnsureNoOverlapAsync(
        Guid ownerId,
        Guid? excludeId,
        DateTime start,
        DateTime end,
        CancellationToken cancellationToken)
    {
        var overlaps = await _db.StudySessions.AnyAsync(
            s => s.StudyTask!.Discipline!.UserId == ownerId
                && (excludeId == null || s.Id != excludeId)
                && s.StartAtUtc < end
                && start < s.EndAtUtc,
            cancellationToken);

        if (overlaps)
        {
            throw new ServiceValidationException("The session overlaps another session.");
        }
    }

    private void EnsureCanAccess(StudyTask task)
    {
        if (_currentUser.IsAdmin)
        {
            return;
        }

        if (task.Discipline?.UserId != _currentUser.UserId)
        {
            throw new ServiceForbiddenException("You do not have access to this session.");
        }
    }

    private static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    private static StudySessionDto Map(StudySession session) => new(
        session.Id,
        session.StudyTaskId,
        session.StudyTask?.Title ?? string.Empty,
        session.StudyTask?.DisciplineId ?? Guid.Empty,
        session.StudyTask?.Discipline?.Name ?? string.Empty,
        session.StartAtUtc,
        session.EndAtUtc,
        session.PlannedMinutes,
        session.ActualMinutes,
        session.Status,
        session.IsLocked);
}
