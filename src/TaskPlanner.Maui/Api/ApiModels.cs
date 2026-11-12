namespace TaskPlanner.Maui.Api;

public sealed record RegisterRequest(string Email, string DisplayName, string Password);

public sealed record LoginRequest(string Email, string Password);

public sealed record AuthResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    UserProfileDto User);

public sealed record UserProfileDto(
    Guid Id,
    string Email,
    string DisplayName,
    UserRole Role);

public sealed record DisciplineDto(
    Guid Id,
    Guid UserId,
    string Name,
    string? Description,
    string Color,
    int TaskCount);

public sealed record CreateDisciplineRequest(
    string Name,
    string? Description,
    string Color = "#3B82F6");

public sealed record UpdateDisciplineRequest(
    string Name,
    string? Description,
    string Color);

public sealed record TimeIntervalDto(TimeOnly StartTime, TimeOnly EndTime);

public sealed record EffectiveDayDto(
    DateOnly Date,
    string Source,
    IReadOnlyList<TimeIntervalDto> Intervals);

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
