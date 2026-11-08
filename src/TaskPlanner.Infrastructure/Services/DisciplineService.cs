using Microsoft.EntityFrameworkCore;
using TaskPlanner.Application;
using TaskPlanner.Application.DTOs.Disciplines;
using TaskPlanner.Application.Services;
using TaskPlanner.Domain.Entities;
using TaskPlanner.Infrastructure.Persistence;

namespace TaskPlanner.Infrastructure.Services;

public sealed class DisciplineService : IDisciplineService
{
    private readonly AppDbContextBase _db;
    private readonly ICurrentUserContext _currentUser;

    public DisciplineService(AppDbContextBase db, ICurrentUserContext currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<DisciplineDto>> GetAllAsync(
        DisciplineQuery query,
        CancellationToken cancellationToken = default)
    {
        var q = _db.Disciplines.AsNoTracking().AsQueryable();

        if (!_currentUser.IsAdmin)
        {
            q = q.Where(d => d.UserId == _currentUser.UserId);
        }
        else if (query.UserId.HasValue)
        {
            q = q.Where(d => d.UserId == query.UserId.Value);
        }

        q = query.Sort.ToLowerInvariant() switch
        {
            "color" => q.OrderBy(d => d.Color),
            _ => q.OrderBy(d => d.Name)
        };

        var items = await q
            .Select(d => new
            {
                Discipline = d,
                TaskCount = d.Tasks.Count
            })
            .ToListAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            items = items
                .Where(x => x.Discipline.Name.Contains(query.Search, StringComparison.OrdinalIgnoreCase) ||
                            (x.Discipline.Description?.Contains(query.Search, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();
        }

        return items
            .Select(x => Map(x.Discipline, x.TaskCount))
            .ToList();
    }

    public async Task<DisciplineDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _db.Disciplines
            .AsNoTracking()
            .Include(d => d.Tasks)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (item is null)
        {
            return null;
        }

        EnsureCanAccess(item.UserId);
        return Map(item, item.Tasks.Count);
    }

    public async Task<DisciplineDto> CreateAsync(CreateDisciplineRequest request, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;

        var duplicate = await _db.Disciplines.AnyAsync(
            d => d.UserId == userId && d.Name == request.Name,
            cancellationToken);

        if (duplicate)
        {
            throw new ServiceValidationException("Discipline with this name already exists for the user.");
        }

        var entity = new Discipline
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name,
            Description = request.Description,
            Color = request.Color
        };

        _db.Disciplines.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(entity, 0);
    }

    public async Task<DisciplineDto?> UpdateAsync(
        Guid id,
        UpdateDisciplineRequest request,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.Disciplines
            .Include(d => d.Tasks)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        EnsureCanAccess(entity.UserId);

        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.Color = request.Color;

        await _db.SaveChangesAsync(cancellationToken);
        return Map(entity, entity.Tasks.Count);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Disciplines.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        EnsureCanAccess(entity.UserId);

        _db.Disciplines.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private void EnsureCanAccess(Guid ownerId)
    {
        if (!_currentUser.IsAdmin && ownerId != _currentUser.UserId)
        {
            throw new ServiceForbiddenException("You do not have access to this discipline.");
        }
    }

    private static DisciplineDto Map(Discipline discipline, int taskCount) => new(
        discipline.Id,
        discipline.UserId,
        discipline.Name,
        discipline.Description,
        discipline.Color,
        taskCount);
}
