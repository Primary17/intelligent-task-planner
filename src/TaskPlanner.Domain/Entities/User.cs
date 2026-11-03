using TaskPlanner.Domain.Enums;

namespace TaskPlanner.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.User;
    public DateTime CreatedAtUtc { get; set; }

    public ICollection<Discipline> Disciplines { get; set; } = new List<Discipline>();
    public ICollection<AvailabilityWindow> AvailabilityWindows { get; set; } = new List<AvailabilityWindow>();
    public ICollection<AvailabilityException> AvailabilityExceptions { get; set; } = new List<AvailabilityException>();
}
