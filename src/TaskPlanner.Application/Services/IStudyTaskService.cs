using TaskPlanner.Application.DTOs.StudyTasks;

namespace TaskPlanner.Application.Services;

public interface IStudyTaskService
{
    Task<IReadOnlyList<StudyTaskDto>> GetAllAsync(StudyTaskQuery query, CancellationToken cancellationToken = default);
    Task<StudyTaskDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<StudyTaskDto> CreateAsync(CreateStudyTaskRequest request, CancellationToken cancellationToken = default);
    Task<StudyTaskDto?> UpdateAsync(Guid id, UpdateStudyTaskRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
