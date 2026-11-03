using TaskPlanner.Domain.Enums;

namespace TaskPlanner.Application.Services;

public interface ICurrentUserContext
{
    Guid UserId { get; }
    string Email { get; }
    UserRole Role { get; }
    bool IsAdmin { get; }
}
