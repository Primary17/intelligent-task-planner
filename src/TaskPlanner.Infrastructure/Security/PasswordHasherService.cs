using Microsoft.AspNetCore.Identity;
using TaskPlanner.Domain.Entities;

namespace TaskPlanner.Infrastructure.Security;

public interface IPasswordHasherService
{
    string Hash(User user, string password);
    bool Verify(User user, string password, string passwordHash);
}

public sealed class PasswordHasherService : IPasswordHasherService
{
    private readonly PasswordHasher<User> _hasher = new();

    public string Hash(User user, string password) => _hasher.HashPassword(user, password);

    public bool Verify(User user, string password, string passwordHash) =>
        _hasher.VerifyHashedPassword(user, passwordHash, password) != PasswordVerificationResult.Failed;
}
