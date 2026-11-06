using System.Net;
using System.Net.Http.Json;
using TaskPlanner.Application.DTOs.Auth;

namespace TaskPlanner.Tests.Api;

[Collection(ApiCollection.Name)]
public sealed class ProfileTests
{
    private const string Password = "Password123!";

    private readonly ApiFactory _factory;

    public ProfileTests(ApiFactory factory) => _factory = factory;

    private static string NewEmail() => $"profile-{Guid.NewGuid():N}@test.local";

    [Fact]
    public async Task UpdateName_TrimsAndPersists()
    {
        var client = await _factory.CreateUserClientAsync();

        var response = await client.PutJsonAsync("/api/auth/me", new UpdateProfileRequest("  Olena K.  "));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Olena K.", (await response.ReadAsync<UserProfileDto>()).DisplayName);
        var me = await (await client.GetAsync("/api/auth/me")).ReadAsync<UserProfileDto>();
        Assert.Equal("Olena K.", me.DisplayName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateName_Empty_ReturnsBadRequest(string name)
    {
        var client = await _factory.CreateUserClientAsync();

        var response = await client.PutJsonAsync("/api/auth/me", new UpdateProfileRequest(name));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_NewPasswordWorksAndOldOneDoesNot()
    {
        var email = NewEmail();
        var client = await _factory.CreateUserClientAsync(email);

        var response = await client.PostJsonAsync("/api/auth/change-password",
            new ChangePasswordRequest(Password, "NewPassword456!"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var anonymous = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK,
            (await anonymous.PostJsonAsync("/api/auth/login", new LoginRequest(email, "NewPassword456!"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await anonymous.PostJsonAsync("/api/auth/login", new LoginRequest(email, Password))).StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WrongCurrent_ReturnsBadRequestNotUnauthorized()
    {
        var client = await _factory.CreateUserClientAsync();

        var response = await client.PostJsonAsync("/api/auth/change-password",
            new ChangePasswordRequest("WrongPassword1!", "NewPassword456!"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("Password123!")]
    public async Task ChangePassword_WeakOrSame_ReturnsBadRequest(string newPassword)
    {
        var client = await _factory.CreateUserClientAsync();

        var response = await client.PostJsonAsync("/api/auth/change-password",
            new ChangePasswordRequest(Password, newPassword));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ProfileEndpoints_RequireAuthentication()
    {
        var anonymous = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized,
            (await anonymous.PutJsonAsync("/api/auth/me", new UpdateProfileRequest("X"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await anonymous.PostJsonAsync("/api/auth/change-password", new ChangePasswordRequest("a", "b"))).StatusCode);
    }
}
