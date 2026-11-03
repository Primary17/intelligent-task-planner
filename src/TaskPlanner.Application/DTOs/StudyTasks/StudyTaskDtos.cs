using TaskPlanner.Domain.Enums;

namespace TaskPlanner.Application.DTOs.StudyTasks;

public sealed record StudyTaskDto(
    Guid Id,
    Guid DisciplineId,
    string DisciplineName,
    string Title,
    DateTime DeadlineUtc,
    int Complexity,
    TaskPriority Priority,
    int EstimatedMinutes,
    int ActualMinutes,
    StudyTaskStatus Status);

public sealed record CreateStudyTaskRequest(
    Guid DisciplineId,
    string Title,
    DateTime DeadlineUtc,
    int Complexity,
    TaskPriority Priority,
    int EstimatedMinutes,
    StudyTaskStatus Status = StudyTaskStatus.NotStarted);

public sealed record UpdateStudyTaskRequest(
    string Title,
    DateTime DeadlineUtc,
    int Complexity,
    TaskPriority Priority,
    int EstimatedMinutes,
    int ActualMinutes,
    StudyTaskStatus Status);

public sealed record StudyTaskQuery(
    Guid? DisciplineId = null,
    StudyTaskStatus? Status = null,
    TaskPriority? MinPriority = null,
    string? Search = null,
    string Sort = "deadline");
