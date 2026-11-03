namespace TaskPlanner.Domain.Entities;

public class Discipline
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Color { get; set; } = "#3B82F6";

    public User? User { get; set; }
    public ICollection<StudyTask> Tasks { get; set; } = new List<StudyTask>();
}
