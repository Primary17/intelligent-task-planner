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
    public async Task Generate_UnlockedPlannedSessionOutsideHorizon_StaysAndReducesRemainingWork()
    {
        var client = await _factory.CreateUserClientAsync();
        var task = await CreateTaskAsync(client, estimatedMinutes: 180, deadline: Monday.AddDays(1));
        await client.PostJsonAsync("/api/availability/windows", Window(DayOfWeek.Monday, 17, 18));
        var outsideStart = Monday.AddDays(-1).AddHours(17);
        var outsideId = await InsertSessionAsync(task.Id, outsideStart, outsideStart.AddHours(1), isLocked: false);

        var plan = await GenerateAsync(client);

        var created = Assert.Single(plan.Sessions);
        Assert.NotEqual(outsideId, created.Id);
        Assert.Equal(Monday.AddHours(17), created.StartAtUtc);
        Assert.Equal(60, created.PlannedMinutes);

        var left = Assert.Single(plan.Unscheduled);
        Assert.Equal(task.Id, left.StudyTaskId);
        Assert.Equal(120, left.RemainingMinutes);
        Assert.Equal(60, left.ScheduledMinutes);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContextBase>();
        var stored = await db.StudySessions.Where(s => s.StudyTaskId == task.Id).ToListAsync();
        Assert.Equal(2, stored.Count);
        Assert.Contains(stored, session => session.Id == outsideId && !session.IsLocked && session.PlannedMinutes == 60);
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

    [Fact]
    public async Task Generate_DaysBounds_AreEnforced()
    {
        var client = await _factory.CreateUserClientAsync();

        var tooSmall = await client.PostJsonAsync("/api/schedule/generate", new GenerateScheduleRequest(null, null, 0, null));
        var tooLarge = await client.PostJsonAsync("/api/schedule/generate", new GenerateScheduleRequest(null, null, 93, null));
        var upperBound = await client.PostJsonAsync("/api/schedule/generate", new GenerateScheduleRequest(null, null, 92, "UTC"));
        var emptyRange = await client.PostJsonAsync("/api/schedule/generate", new GenerateScheduleRequest(null, null, null, null));

        Assert.Equal(HttpStatusCode.BadRequest, tooSmall.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, tooLarge.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, emptyRange.StatusCode);
        Assert.Equal(HttpStatusCode.OK, upperBound.StatusCode);
    }

    [Fact]
    public async Task Generate_WithoutWindows_ReportsTheWholeTaskUnscheduled()
    {
        var client = await _factory.CreateUserClientAsync();
        var task = await CreateTaskAsync(client, estimatedMinutes: 60, deadline: Monday.AddDays(3));

        var plan = await GenerateAsync(client);

        Assert.Empty(plan.Sessions);
        var left = Assert.Single(plan.Unscheduled);
        Assert.Equal(task.Id, left.StudyTaskId);
        Assert.Equal(60, left.RemainingMinutes);
        Assert.Equal(0, left.ScheduledMinutes);
        Assert.Equal(0, await CountSessionsAsync(task.Id));
    }

    private static AvailabilityWindowRequest Window(DayOfWeek day, int fromHour, int toHour) =>
        new(day, new TimeOnly(fromHour, 0), new TimeOnly(toHour, 0));

    private static async Task<StudyTaskDto> CreateTaskAsync(
        HttpClient client,
        int estimatedMinutes,
        DateTime deadline,
        string disciplineName = "OS")
    {
        var discipline = await (await client.PostJsonAsync("/api/disciplines", new CreateDisciplineRequest(disciplineName, null, "#112233")))
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

    private async Task<Guid> InsertSessionAsync(
        Guid taskId,
        DateTime start,
        DateTime end,
        bool isLocked = false,
        SessionStatus status = SessionStatus.Planned,
        int plannedMinutes = 60,
        int? actualMinutes = null)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContextBase>();
        var session = new StudySession
        {
            Id = Guid.NewGuid(),
            StudyTaskId = taskId,
            StartAtUtc = start,
            EndAtUtc = end,
            PlannedMinutes = plannedMinutes,
            ActualMinutes = actualMinutes,
            Status = status,
            IsLocked = isLocked
        };
        db.StudySessions.Add(session);
        await db.SaveChangesAsync();
        return session.Id;
    }

    [Fact]
    public async Task Replan_MovesMissedSessionToTheNextFreeDay()
    {
        var client = await _factory.CreateUserClientAsync();
        var task = await CreateTaskAsync(client, estimatedMinutes: 120, deadline: Monday.AddDays(4));
        await client.PostJsonAsync("/api/availability/windows", Window(DayOfWeek.Monday, 17, 19));
        await client.PostJsonAsync("/api/availability/windows", Window(DayOfWeek.Tuesday, 17, 19));
        await client.PostJsonAsync("/api/availability/exceptions", new AvailabilityExceptionRequest(
            DateOnly.FromDateTime(Monday), AvailabilityExceptionKind.Unavailable, null, null, null));
        var missedId = await InsertSessionAsync(
            task.Id, Monday.AddHours(17), Monday.AddHours(19), status: SessionStatus.Missed, plannedMinutes: 120);

        var plan = await ReplanAsync(client);

        Assert.Equal(missedId, Assert.Single(plan.Removed).Id);
        var created = Assert.Single(plan.Created);
        Assert.Equal(Monday.AddDays(1).AddHours(17), created.StartAtUtc);
        Assert.Equal(120, created.PlannedMinutes);
        Assert.Equal(SessionStatus.Planned, created.Status);
        Assert.Empty(plan.Stayed);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContextBase>();
        var stored = await db.StudySessions.SingleAsync(s => s.StudyTaskId == task.Id);
        Assert.Equal(created.Id, stored.Id);
        Assert.Equal(SessionStatus.Planned, stored.Status);
    }

    [Fact]
    public async Task Replan_KeepsCompletedSessionAndPlacesTheRemainder()
    {
        var client = await _factory.CreateUserClientAsync();
        var task = await CreateTaskAsync(client, estimatedMinutes: 120, deadline: Monday.AddDays(3));
        await client.PostJsonAsync("/api/availability/windows", Window(DayOfWeek.Monday, 17, 19));
        var completedId = await InsertSessionAsync(
            task.Id,
            Monday.AddHours(17),
            Monday.AddHours(18),
            status: SessionStatus.Completed,
            actualMinutes: 60);

        var plan = await ReplanAsync(client);

        var stayed = Assert.Single(plan.Stayed);
        Assert.Equal(completedId, stayed.Id);
        Assert.Equal(Monday.AddHours(17), stayed.StartAtUtc);
        Assert.Equal(SessionStatus.Completed, stayed.Status);
        Assert.Empty(plan.Removed);
        var created = Assert.Single(plan.Created);
        Assert.Equal(Monday.AddHours(18), created.StartAtUtc);
        Assert.Equal(60, created.PlannedMinutes);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContextBase>();
        var completed = await db.StudySessions.SingleAsync(s => s.Id == completedId);
        Assert.Equal(SessionStatus.Completed, completed.Status);
        Assert.Equal(Monday.AddHours(17), completed.StartAtUtc);
        Assert.Equal(60, completed.ActualMinutes);
    }

    [Fact]
    public async Task Replan_DoesNotAddTaskActualToTheSameCompletedSession()
    {
        var client = await _factory.CreateUserClientAsync();
        var task = await CreateTaskAsync(client, estimatedMinutes: 180, deadline: Monday.AddDays(4));
        await client.PutJsonAsync($"/api/tasks/{task.Id}", new UpdateStudyTaskRequest(
            "Lab", task.DeadlineUtc, task.Complexity, task.Priority, task.EstimatedMinutes, 60, StudyTaskStatus.InProgress));
        await client.PostJsonAsync("/api/availability/windows", Window(DayOfWeek.Monday, 17, 19));
        await client.PostJsonAsync("/api/availability/windows", Window(DayOfWeek.Tuesday, 17, 19));
        var completedId = await InsertSessionAsync(
            task.Id,
            Monday.AddHours(17),
            Monday.AddHours(18),
            status: SessionStatus.Completed,
            actualMinutes: 60);

        var plan = await ReplanAsync(client);

        Assert.Equal(completedId, Assert.Single(plan.Stayed).Id);
        Assert.Empty(plan.Removed);
        Assert.Equal(120, plan.Created.Sum(session => session.PlannedMinutes));
        Assert.Equal(
            new[] { Monday.AddHours(18), Monday.AddDays(1).AddHours(17) },
            plan.Created.Select(session => session.StartAtUtc));
        Assert.Empty(plan.Unscheduled);
    }

    [Fact]
    public async Task Replan_CountsTheLargerActualMeasurement()
    {
        var client = await _factory.CreateUserClientAsync();
        var task = await CreateTaskAsync(client, estimatedMinutes: 180, deadline: Monday.AddDays(4));
        await client.PutJsonAsync($"/api/tasks/{task.Id}", new UpdateStudyTaskRequest(
            "Lab", task.DeadlineUtc, task.Complexity, task.Priority, task.EstimatedMinutes, 30, StudyTaskStatus.InProgress));
        await client.PostJsonAsync("/api/availability/windows", Window(DayOfWeek.Monday, 17, 19));
        await client.PostJsonAsync("/api/availability/windows", Window(DayOfWeek.Tuesday, 17, 19));
        await InsertSessionAsync(
            task.Id,
            Monday.AddHours(17),
            Monday.AddHours(18),
            status: SessionStatus.Completed,
            actualMinutes: 60);

        var plan = await ReplanAsync(client);

        Assert.Equal(120, plan.Created.Sum(session => session.PlannedMinutes));
        Assert.Empty(plan.Unscheduled);
    }

    [Fact]
    public async Task Replan_PlacesNewTaskInTheNearestSlot()
    {
        var client = await _factory.CreateUserClientAsync();
        var existing = await CreateTaskAsync(client, estimatedMinutes: 60, deadline: Monday.AddDays(4));
        var added = await CreateTaskAsync(client, estimatedMinutes: 60, deadline: Monday.AddDays(1), disciplineName: "DB");
        await client.PostJsonAsync("/api/availability/windows", Window(DayOfWeek.Monday, 17, 19));
        var oldId = await InsertSessionAsync(existing.Id, Monday.AddDays(1).AddHours(17), Monday.AddDays(1).AddHours(18));

        var plan = await ReplanAsync(client);

        Assert.Equal(oldId, Assert.Single(plan.Removed).Id);
        Assert.Equal(added.Id, plan.Created[0].StudyTaskId);
        Assert.Equal(Monday.AddHours(17), plan.Created[0].StartAtUtc);
        Assert.Equal(existing.Id, plan.Created[1].StudyTaskId);
        Assert.Equal(Monday.AddHours(18), plan.Created[1].StartAtUtc);
    }

    [Fact]
    public async Task Replan_DoesNotMoveLockedSession()
    {
        var client = await _factory.CreateUserClientAsync();
        var task = await CreateTaskAsync(client, estimatedMinutes: 120, deadline: Monday.AddDays(3));
        await client.PostJsonAsync("/api/availability/windows", Window(DayOfWeek.Monday, 17, 19));
        var lockedId = await InsertSessionAsync(task.Id, Monday.AddHours(17), Monday.AddHours(18), isLocked: true);

        var plan = await ReplanAsync(client);

        var stayed = Assert.Single(plan.Stayed);
        Assert.Equal(lockedId, stayed.Id);
        Assert.True(stayed.IsLocked);
        Assert.Equal(Monday.AddHours(17), stayed.StartAtUtc);
        Assert.Empty(plan.Removed);
        Assert.Equal(Monday.AddHours(18), Assert.Single(plan.Created).StartAtUtc);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContextBase>();
        var locked = await db.StudySessions.SingleAsync(s => s.Id == lockedId);
        Assert.True(locked.IsLocked);
        Assert.Equal(Monday.AddHours(17), locked.StartAtUtc);
        Assert.Equal(Monday.AddHours(18), locked.EndAtUtc);
    }

    [Fact]
    public async Task Replan_ReportsMinutesThatDoNotFit()
    {
        var client = await _factory.CreateUserClientAsync();
        var task = await CreateTaskAsync(client, estimatedMinutes: 180, deadline: Monday.AddDays(1));
        await client.PostJsonAsync("/api/availability/windows", Window(DayOfWeek.Monday, 17, 18));

        var plan = await ReplanAsync(client);

        Assert.Equal(60, Assert.Single(plan.Created).PlannedMinutes);
        var left = Assert.Single(plan.Unscheduled);
        Assert.Equal(task.Id, left.StudyTaskId);
        Assert.Equal(180, left.RemainingMinutes);
        Assert.Equal(60, left.ScheduledMinutes);
    }

    [Fact]
    public async Task Replan_InvalidHorizon_ReturnsBadRequest()
    {
        var client = await _factory.CreateUserClientAsync();

        var response = await client.PostJsonAsync("/api/schedule/replan", new GenerateScheduleRequest(
            HorizonEnd, Monday, null, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<ReplanScheduleResponse> ReplanAsync(HttpClient client)
    {
        var response = await client.PostJsonAsync("/api/schedule/replan", new GenerateScheduleRequest(
            Monday, HorizonEnd, null, "UTC"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.ReadAsync<ReplanScheduleResponse>();
    }
}
