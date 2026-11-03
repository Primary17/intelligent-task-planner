using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TaskPlanner.Application.Services;
using TaskPlanner.Domain.Enums;

namespace TaskPlanner.Infrastructure.Security;

public sealed class CurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid UserId => Guid.Parse(
        User?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("User id claim is missing."));

    public string Email => User?.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

    public UserRole Role
    {
        get
        {
            var roleValue = User?.FindFirstValue(ClaimTypes.Role);
            return Enum.TryParse<UserRole>(roleValue, ignoreCase: true, out var role)
                ? role
                : UserRole.User;
        }
    }

    public bool IsAdmin => Role == UserRole.Admin;
}
