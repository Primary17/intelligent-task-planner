using System.Net;
using System.Net.Http.Json;
using TaskPlanner.Application.DTOs.Disciplines;
using TaskPlanner.Application.DTOs.StudySessions;
using TaskPlanner.Application.DTOs.StudyTasks;
using TaskPlanner.Domain.Enums;

namespace TaskPlanner.Tests.Api;

[Collection(ApiCollection.Name)]
public sealed class SessionTests
{
    private static readonly DateTime Base = DateTime.UtcNow.Date.AddDays(10).AddHours(10);

    private readonly ApiFactory _factory;

    public SessionTests(ApiFactory factory) => _factory = factory;

    private static async Task<StudyTaskDto> CreateTaskAsync(HttpClient client)
    {
        var discipline = await (await client.PostJsonAsync("/api/disciplines",
            new CreateDisciplineRequest("OS", null, "#112233"))).ReadAsync<DisciplineDto>();
        return await (await client.PostJsonAsync("/api/tasks", new CreateStudyTaskRequest(
            discipline.Id, "Lab 3", DateTime.UtcNow.AddDays(20), 3, TaskPriority.High, 360))).ReadAsync<StudyTaskDto>();
    }

    private static Task<HttpResponseMessage> CreateSessionAsync(HttpClient client, Guid taskId, int startHours, int minutes) =>
        client.PostJsonAsync("/api/sessions", new CreateStudySessionRequest(
            taskId, Base.AddHours(startHours), Base.AddHours(startHours).AddMinutes(minutes)));

    private static UpdateStudySessionRequest Update(StudySessionDto s, SessionStatus status, int? actual = null) =>
        new(s.StartAtUtc, s.EndAtUtc, status, actual, s.IsLocked);

    private static async Task<StudyTaskDto> GetTaskAsync(HttpClient client, Guid id) =>
        await (await client.GetAsync($"/api/tasks/{id}")).ReadAsync<StudyTaskDto>();

