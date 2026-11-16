namespace TaskPlanner.Maui.Api;

public enum UserRole
{
    User = 0,
    Admin = 1
}

public enum TaskPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

public enum StudyTaskStatus
{
    NotStarted = 0,
    InProgress = 1,
    Completed = 2
}

public enum SessionStatus
{
    Planned = 0,
    Completed = 1,
    Missed = 2
}
