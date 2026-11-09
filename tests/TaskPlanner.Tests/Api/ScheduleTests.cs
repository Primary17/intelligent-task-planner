using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskPlanner.Application.DTOs.Availability;
using TaskPlanner.Application.DTOs.Disciplines;
using TaskPlanner.Application.DTOs.Scheduling;
using TaskPlanner.Application.DTOs.StudyTasks;
using TaskPlanner.Domain.Entities;
using TaskPlanner.Domain.Enums;
using TaskPlanner.Infrastructure.Persistence;

namespace TaskPlanner.Tests.Api;

[Collection(ApiCollection.Name)]
public sealed class ScheduleTests
{
    private static readonly DateTime Monday = new(2026, 11, 16, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime HorizonEnd = Monday.AddDays(7);

    private readonly ApiFactory _factory;

    public ScheduleTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Generate_PlacesSessionInsideFreeHours()
    {
        var client = await _factory.CreateUserClientAsync();
        var task = await CreateTaskAsync(client, estimatedMinutes: 60, deadline: Monday.AddDays(3));
        await client.PostJsonAsync("/api/availability/windows", Window(DayOfWeek.Monday, 17, 19));

        var plan = await GenerateAsync(client);

        var session = Assert.Single(plan.Sessions);
        Assert.Equal(task.Id, session.StudyTaskId);
        Assert.Equal(Monday.AddHours(17), session.StartAtUtc);
        Assert.Equal(Monday.AddHours(18), session.EndAtUtc);
        Assert.Equal(60, session.PlannedMinutes);
        Assert.Equal(SessionStatus.Planned, session.Status);
        Assert.Empty(plan.Unscheduled);
        Assert.Equal(1, await CountSessionsAsync(task.Id));
    }

    [Fact]
    public async Task Generate_ReportsMinutesThatDoNotFit()
    {
        var client = await _factory.CreateUserClientAsync();
        var task = await CreateTaskAsync(client, estimatedMinutes: 180, deadline: Monday.AddDays(1));
        await client.PostJsonAsync("/api/availability/windows", Window(DayOfWeek.Monday, 17, 18));

        var plan = await GenerateAsync(client);

        var session = Assert.Single(plan.Sessions);
        Assert.Equal(60, session.PlannedMinutes);
        var left = Assert.Single(plan.Unscheduled);
        Assert.Equal(task.Id, left.StudyTaskId);
        Assert.Equal(180, left.RemainingMinutes);
        Assert.Equal(60, left.ScheduledMinutes);
    }

    [Fact]
    public async Task Generate_DoesNotScheduleAnotherUsersTasks()
    {
        var owner = await _factory.CreateUserClientAsync();
        var stranger = await _factory.CreateUserClientAsync();
        var task = await CreateTaskAsync(owner, estimatedMinutes: 60, deadline: Monday.AddDays(3));
        await owner.PostJsonAsync("/api/availability/windows", Window(DayOfWeek.Monday, 17, 19));
        await stranger.PostJsonAsync("/api/availability/windows", Window(DayOfWeek.Monday, 17, 19));

        var plan = await GenerateAsync(stranger);

        Assert.Empty(plan.Sessions);
        Assert.DoesNotContain(plan.Unscheduled, item => item.StudyTaskId == task.Id);
        Assert.Equal(0, await CountSessionsAsync(task.Id));
    }

    [Fact]
    public async Task Generate_RepeatedCallReplacesPlannedSessions()
    {
        var client = await _factory.CreateUserClientAsync();
        var task = await CreateTaskAsync(client, estimatedMinutes: 60, deadline: Monday.AddDays(3));
        await client.PostJsonAsync("/api/availability/windows", Window(DayOfWeek.Monday, 17, 19));

        var first = await GenerateAsync(client);
        var second = await GenerateAsync(client);

        Assert.Equal(first.Sessions[0].StartAtUtc, Assert.Single(second.Sessions).StartAtUtc);
        Assert.NotEqual(first.Sessions[0].Id, second.Sessions[0].Id);
        Assert.Equal(1, await CountSessionsAsync(task.Id));
    }

    [Fact]
    public async Task Generate_KeepsLockedSession()
    {
        var client = await _factory.CreateUserClientAsync();
        var task = await CreateTaskAsync(client, estimatedMinutes: 120, deadline: Monday.AddDays(3));
        await client.PostJsonAsync("/api/availability/windows", Window(DayOfWeek.Monday, 17, 19));
        var lockedId = await InsertSessionAsync(task.Id, Monday.AddHours(17), Monday.AddHours(18), isLocked: true);

        var plan = await GenerateAsync(client);

        var created = Assert.Single(plan.Sessions);
        Assert.Equal(Monday.AddHours(18), created.StartAtUtc);
        Assert.NotEqual(lockedId, created.Id);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContextBase>();
        var stored = await db.StudySessions.Where(s => s.StudyTaskId == task.Id).ToListAsync();
        Assert.Equal(2, stored.Count);
        Assert.Contains(stored, s => s.Id == lockedId && s.IsLocked);
    }

    [Fact]
    public async Task Generate_InvalidHorizon_ReturnsBadRequest()
    {
        var client = await _factory.CreateUserClientAsync();

        var reversed = await client.PostJsonAsync("/api/schedule/generate", new GenerateScheduleRequest(
            HorizonEnd, Monday, null, null));
        var both = await client.PostJsonAsync("/api/schedule/generate", new GenerateScheduleRequest(
            Monday, HorizonEnd, 7, null));
        var zone = await client.PostJsonAsync("/api/schedule/generate", new GenerateScheduleRequest(
            Monday, HorizonEnd, null, "Not/AZone"));

        Assert.Equal(HttpStatusCode.BadRequest, reversed.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, both.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, zone.StatusCode);
    }

    [Fact]
    public async Task Generate_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostJsonAsync("/api/schedule/generate", new GenerateScheduleRequest(
            Monday, HorizonEnd, null, null));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static AvailabilityWindowRequest Window(DayOfWeek day, int fromHour, int toHour) =>
        new(day, new TimeOnly(fromHour, 0), new TimeOnly(toHour, 0));

    private static async Task<StudyTaskDto> CreateTaskAsync(
        HttpClient client,
        int estimatedMinutes,
        DateTime deadline)
    {
        var discipline = await (await client.PostJsonAsync("/api/disciplines", new CreateDisciplineRequest("OS", null, "#112233")))
            .ReadAsync<DisciplineDto>();
        return await (await client.PostJsonAsync("/api/tasks", new CreateStudyTaskRequest(
            discipline.Id, "Lab", deadline, 3, TaskPriority.High, estimatedMinutes))).ReadAsync<StudyTaskDto>();
    }

    private static async Task<GenerateScheduleResponse> GenerateAsync(HttpClient client)
    {
        var response = await client.PostJsonAsync("/api/schedule/generate", new GenerateScheduleRequest(
            Monday, HorizonEnd, null, "UTC"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.ReadAsync<GenerateScheduleResponse>();
    }

    private async Task<int> CountSessionsAsync(Guid taskId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContextBase>();
        return await db.StudySessions.CountAsync(s => s.StudyTaskId == taskId);
    }

    private async Task<Guid> InsertSessionAsync(Guid taskId, DateTime start, DateTime end, bool isLocked)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContextBase>();
        var session = new StudySession
        {
            Id = Guid.NewGuid(),
            StudyTaskId = taskId,
            StartAtUtc = start,
            EndAtUtc = end,
            PlannedMinutes = 60,
            Status = SessionStatus.Planned,
            IsLocked = isLocked
        };
        db.StudySessions.Add(session);
        await db.SaveChangesAsync();
        return session.Id;
    }
}
