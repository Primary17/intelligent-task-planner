using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TaskPlanner.Maui.Api;

public sealed class TaskPlannerApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly HttpClient _http;

    public TaskPlannerApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/auth/login", request, JsonOptions, ct).ConfigureAwait(false);
        return await ReadAuthOrThrow(response, ct).ConfigureAwait(false);
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/auth/register", request, JsonOptions, ct).ConfigureAwait(false);
        return await ReadAuthOrThrow(response, ct).ConfigureAwait(false);
    }

    public async Task<UserProfileDto?> MeAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync("api/auth/me", ct).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserProfileDto>(JsonOptions, ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<DisciplineDto>> GetDisciplinesAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync("api/disciplines", ct).ConfigureAwait(false);
        await EnsureSuccessOrProblemAsync(response, ct).ConfigureAwait(false);
        var list = await response.Content.ReadFromJsonAsync<List<DisciplineDto>>(JsonOptions, ct).ConfigureAwait(false);
        return list ?? [];
    }

    public async Task<DisciplineDto?> GetDisciplineAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"api/disciplines/{id}", ct).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessOrProblemAsync(response, ct).ConfigureAwait(false);
        return await response.Content.ReadFromJsonAsync<DisciplineDto>(JsonOptions, ct).ConfigureAwait(false);
    }

    public async Task<DisciplineDto> CreateDisciplineAsync(CreateDisciplineRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/disciplines", request, JsonOptions, ct).ConfigureAwait(false);
        await EnsureSuccessOrProblemAsync(response, ct).ConfigureAwait(false);
        return (await response.Content.ReadFromJsonAsync<DisciplineDto>(JsonOptions, ct).ConfigureAwait(false))!;
    }

    public async Task<DisciplineDto?> UpdateDisciplineAsync(Guid id, UpdateDisciplineRequest request, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync($"api/disciplines/{id}", request, JsonOptions, ct).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessOrProblemAsync(response, ct).ConfigureAwait(false);
        return await response.Content.ReadFromJsonAsync<DisciplineDto>(JsonOptions, ct).ConfigureAwait(false);
    }

    public async Task<bool> DeleteDisciplineAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"api/disciplines/{id}", ct).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }

        await EnsureSuccessOrProblemAsync(response, ct).ConfigureAwait(false);
        return true;
    }

    public async Task<IReadOnlyList<StudyTaskDto>> GetTasksAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync("api/tasks", ct).ConfigureAwait(false);
        await EnsureSuccessOrProblemAsync(response, ct).ConfigureAwait(false);
        var list = await response.Content.ReadFromJsonAsync<List<StudyTaskDto>>(JsonOptions, ct).ConfigureAwait(false);
        return list ?? [];
    }

    public async Task<StudyTaskDto?> GetTaskAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"api/tasks/{id}", ct).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessOrProblemAsync(response, ct).ConfigureAwait(false);
        return await response.Content.ReadFromJsonAsync<StudyTaskDto>(JsonOptions, ct).ConfigureAwait(false);
    }

    public async Task<StudyTaskDto> CreateTaskAsync(CreateStudyTaskRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/tasks", request, JsonOptions, ct).ConfigureAwait(false);
        await EnsureSuccessOrProblemAsync(response, ct).ConfigureAwait(false);
        return (await response.Content.ReadFromJsonAsync<StudyTaskDto>(JsonOptions, ct).ConfigureAwait(false))!;
    }

    public async Task<StudyTaskDto?> UpdateTaskAsync(Guid id, UpdateStudyTaskRequest request, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync($"api/tasks/{id}", request, JsonOptions, ct).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessOrProblemAsync(response, ct).ConfigureAwait(false);
        return await response.Content.ReadFromJsonAsync<StudyTaskDto>(JsonOptions, ct).ConfigureAwait(false);
    }

    public async Task<bool> DeleteTaskAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"api/tasks/{id}", ct).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }

        await EnsureSuccessOrProblemAsync(response, ct).ConfigureAwait(false);
        return true;
    }

    private static async Task<AuthResponse?> ReadAuthOrThrow(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw await ApiException.FromResponseAsync(response, ct).ConfigureAwait(false);
        }

        await EnsureSuccessOrProblemAsync(response, ct).ConfigureAwait(false);
        return await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions, ct).ConfigureAwait(false);
    }

    private static async Task EnsureSuccessOrProblemAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        throw await ApiException.FromResponseAsync(response, ct).ConfigureAwait(false);
    }
}

public sealed class ApiException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public string? ProblemTitle { get; }
    public string? ProblemDetail { get; }

    public ApiException(HttpStatusCode statusCode, string? title, string? detail, string message)
        : base(message)
    {
        StatusCode = statusCode;
        ProblemTitle = title;
        ProblemDetail = detail;
    }

    public static async Task<ApiException> FromResponseAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var status = response.StatusCode;
        string? title = null;
        string? detail = null;
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
            if (doc.RootElement.TryGetProperty("title", out var t))
            {
                title = t.GetString();
            }

            if (doc.RootElement.TryGetProperty("detail", out var d))
            {
                detail = d.GetString();
            }
        }
        catch
        {
            // залишаємо title/detail порожніми
        }

        var message = !string.IsNullOrWhiteSpace(detail)
            ? detail!
            : (!string.IsNullOrWhiteSpace(title) ? title! : $"HTTP {(int)status} {status}");

        return new ApiException(status, title, detail, message);
    }
}
