using TaskPlanner.Application.DTOs.Planning;

namespace TaskPlanner.Application.Services;

public interface IPlanningService
{
    Task<CapacityReportDto> GetCapacityAsync(int utcOffsetMinutes, CancellationToken cancellationToken = default);
}
