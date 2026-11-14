using TaskPlanner.Application.DTOs.Scheduling;

namespace TaskPlanner.Application.Services;

public interface IScheduleService
{
    Task<GenerateScheduleResponse> GenerateAsync(
        GenerateScheduleRequest request,
        CancellationToken cancellationToken = default);
}
