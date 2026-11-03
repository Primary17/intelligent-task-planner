namespace TaskPlanner.Domain.Entities;

public class AvailabilityWindow
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    public User? User { get; set; }
}
