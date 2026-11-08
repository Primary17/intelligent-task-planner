using TaskPlanner.Application.DTOs.StudySessions;

namespace TaskPlanner.Application.Services;

public interface IStudySessionService
{
    Task<IReadOnlyList<StudySessionDto>> GetAllAsync(StudySessionQuery query, CancellationToken cancellationToken = default);
    Task<StudySessionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<StudySessionDto> CreateAsync(CreateStudySessionRequest request, CancellationToken cancellationToken = default);
    Task<StudySessionDto?> UpdateAsync(Guid id, UpdateStudySessionRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
