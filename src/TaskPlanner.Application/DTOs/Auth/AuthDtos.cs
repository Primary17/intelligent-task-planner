using TaskPlanner.Domain.Enums;

namespace TaskPlanner.Application.DTOs.Auth;

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

public sealed record AdminUserListItemDto(
    Guid Id,
    string Email,
    string DisplayName,
    UserRole Role,
    DateTime CreatedAtUtc);
