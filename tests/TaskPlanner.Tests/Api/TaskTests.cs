using System.Net;
using TaskPlanner.Application.DTOs.Disciplines;
using TaskPlanner.Application.DTOs.StudyTasks;
using TaskPlanner.Domain.Enums;

namespace TaskPlanner.Tests.Api;

[Collection(ApiCollection.Name)]
public sealed class TaskTests
{
    private readonly ApiFactory _factory;

    public TaskTests(ApiFactory factory) => _factory = factory;

    private static async Task<DisciplineDto> CreateDisciplineAsync(HttpClient client, string name = "OS") =>
        await (await client.PostJsonAsync("/api/disciplines", new CreateDisciplineRequest(name, null, "#112233")))
            .ReadAsync<DisciplineDto>();

    private static CreateStudyTaskRequest NewTask(Guid disciplineId, string title = "Lab 3", int complexity = 3) =>
        new(disciplineId, title, DateTime.UtcNow.AddDays(3), complexity, TaskPriority.High, 360);

    [Fact]
    public async Task Crud_FullCycle()
    {
        var client = await _factory.CreateUserClientAsync();
        var discipline = await CreateDisciplineAsync(client);

        var createdResponse = await client.PostJsonAsync("/api/tasks", NewTask(discipline.Id));
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var task = await createdResponse.ReadAsync<StudyTaskDto>();
        Assert.Equal("OS", task.DisciplineName);
        Assert.Equal(StudyTaskStatus.NotStarted, task.Status);

        var updated = await client.PutJsonAsync($"/api/tasks/{task.Id}", new UpdateStudyTaskRequest(
            "Lab 3 (done)", task.DeadlineUtc, 3, TaskPriority.High, 360, 120, StudyTaskStatus.InProgress));
        var updatedTask = await updated.ReadAsync<StudyTaskDto>();
        Assert.Equal(120, updatedTask.ActualMinutes);
        Assert.Equal(StudyTaskStatus.InProgress, updatedTask.Status);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/tasks/{task.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/tasks/{task.Id}")).StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task Create_ComplexityOutOfRange_ReturnsBadRequest(int complexity)
    {
        var client = await _factory.CreateUserClientAsync();
        var discipline = await CreateDisciplineAsync(client);

        var response = await client.PostJsonAsync("/api/tasks", NewTask(discipline.Id, complexity: complexity));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_DeadlineInPast_ReturnsBadRequest()
    {
        var client = await _factory.CreateUserClientAsync();
        var discipline = await CreateDisciplineAsync(client);

        var response = await client.PostJsonAsync("/api/tasks",
            NewTask(discipline.Id) with { DeadlineUtc = DateTime.UtcNow.AddDays(-1) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_AfterDeadline_IsAllowed()
    {
        var client = await _factory.CreateUserClientAsync();
        var discipline = await CreateDisciplineAsync(client);
        var task = await (await client.PostJsonAsync("/api/tasks", NewTask(discipline.Id))).ReadAsync<StudyTaskDto>();

        var response = await client.PutJsonAsync($"/api/tasks/{task.Id}", new UpdateStudyTaskRequest(
            task.Title, DateTime.UtcNow.AddDays(-1), 3, TaskPriority.High, 360, 360, StudyTaskStatus.Completed));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Create_InForeignDiscipline_IsForbidden()
    {
        var owner = await _factory.CreateUserClientAsync();
        var stranger = await _factory.CreateUserClientAsync();
        var discipline = await CreateDisciplineAsync(owner);

        var response = await stranger.PostJsonAsync("/api/tasks", NewTask(discipline.Id));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ForeignTask_IsForbiddenAndHiddenFromList()
    {
        var owner = await _factory.CreateUserClientAsync();
        var stranger = await _factory.CreateUserClientAsync();
        var discipline = await CreateDisciplineAsync(owner);
        var task = await (await owner.PostJsonAsync("/api/tasks", NewTask(discipline.Id))).ReadAsync<StudyTaskDto>();

        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.GetAsync($"/api/tasks/{task.Id}")).StatusCode);
        Assert.Empty(await (await stranger.GetAsync("/api/tasks")).ReadAsync<List<StudyTaskDto>>());
    }

    [Fact]
    public async Task List_FiltersAndSorts()
    {
        var client = await _factory.CreateUserClientAsync();
        var discipline = await CreateDisciplineAsync(client);
        await client.PostJsonAsync("/api/tasks", NewTask(discipline.Id, "Alpha") with { Priority = TaskPriority.Low });
        await client.PostJsonAsync("/api/tasks", NewTask(discipline.Id, "Beta") with { Priority = TaskPriority.Critical });
        await client.PostJsonAsync("/api/tasks", NewTask(discipline.Id, "Gamma") with { Priority = TaskPriority.Medium });

        var byPriority = await (await client.GetAsync("/api/tasks?sort=priority")).ReadAsync<List<StudyTaskDto>>();
        Assert.Equal(new[] { "Beta", "Gamma", "Alpha" }, byPriority.Select(t => t.Title));

        var filtered = await (await client.GetAsync("/api/tasks?minPriority=High")).ReadAsync<List<StudyTaskDto>>();
        Assert.Equal("Beta", Assert.Single(filtered).Title);

        var searched = await (await client.GetAsync("/api/tasks?search=alp")).ReadAsync<List<StudyTaskDto>>();
        Assert.Equal("Alpha", Assert.Single(searched).Title);
    }

    [Fact]
    public async Task Search_IsCaseInsensitiveForCyrillic()
    {
        var client = await _factory.CreateUserClientAsync();
        var discipline = await CreateDisciplineAsync(client);
        await client.PostJsonAsync("/api/tasks", NewTask(discipline.Id, "Лабораторна робота №3"));

        var found = await (await client.GetAsync("/api/tasks?search=" + Uri.EscapeDataString("ЛАБОР"))).ReadAsync<List<StudyTaskDto>>();

        Assert.Single(found);
    }

    [Fact]
    public async Task DeletingDiscipline_RemovesItsTasks()
    {
        var client = await _factory.CreateUserClientAsync();
        var discipline = await CreateDisciplineAsync(client);
        var task = await (await client.PostJsonAsync("/api/tasks", NewTask(discipline.Id))).ReadAsync<StudyTaskDto>();

        await client.DeleteAsync($"/api/disciplines/{discipline.Id}");

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/tasks/{task.Id}")).StatusCode);
    }
}
