using Microsoft.EntityFrameworkCore;
using TaskPlanner.Application;
using TaskPlanner.Application.DTOs.Availability;
using TaskPlanner.Application.Services;
using TaskPlanner.Domain.Entities;
using TaskPlanner.Domain.Enums;
using TaskPlanner.Infrastructure.Persistence;

namespace TaskPlanner.Infrastructure.Services;

public sealed class AvailabilityService : IAvailabilityService
{
    private const int MaxRangeDays = 92;

    private readonly AppDbContextBase _db;
    private readonly ICurrentUserContext _currentUser;

    public AvailabilityService(AppDbContextBase db, ICurrentUserContext currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<AvailabilityWindowDto>> GetWindowsAsync(CancellationToken cancellationToken = default)
    {
        var windows = await LoadWindowsAsync(cancellationToken);
        return windows
            .OrderBy(w => WeekOrder(w.DayOfWeek))
            .ThenBy(w => w.StartTime)
            .Select(MapWindow)
            .ToList();
    }

    public async Task<AvailabilityWindowDto> CreateWindowAsync(
        AvailabilityWindowRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureNoOverlapAsync(request, null, cancellationToken);

        var entity = new AvailabilityWindow
        {
            Id = Guid.NewGuid(),
            UserId = _currentUser.UserId,
            DayOfWeek = request.DayOfWeek,
            StartTime = request.StartTime,
            EndTime = request.EndTime
        };

        _db.AvailabilityWindows.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return MapWindow(entity);
    }

    public async Task<AvailabilityWindowDto?> UpdateWindowAsync(
        Guid id,
        AvailabilityWindowRequest request,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.AvailabilityWindows.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        EnsureOwner(entity.UserId);
        await EnsureNoOverlapAsync(request, id, cancellationToken);

        entity.DayOfWeek = request.DayOfWeek;
        entity.StartTime = request.StartTime;
        entity.EndTime = request.EndTime;

        await _db.SaveChangesAsync(cancellationToken);
        return MapWindow(entity);
    }

    public async Task<bool> DeleteWindowAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.AvailabilityWindows.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        EnsureOwner(entity.UserId);
        _db.AvailabilityWindows.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<AvailabilityExceptionDto>> GetExceptionsAsync(
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default)
    {
        var q = _db.AvailabilityExceptions.AsNoTracking().Where(e => e.UserId == _currentUser.UserId);

        if (from.HasValue)
        {
            q = q.Where(e => e.Date >= from.Value);
        }

        if (to.HasValue)
        {
            q = q.Where(e => e.Date <= to.Value);
        }

        var items = await q.OrderBy(e => e.Date).ToListAsync(cancellationToken);
        return items.Select(MapException).ToList();
    }

    public async Task<AvailabilityExceptionDto> CreateExceptionAsync(
        AvailabilityExceptionRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureDateFreeAsync(request.Date, null, cancellationToken);

        var entity = new AvailabilityException
        {
            Id = Guid.NewGuid(),
            UserId = _currentUser.UserId
        };
        Apply(entity, request);

        _db.AvailabilityExceptions.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return MapException(entity);
    }

    public async Task<AvailabilityExceptionDto?> UpdateExceptionAsync(
        Guid id,
        AvailabilityExceptionRequest request,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.AvailabilityExceptions.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        EnsureOwner(entity.UserId);
        await EnsureDateFreeAsync(request.Date, id, cancellationToken);

        Apply(entity, request);
        await _db.SaveChangesAsync(cancellationToken);
        return MapException(entity);
    }

    public async Task<bool> DeleteExceptionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.AvailabilityExceptions.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        EnsureOwner(entity.UserId);
        _db.AvailabilityExceptions.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<EffectiveAvailabilityDayDto>> GetEffectiveAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        if (to < from)
        {
            throw new ServiceValidationException("'to' must not be earlier than 'from'.");
        }

        if (to.DayNumber - from.DayNumber + 1 > MaxRangeDays)
        {
            throw new ServiceValidationException($"The range cannot exceed {MaxRangeDays} days.");
        }

        var windows = await LoadWindowsAsync(cancellationToken);
        var exceptions = (await GetExceptionsAsync(from, to, cancellationToken))
            .ToDictionary(e => e.Date);

        var result = new List<EffectiveAvailabilityDayDto>();
        for (var date = from; date <= to; date = date.AddDays(1))
        {
            if (exceptions.TryGetValue(date, out var exception))
            {
                var intervals = exception.Kind == AvailabilityExceptionKind.CustomHours
                    ? new List<TimeIntervalDto> { new(exception.StartTime!.Value, exception.EndTime!.Value) }
                    : new List<TimeIntervalDto>();
                result.Add(new EffectiveAvailabilityDayDto(date, exception.Kind.ToString(), intervals));
                continue;
            }

            var weekly = windows
                .Where(w => w.DayOfWeek == date.DayOfWeek)
                .OrderBy(w => w.StartTime)
                .Select(w => new TimeIntervalDto(w.StartTime, w.EndTime))
                .ToList();
            result.Add(new EffectiveAvailabilityDayDto(date, "Weekly", weekly));
        }

        return result;
    }

    private Task<List<AvailabilityWindow>> LoadWindowsAsync(CancellationToken cancellationToken) =>
        _db.AvailabilityWindows
            .AsNoTracking()
            .Where(w => w.UserId == _currentUser.UserId)
            .ToListAsync(cancellationToken);

    private async Task EnsureNoOverlapAsync(AvailabilityWindowRequest request, Guid? ignoreId, CancellationToken cancellationToken)
    {
        var sameDay = await _db.AvailabilityWindows
            .AsNoTracking()
            .Where(w => w.UserId == _currentUser.UserId && w.DayOfWeek == request.DayOfWeek)
            .ToListAsync(cancellationToken);

        var overlaps = sameDay.Any(w =>
            w.Id != ignoreId && request.StartTime < w.EndTime && w.StartTime < request.EndTime);

        if (overlaps)
        {
            throw new ServiceValidationException("The window overlaps with an existing window on this day.");
        }
    }

    private async Task EnsureDateFreeAsync(DateOnly date, Guid? ignoreId, CancellationToken cancellationToken)
    {
        var taken = await _db.AvailabilityExceptions.AnyAsync(
            e => e.UserId == _currentUser.UserId && e.Date == date && e.Id != ignoreId,
            cancellationToken);

        if (taken)
        {
            throw new ServiceValidationException("An exception for this date already exists.");
        }
    }

    private void EnsureOwner(Guid ownerId)
    {
        if (ownerId != _currentUser.UserId)
        {
            throw new ServiceForbiddenException("You do not have access to this resource.");
        }
    }

    private static void Apply(AvailabilityException entity, AvailabilityExceptionRequest request)
    {
        entity.Date = request.Date;
        entity.Kind = request.Kind;
        entity.StartTime = request.StartTime;
        entity.EndTime = request.EndTime;
        entity.Reason = request.Reason;
    }

    private static int WeekOrder(DayOfWeek day) => ((int)day + 6) % 7;

    private static AvailabilityWindowDto MapWindow(AvailabilityWindow w) =>
        new(w.Id, w.DayOfWeek, w.StartTime, w.EndTime);

    private static AvailabilityExceptionDto MapException(AvailabilityException e) =>
        new(e.Id, e.Date, e.Kind, e.StartTime, e.EndTime, e.Reason);
}
