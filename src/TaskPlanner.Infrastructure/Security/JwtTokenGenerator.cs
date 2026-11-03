using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TaskPlanner.Application.Security;
using TaskPlanner.Domain.Entities;
using TaskPlanner.Domain.Enums;

namespace TaskPlanner.Infrastructure.Security;

public interface IJwtTokenGenerator
{
    (string Token, DateTime ExpiresAtUtc) CreateAccessToken(User user);
}

public sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtOptions _options;
    private readonly byte[] _secret;

    public JwtTokenGenerator(IOptions<JwtOptions> options, IConfiguration configuration)
    {
        _options = options.Value;
        var secret = configuration["Jwt:Secret"]
                     ?? configuration["JWT_SECRET"]
                     ?? throw new InvalidOperationException(
                         "JWT secret is not configured. Set Jwt:Secret or environment variable JWT_SECRET.");

        _secret = Encoding.UTF8.GetBytes(secret);
        if (_secret.Length < 32)
        {
            throw new InvalidOperationException("JWT secret must be at least 32 bytes.");
        }
    }

    public (string Token, DateTime ExpiresAtUtc) CreateAccessToken(User user)
    {
        var expires = DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes);
        var role = user.Role == UserRole.Admin ? AuthRoles.Admin : AuthRoles.User;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Role, role)
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(_secret),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
