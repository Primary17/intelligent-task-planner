using TaskPlanner.Domain.Enums;

namespace TaskPlanner.Domain.Entities;

public class AvailabilityException
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateOnly Date { get; set; }
    public AvailabilityExceptionKind Kind { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public string? Reason { get; set; }

    public User? User { get; set; }
}
