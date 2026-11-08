using TaskPlanner.Application.DTOs.Availability;

namespace TaskPlanner.Application.Services;

public interface IAvailabilityService
{
    Task<IReadOnlyList<AvailabilityWindowDto>> GetWindowsAsync(CancellationToken cancellationToken = default);
    Task<AvailabilityWindowDto> CreateWindowAsync(AvailabilityWindowRequest request, CancellationToken cancellationToken = default);
    Task<AvailabilityWindowDto?> UpdateWindowAsync(Guid id, AvailabilityWindowRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteWindowAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AvailabilityExceptionDto>> GetExceptionsAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default);
    Task<AvailabilityExceptionDto> CreateExceptionAsync(AvailabilityExceptionRequest request, CancellationToken cancellationToken = default);
    Task<AvailabilityExceptionDto?> UpdateExceptionAsync(Guid id, AvailabilityExceptionRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteExceptionAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EffectiveAvailabilityDayDto>> GetEffectiveAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
}
