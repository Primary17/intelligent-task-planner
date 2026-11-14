using System.Net;
using TaskPlanner.Application.DTOs.Availability;
using TaskPlanner.Application.DTOs.Disciplines;
using TaskPlanner.Application.DTOs.Planning;
using TaskPlanner.Application.DTOs.StudyTasks;
using TaskPlanner.Domain.Enums;

namespace TaskPlanner.Tests.Api;

[Collection(ApiCollection.Name)]
public sealed class CapacityTests
{
    private readonly ApiFactory _factory;

    public CapacityTests(ApiFactory factory) => _factory = factory;

    private static async Task<StudyTaskDto> CreateTaskAsync(
        HttpClient client, int estimatedMinutes, int daysAhead, string title = "Lab")
    {
        var discipline = await (await client.PostJsonAsync("/api/disciplines",
            new CreateDisciplineRequest("OS", null, "#112233"))).ReadAsync<DisciplineDto>();
        return await (await client.PostJsonAsync("/api/tasks", new CreateStudyTaskRequest(
            discipline.Id, title, DateTime.UtcNow.AddDays(daysAhead), 3, TaskPriority.High, estimatedMinutes)))
            .ReadAsync<StudyTaskDto>();
    }

    private static async Task AddDailyWindowsAsync(HttpClient client, int hours)
    {
        foreach (var day in Enum.GetValues<DayOfWeek>())
        {
            var response = await client.PostJsonAsync("/api/availability/windows",
                new AvailabilityWindowRequest(day, new TimeOnly(18, 0), new TimeOnly(18 + hours, 0)));
            response.EnsureSuccessStatusCode();
        }
    }

    private static async Task<CapacityReportDto> GetCapacityAsync(HttpClient client, int offset = 0) =>
        await (await client.GetAsync($"/api/planning/capacity?utcOffsetMinutes={offset}")).ReadAsync<CapacityReportDto>();

    [Fact]
    public async Task WithoutAvailability_TaskIsShortfall()
    {
        var client = await _factory.CreateUserClientAsync();
        var task = await CreateTaskAsync(client, 120, 3);

        var report = await GetCapacityAsync(client);

        var row = Assert.Single(report.Tasks);
        Assert.Equal(task.Id, row.TaskId);
        Assert.Equal(CapacityStatus.Shortfall, row.Status);
        Assert.Equal(0, row.AvailableMinutes);
        Assert.Equal(120, row.ShortfallMinutes);
        Assert.Equal(120, report.TotalShortfallMinutes);
    }

    [Fact]
    public async Task WithEnoughAvailability_TaskIsOk()
    {
        var client = await _factory.CreateUserClientAsync();
        await AddDailyWindowsAsync(client, 2);
        await CreateTaskAsync(client, 120, 6);

        var row = Assert.Single((await GetCapacityAsync(client)).Tasks);

        Assert.Equal(CapacityStatus.Ok, row.Status);
        Assert.True(row.AvailableMinutes >= 120);
    }

    [Fact]
    public async Task HugeTask_ExceedsAvailability()
    {
        var client = await _factory.CreateUserClientAsync();
        await AddDailyWindowsAsync(client, 1);
        await CreateTaskAsync(client, 60 * 100, 5);

        var row = Assert.Single((await GetCapacityAsync(client)).Tasks);

        Assert.Equal(CapacityStatus.Shortfall, row.Status);
        Assert.True(row.ShortfallMinutes > 0);
    }

    [Fact]
    public async Task CompletedTasks_AreNotListed()
    {
        var client = await _factory.CreateUserClientAsync();
        var task = await CreateTaskAsync(client, 120, 3);
        await client.PutJsonAsync($"/api/tasks/{task.Id}", new UpdateStudyTaskRequest(
            task.Title, task.DeadlineUtc, 3, TaskPriority.High, 120, 120, StudyTaskStatus.Completed));

        Assert.Empty((await GetCapacityAsync(client)).Tasks);
    }

    [Fact]
    public async Task PastDeadline_IsOverdue()
    {
        var client = await _factory.CreateUserClientAsync();
        var task = await CreateTaskAsync(client, 120, 3);
        await client.PutJsonAsync($"/api/tasks/{task.Id}", new UpdateStudyTaskRequest(
            task.Title, DateTime.UtcNow.AddDays(-1), 3, TaskPriority.High, 120, 30, StudyTaskStatus.InProgress));

        var row = Assert.Single((await GetCapacityAsync(client)).Tasks);

        Assert.Equal(CapacityStatus.Overdue, row.Status);
        Assert.Equal(90, row.RemainingMinutes);
    }

    [Fact]
    public async Task OnlyOwnTasksAreCounted()
    {
        var alice = await _factory.CreateUserClientAsync();
        var bob = await _factory.CreateUserClientAsync();
        await CreateTaskAsync(alice, 60, 3);

        Assert.Empty((await GetCapacityAsync(bob)).Tasks);
    }

    [Theory]
    [InlineData(841)]
    [InlineData(-841)]
    public async Task OffsetOutOfRange_ReturnsBadRequest(int offset)
    {
        var client = await _factory.CreateUserClientAsync();

        var response = await client.GetAsync($"/api/planning/capacity?utcOffsetMinutes={offset}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RequiresAuthentication()
    {
        var response = await _factory.CreateClient().GetAsync("/api/planning/capacity");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
