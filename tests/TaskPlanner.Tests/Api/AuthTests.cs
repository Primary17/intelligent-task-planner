using System.Net;
using TaskPlanner.Application.DTOs.Auth;

namespace TaskPlanner.Tests.Api;

[Collection(ApiCollection.Name)]
public sealed class AuthTests
{
    private readonly ApiFactory _factory;

    public AuthTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Register_ReturnsTokenAndProfile()
    {
        var client = _factory.CreateClient();
        var email = $"new-{Guid.NewGuid():N}@test.local";

        var response = await client.PostJsonAsync("/api/auth/register", new RegisterRequest(email, "Anna", "Password123!"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var auth = await response.ReadAsync<AuthResponse>();
        Assert.False(string.IsNullOrEmpty(auth.AccessToken));
        Assert.Equal(email, auth.User.Email);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var request = new RegisterRequest($"dup-{Guid.NewGuid():N}@test.local", "Anna", "Password123!");

        await client.PostJsonAsync("/api/auth/register", request);
        var second = await client.PostJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task Register_ShortPassword_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostJsonAsync("/api/auth/register",
            new RegisterRequest($"x-{Guid.NewGuid():N}@test.local", "Anna", "short"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        var email = $"login-{Guid.NewGuid():N}@test.local";
        var client = _factory.CreateClient();
        await client.PostJsonAsync("/api/auth/register", new RegisterRequest(email, "Anna", "Password123!"));

        var response = await client.PostJsonAsync("/api/auth/login", new LoginRequest(email, "WrongPassword1!"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_EmailIsCaseInsensitive()
    {
        var email = $"case-{Guid.NewGuid():N}@test.local";
        var client = _factory.CreateClient();
        await client.PostJsonAsync("/api/auth/register", new RegisterRequest(email, "Anna", "Password123!"));

        var response = await client.PostJsonAsync("/api/auth/login", new LoginRequest(email.ToUpperInvariant(), "Password123!"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _factory.CreateClient().GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithToken_ReturnsProfile()
    {
        var client = await _factory.CreateUserClientAsync();

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Test User", (await response.ReadAsync<UserProfileDto>()).DisplayName);
    }

    [Fact]
    public async Task AdminUsers_RegularUser_IsForbidden()
    {
        var client = await _factory.CreateUserClientAsync();

        var response = await client.GetAsync("/api/admin/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminUsers_Admin_ReturnsList()
    {
        var admin = await _factory.CreateAdminClientAsync();

        var response = await admin.GetAsync("/api/admin/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var users = await response.ReadAsync<List<AdminUserListItemDto>>();
        Assert.Contains(users, u => u.Email == "admin@taskplanner.local");
    }
}
