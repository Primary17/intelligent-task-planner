using TaskPlanner.Domain.Enums;

namespace TaskPlanner.Domain.Entities;

public class StudyTask
{
    public Guid Id { get; set; }
    public Guid DisciplineId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime DeadlineUtc { get; set; }
    public int Complexity { get; set; } = 3;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public int EstimatedMinutes { get; set; }
    public int ActualMinutes { get; set; }
    public StudyTaskStatus Status { get; set; } = StudyTaskStatus.NotStarted;

    public Discipline? Discipline { get; set; }
    public ICollection<StudySession> Sessions { get; set; } = new List<StudySession>();
}