    [Fact]
    public async Task Crud_FullCycle()
    {
        var client = await _factory.CreateUserClientAsync();
        var task = await CreateTaskAsync(client);

        var created = await CreateSessionAsync(client, task.Id, 0, 120);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var session = await created.ReadAsync<StudySessionDto>();
        Assert.Equal(120, session.PlannedMinutes);
        Assert.Equal(SessionStatus.Planned, session.Status);
        Assert.Null(session.ActualMinutes);
        Assert.Equal("Lab 3", session.TaskTitle);
        Assert.Equal("OS", session.DisciplineName);

        var updated = await client.PutJsonAsync($"/api/sessions/{session.Id}", Update(session, SessionStatus.Completed, 90));
        var done = await updated.ReadAsync<StudySessionDto>();
        Assert.Equal(SessionStatus.Completed, done.Status);
        Assert.Equal(90, done.ActualMinutes);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/sessions/{session.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/sessions/{session.Id}")).StatusCode);
    }

    [Fact]
    public async Task Complete_AddsLoggedTimeToTask_AndStartsIt()
    {
        var client = await _factory.CreateUserClientAsync();
        var task = await CreateTaskAsync(client);
        var session = await (await CreateSessionAsync(client, task.Id, 0, 120)).ReadAsync<StudySessionDto>();

        await client.PutJsonAsync($"/api/sessions/{session.Id}", Update(session, SessionStatus.Completed, 90));

        var afterComplete = await GetTaskAsync(client, task.Id);
        Assert.Equal(90, afterComplete.ActualMinutes);
        Assert.Equal(StudyTaskStatus.InProgress, afterComplete.Status);

        await client.PutJsonAsync($"/api/sessions/{session.Id}", Update(session, SessionStatus.Missed));

        var afterMissed = await GetTaskAsync(client, task.Id);
        Assert.Equal(0, afterMissed.ActualMinutes);
    }

    [Fact]
    public async Task Complete_WithoutActual_UsesPlannedMinutes()
    {
        var client = await _factory.CreateUserClientAsync();
        var task = await CreateTaskAsync(client);
        var session = await (await CreateSessionAsync(client, task.Id, 0, 60)).ReadAsync<StudySessionDto>();

        var done = await (await client.PutJsonAsync($"/api/sessions/{session.Id}",
            Update(session, SessionStatus.Completed))).ReadAsync<StudySessionDto>();

        Assert.Equal(60, done.ActualMinutes);
        Assert.Equal(60, (await GetTaskAsync(client, task.Id)).ActualMinutes);
    }

    [Fact]
    public async Task Create_Overlapping_ReturnsBadRequest()
    {
        var client = await _factory.CreateUserClientAsync();
        var task = await CreateTaskAsync(client);
        await CreateSessionAsync(client, task.Id, 0, 120);

        var overlapping = await CreateSessionAsync(client, task.Id, 1, 60);
        var adjacent = await CreateSessionAsync(client, task.Id, 2, 60);

        Assert.Equal(HttpStatusCode.BadRequest, overlapping.StatusCode);
        Assert.Equal(HttpStatusCode.Created, adjacent.StatusCode);
    }

    [Fact]
    public async Task Update_MovingOntoAnotherSession_ReturnsBadRequest()
    {
        var client = await _factory.CreateUserClientAsync();
        var task = await CreateTaskAsync(client);
        await CreateSessionAsync(client, task.Id, 0, 60);
        var second = await (await CreateSessionAsync(client, task.Id, 3, 60)).ReadAsync<StudySessionDto>();

        var response = await client.PutJsonAsync($"/api/sessions/{second.Id}",
            new UpdateStudySessionRequest(Base, Base.AddMinutes(30), SessionStatus.Planned, null, false));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task List_FiltersByPeriod_AndSortsByStart()
    {
        var client = await _factory.CreateUserClientAsync();
        var task = await CreateTaskAsync(client);
        await CreateSessionAsync(client, task.Id, 30, 60);
        await CreateSessionAsync(client, task.Id, 0, 60);
        await CreateSessionAsync(client, task.Id, 100, 60);

        var from = Uri.EscapeDataString(Base.AddHours(-1).ToString("o"));
        var to = Uri.EscapeDataString(Base.AddHours(40).ToString("o"));
        var items = await (await client.GetAsync($"/api/sessions?from={from}&to={to}"))
            .ReadAsync<List<StudySessionDto>>();

        Assert.Equal(2, items.Count);
        Assert.True(items[0].StartAtUtc < items[1].StartAtUtc);

        var all = await (await client.GetAsync("/api/sessions")).ReadAsync<List<StudySessionDto>>();
        Assert.Equal(3, all.Count);
    }

    [Fact]
    public async Task List_ShowsOnlyOwnSessions()
    {
        var alice = await _factory.CreateUserClientAsync();
        var bob = await _factory.CreateUserClientAsync();
        await CreateSessionAsync(alice, (await CreateTaskAsync(alice)).Id, 0, 60);
        await CreateSessionAsync(bob, (await CreateTaskAsync(bob)).Id, 0, 60);

        var aliceItems = await (await alice.GetAsync("/api/sessions")).ReadAsync<List<StudySessionDto>>();
        var bobItems = await (await bob.GetAsync("/api/sessions")).ReadAsync<List<StudySessionDto>>();

        Assert.Single(aliceItems);
        Assert.Single(bobItems);
        Assert.NotEqual(aliceItems[0].Id, bobItems[0].Id);
    }

    [Fact]
    public async Task ForeignTaskAndSession_AreForbidden()
    {
        var owner = await _factory.CreateUserClientAsync();
        var stranger = await _factory.CreateUserClientAsync();
        var task = await CreateTaskAsync(owner);
        var session = await (await CreateSessionAsync(owner, task.Id, 0, 60)).ReadAsync<StudySessionDto>();

        Assert.Equal(HttpStatusCode.Forbidden, (await CreateSessionAsync(stranger, task.Id, 5, 60)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.GetAsync($"/api/sessions/{session.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await stranger.PutJsonAsync($"/api/sessions/{session.Id}", Update(session, SessionStatus.Completed))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.DeleteAsync($"/api/sessions/{session.Id}")).StatusCode);
    }

    [Fact]
    public async Task Create_EndBeforeStart_ReturnsBadRequest()
    {
        var client = await _factory.CreateUserClientAsync();
        var task = await CreateTaskAsync(client);

        var response = await client.PostJsonAsync("/api/sessions",
            new CreateStudySessionRequest(task.Id, Base.AddHours(2), Base));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_UnknownTask_ReturnsBadRequest()
    {
        var client = await _factory.CreateUserClientAsync();

        var response = await CreateSessionAsync(client, Guid.NewGuid(), 0, 60);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Sessions_RequireAuthentication()
    {
        var response = await _factory.CreateClient().GetAsync("/api/sessions");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeletingTask_RemovesItsSessions()
    {
        var client = await _factory.CreateUserClientAsync();
        var task = await CreateTaskAsync(client);
        await CreateSessionAsync(client, task.Id, 0, 60);

        await client.DeleteAsync($"/api/tasks/{task.Id}");

        var items = await (await client.GetAsync("/api/sessions")).ReadAsync<List<StudySessionDto>>();
        Assert.Empty(items);
    }
}
