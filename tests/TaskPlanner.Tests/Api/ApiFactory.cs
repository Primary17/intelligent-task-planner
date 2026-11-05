using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using TaskPlanner.Application.DTOs.Auth;

namespace TaskPlanner.Tests.Api;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"taskplanner-tests-{Guid.NewGuid():N}.db");

    public ApiFactory()
    {
        Environment.SetEnvironmentVariable("JWT_SECRET", "Tests_JwtSecretKey_Min32CharactersLong!!");
        Environment.SetEnvironmentVariable("Database__Provider", "Sqlite");
        Environment.SetEnvironmentVariable("ConnectionStrings__Sqlite", $"Data Source={_dbPath}");
    }

    public async Task<HttpClient> CreateUserClientAsync(string? email = null)
    {
        email ??= $"user-{Guid.NewGuid():N}@test.local";
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, "Test User", "Password123!"), Json);
        response.EnsureSuccessStatusCode();
        return await Authorize(client, response);
    }

    public async Task<HttpClient> CreateAdminClientAsync()
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("admin@taskplanner.local", "Admin123!"), Json);
        response.EnsureSuccessStatusCode();
        return await Authorize(client, response);
    }

    private static async Task<HttpClient> Authorize(HttpClient client, HttpResponseMessage response)
    {
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(Json);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        foreach (var suffix in new[] { "", "-shm", "-wal" })
        {
            try { File.Delete(_dbPath + suffix); } catch (IOException) { }
        }
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}

public static class HttpExtensions
{
    public static Task<HttpResponseMessage> PostJsonAsync<T>(this HttpClient client, string url, T body) =>
        client.PostAsJsonAsync(url, body, ApiFactory.Json);

    public static Task<HttpResponseMessage> PutJsonAsync<T>(this HttpClient client, string url, T body) =>
        client.PutAsJsonAsync(url, body, ApiFactory.Json);

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(ApiFactory.Json))!;
}
