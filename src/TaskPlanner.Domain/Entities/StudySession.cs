using TaskPlanner.Domain.Enums;

namespace TaskPlanner.Domain.Entities;

public class StudySession
{
    public Guid Id { get; set; }
    public Guid StudyTaskId { get; set; }
    public DateTime StartAtUtc { get; set; }
    public DateTime EndAtUtc { get; set; }
    public int PlannedMinutes { get; set; }
    public int? ActualMinutes { get; set; }
    public SessionStatus Status { get; set; } = SessionStatus.Planned;
    public bool IsLocked { get; set; }

    public StudyTask? StudyTask { get; set; }
}
