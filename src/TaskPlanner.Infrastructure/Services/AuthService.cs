using Microsoft.EntityFrameworkCore;
using TaskPlanner.Application;
using TaskPlanner.Application.DTOs.Auth;
using TaskPlanner.Application.Services;
using TaskPlanner.Domain.Entities;
using TaskPlanner.Domain.Enums;
using TaskPlanner.Infrastructure.Persistence;
using TaskPlanner.Infrastructure.Security;

namespace TaskPlanner.Infrastructure.Services;

public sealed class AuthService : IAuthService
{
    private readonly AppDbContextBase _db;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthService(
        AppDbContextBase db,
        IPasswordHasherService passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        if (await _db.Users.AnyAsync(u => u.Email == normalizedEmail, cancellationToken))
        {
            throw new ServiceValidationException("A user with this email already exists.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            DisplayName = request.DisplayName.Trim(),
            Role = UserRole.User,
            CreatedAtUtc = DateTime.UtcNow
        };
        user.PasswordHash = _passwordHasher.Hash(user, request.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        return BuildAuthResponse(user);
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (user is null || !_passwordHasher.Verify(user, request.Password, user.PasswordHash))
        {
            return null;
        }

        return BuildAuthResponse(user);
    }

    public async Task<UserProfileDto?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        return user is null ? null : MapProfile(user);
    }

    public async Task<IReadOnlyList<AdminUserListItemDto>> ListUsersForAdminAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.Users
            .AsNoTracking()
            .OrderBy(u => u.Email)
            .Select(u => new AdminUserListItemDto(u.Id, u.Email, u.DisplayName, u.Role, u.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    private AuthResponse BuildAuthResponse(User user)
    {
        var (token, expires) = _jwtTokenGenerator.CreateAccessToken(user);
        return new AuthResponse(token, expires, MapProfile(user));
    }

    private static UserProfileDto MapProfile(User user) => new(
        user.Id,
        user.Email,
        user.DisplayName,
        user.Role);
}
