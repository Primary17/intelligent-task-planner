using TaskPlanner.Domain.Enums;

namespace TaskPlanner.Application.DTOs.StudySessions;

public sealed record StudySessionDto(
    Guid Id,
    Guid StudyTaskId,
    string TaskTitle,
    Guid DisciplineId,
    string DisciplineName,
    DateTime StartAtUtc,
    DateTime EndAtUtc,
    int PlannedMinutes,
    int? ActualMinutes,
    SessionStatus Status,
    bool IsLocked);

public sealed record CreateStudySessionRequest(
    Guid StudyTaskId,
    DateTime StartAtUtc,
    DateTime EndAtUtc,
    bool IsLocked = false);

public sealed record UpdateStudySessionRequest(
    DateTime StartAtUtc,
    DateTime EndAtUtc,
    SessionStatus Status,
    int? ActualMinutes,
    bool IsLocked);

public sealed record StudySessionQuery(
    DateTime? From = null,
    DateTime? To = null,
    Guid? StudyTaskId = null,
    SessionStatus? Status = null);
