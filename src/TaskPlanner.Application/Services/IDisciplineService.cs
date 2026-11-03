using TaskPlanner.Application.DTOs.Disciplines;

namespace TaskPlanner.Application.Services;

public interface IDisciplineService
{
    Task<IReadOnlyList<DisciplineDto>> GetAllAsync(DisciplineQuery query, CancellationToken cancellationToken = default);
    Task<DisciplineDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DisciplineDto> CreateAsync(CreateDisciplineRequest request, CancellationToken cancellationToken = default);
    Task<DisciplineDto?> UpdateAsync(Guid id, UpdateDisciplineRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
