using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TaskPlanner.Application;
using TaskPlanner.Application.DTOs.StudyTasks;
using TaskPlanner.Application.Services;
using TaskPlanner.Domain.Entities;
using TaskPlanner.Domain.Enums;
using TaskPlanner.Infrastructure.Persistence;

namespace TaskPlanner.Infrastructure.Services;

public sealed class StudyTaskService : IStudyTaskService
{
    private static readonly Expression<Func<StudyTask, int>> PriorityRank = t =>
        t.Priority == TaskPriority.Critical ? 3 :
        t.Priority == TaskPriority.High ? 2 :
        t.Priority == TaskPriority.Medium ? 1 : 0;

    private readonly AppDbContextBase _db;
    private readonly ICurrentUserContext _currentUser;

    public StudyTaskService(AppDbContextBase db, ICurrentUserContext currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<StudyTaskDto>> GetAllAsync(
        StudyTaskQuery query,
        CancellationToken cancellationToken = default)
    {
        var q = _db.StudyTasks.AsNoTracking().Include(t => t.Discipline).AsQueryable();

        if (!_currentUser.IsAdmin)
        {
            q = q.Where(t => t.Discipline!.UserId == _currentUser.UserId);
        }

        if (query.DisciplineId.HasValue)
        {
            q = q.Where(t => t.DisciplineId == query.DisciplineId.Value);
            if (!_currentUser.IsAdmin)
            {
                await EnsureDisciplineOwnedAsync(query.DisciplineId.Value, cancellationToken);
            }
        }

        if (query.Status.HasValue)
        {
            q = q.Where(t => t.Status == query.Status.Value);
        }

        if (query.MinPriority.HasValue)
        {
            q = q.Where(PriorityAtLeast(query.MinPriority.Value));
        }

        q = query.Sort.ToLowerInvariant() switch
        {
            "priority" => q.OrderByDescending(PriorityRank).ThenBy(t => t.DeadlineUtc),
            "title" => q.OrderBy(t => t.Title),
            _ => q.OrderBy(t => t.DeadlineUtc).ThenByDescending(PriorityRank)
        };

        var items = await q.ToListAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            items = items
                .Where(t => t.Title.Contains(query.Search, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return items.Select(Map).ToList();
    }

    public async Task<StudyTaskDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _db.StudyTasks
            .AsNoTracking()
            .Include(t => t.Discipline)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (item is null)
        {
            return null;
        }

        EnsureCanAccessTask(item);
        return Map(item);
    }

    public async Task<StudyTaskDto> CreateAsync(CreateStudyTaskRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureDisciplineOwnedAsync(request.DisciplineId, cancellationToken);

        var entity = new StudyTask
        {
            Id = Guid.NewGuid(),
            DisciplineId = request.DisciplineId,
            Title = request.Title,
            DeadlineUtc = request.DeadlineUtc,
            Complexity = request.Complexity,
            Priority = request.Priority,
            EstimatedMinutes = request.EstimatedMinutes,
            Status = request.Status
        };

        _db.StudyTasks.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        var created = await _db.StudyTasks
            .AsNoTracking()
            .Include(t => t.Discipline)
            .FirstAsync(t => t.Id == entity.Id, cancellationToken);
        return Map(created);
    }

    public async Task<StudyTaskDto?> UpdateAsync(
        Guid id,
        UpdateStudyTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.StudyTasks
            .Include(t => t.Discipline)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        EnsureCanAccessTask(entity);

        entity.Title = request.Title;
        entity.DeadlineUtc = request.DeadlineUtc;
        entity.Complexity = request.Complexity;
        entity.Priority = request.Priority;
        entity.EstimatedMinutes = request.EstimatedMinutes;
        entity.ActualMinutes = request.ActualMinutes;
        entity.Status = request.Status;

        await _db.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.StudyTasks
            .Include(t => t.Discipline)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        EnsureCanAccessTask(entity);

        _db.StudyTasks.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static Expression<Func<StudyTask, bool>> PriorityAtLeast(TaskPriority min) => min switch
    {
        TaskPriority.Critical => t => t.Priority == TaskPriority.Critical,
        TaskPriority.High => t => t.Priority == TaskPriority.High || t.Priority == TaskPriority.Critical,
        TaskPriority.Medium => t => t.Priority != TaskPriority.Low,
        _ => t => true
    };

    private async Task EnsureDisciplineOwnedAsync(Guid disciplineId, CancellationToken cancellationToken)
    {
        if (_currentUser.IsAdmin)
        {
            if (!await _db.Disciplines.AnyAsync(d => d.Id == disciplineId, cancellationToken))
            {
                throw new ServiceValidationException($"Discipline '{disciplineId}' was not found.");
            }

            return;
        }

        var owned = await _db.Disciplines.AnyAsync(
            d => d.Id == disciplineId && d.UserId == _currentUser.UserId,
            cancellationToken);

        if (!owned)
        {
            throw new ServiceForbiddenException("You cannot use this discipline.");
        }
    }

    private void EnsureCanAccessTask(StudyTask task)
    {
        if (_currentUser.IsAdmin)
        {
            return;
        }

        if (task.Discipline?.UserId != _currentUser.UserId)
        {
            throw new ServiceForbiddenException("You do not have access to this task.");
        }
    }

    private static StudyTaskDto Map(StudyTask task) => new(
        task.Id,
        task.DisciplineId,
        task.Discipline?.Name ?? string.Empty,
        task.Title,
        task.DeadlineUtc,
        task.Complexity,
        task.Priority,
        task.EstimatedMinutes,
        task.ActualMinutes,
        task.Status);
}
