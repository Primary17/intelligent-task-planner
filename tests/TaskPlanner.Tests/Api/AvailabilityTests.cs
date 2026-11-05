using System.Net;
using TaskPlanner.Application.DTOs.Availability;
using TaskPlanner.Domain.Enums;

namespace TaskPlanner.Tests.Api;

[Collection(ApiCollection.Name)]
public sealed class AvailabilityTests
{
    private readonly ApiFactory _factory;

    public AvailabilityTests(ApiFactory factory) => _factory = factory;

    private static AvailabilityWindowRequest Window(DayOfWeek day, int fromHour, int toHour) =>
        new(day, new TimeOnly(fromHour, 0), new TimeOnly(toHour, 0));

    [Fact]
    public async Task Windows_CrudAndOrdering()
    {
        var client = await _factory.CreateUserClientAsync();

        await client.PostJsonAsync("/api/availability/windows", Window(DayOfWeek.Sunday, 10, 12));
        var monday = await (await client.PostJsonAsync("/api/availability/windows", Window(DayOfWeek.Monday, 17, 20)))
            .ReadAsync<AvailabilityWindowDto>();

        var list = await (await client.GetAsync("/api/availability/windows")).ReadAsync<List<AvailabilityWindowDto>>();
        Assert.Equal(DayOfWeek.Monday, list[0].DayOfWeek);
        Assert.Equal(DayOfWeek.Sunday, list[1].DayOfWeek);

        var updated = await client.PutJsonAsync($"/api/availability/windows/{monday.Id}", Window(DayOfWeek.Monday, 16, 19));
        Assert.Equal(new TimeOnly(16, 0), (await updated.ReadAsync<AvailabilityWindowDto>()).StartTime);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/availability/windows/{monday.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/availability/windows/{monday.Id}")).StatusCode);
    }

    [Fact]
    public async Task Window_EndBeforeStart_ReturnsBadRequest()
    {
        var client = await _factory.CreateUserClientAsync();

        var response = await client.PostJsonAsync("/api/availability/windows", Window(DayOfWeek.Monday, 20, 17));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Window_Overlap_ReturnsBadRequest_ButAdjacentIsAllowed()
    {
        var client = await _factory.CreateUserClientAsync();
        await client.PostJsonAsync("/api/availability/windows", Window(DayOfWeek.Tuesday, 10, 12));

        var overlapping = await client.PostJsonAsync("/api/availability/windows", Window(DayOfWeek.Tuesday, 11, 13));
        var adjacent = await client.PostJsonAsync("/api/availability/windows", Window(DayOfWeek.Tuesday, 12, 14));

        Assert.Equal(HttpStatusCode.BadRequest, overlapping.StatusCode);
        Assert.Equal(HttpStatusCode.Created, adjacent.StatusCode);
    }

    [Fact]
    public async Task Window_UpdateDoesNotConflictWithItself()
    {
        var client = await _factory.CreateUserClientAsync();
        var window = await (await client.PostJsonAsync("/api/availability/windows", Window(DayOfWeek.Friday, 10, 12)))
            .ReadAsync<AvailabilityWindowDto>();

        var response = await client.PutJsonAsync($"/api/availability/windows/{window.Id}", Window(DayOfWeek.Friday, 10, 13));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Window_OfAnotherUser_IsForbidden()
    {
        var owner = await _factory.CreateUserClientAsync();
        var stranger = await _factory.CreateUserClientAsync();
        var window = await (await owner.PostJsonAsync("/api/availability/windows", Window(DayOfWeek.Monday, 9, 10)))
            .ReadAsync<AvailabilityWindowDto>();

        var response = await stranger.DeleteAsync($"/api/availability/windows/{window.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Empty(await (await stranger.GetAsync("/api/availability/windows")).ReadAsync<List<AvailabilityWindowDto>>());
    }

    [Fact]
    public async Task Exception_CustomHoursRequiresTimes()
    {
        var client = await _factory.CreateUserClientAsync();

        var response = await client.PostJsonAsync("/api/availability/exceptions", new AvailabilityExceptionRequest(
            new DateOnly(2030, 1, 7), AvailabilityExceptionKind.CustomHours, null, null, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Exception_UnavailableMustNotHaveTimes()
    {
        var client = await _factory.CreateUserClientAsync();

        var response = await client.PostJsonAsync("/api/availability/exceptions", new AvailabilityExceptionRequest(
            new DateOnly(2030, 1, 7), AvailabilityExceptionKind.Unavailable, new TimeOnly(9, 0), new TimeOnly(10, 0), null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Exception_OnePerDate()
    {
        var client = await _factory.CreateUserClientAsync();
        var request = new AvailabilityExceptionRequest(new DateOnly(2030, 1, 7), AvailabilityExceptionKind.Unavailable, null, null, "Exam");

        var first = await client.PostJsonAsync("/api/availability/exceptions", request);
        var second = await client.PostJsonAsync("/api/availability/exceptions", request);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task Exception_UpdateDeleteAndRangeFilter()
    {
        var client = await _factory.CreateUserClientAsync();
        var created = await (await client.PostJsonAsync("/api/availability/exceptions", new AvailabilityExceptionRequest(
            new DateOnly(2030, 2, 1), AvailabilityExceptionKind.Unavailable, null, null, null))).ReadAsync<AvailabilityExceptionDto>();
        await client.PostJsonAsync("/api/availability/exceptions", new AvailabilityExceptionRequest(
            new DateOnly(2030, 3, 1), AvailabilityExceptionKind.Unavailable, null, null, null));

        var february = await (await client.GetAsync("/api/availability/exceptions?from=2030-02-01&to=2030-02-28"))
            .ReadAsync<List<AvailabilityExceptionDto>>();
        Assert.Single(february);

        var updated = await client.PutJsonAsync($"/api/availability/exceptions/{created.Id}", new AvailabilityExceptionRequest(
            new DateOnly(2030, 2, 1), AvailabilityExceptionKind.CustomHours, new TimeOnly(9, 0), new TimeOnly(11, 0), "Short day"));
        Assert.Equal(AvailabilityExceptionKind.CustomHours, (await updated.ReadAsync<AvailabilityExceptionDto>()).Kind);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/availability/exceptions/{created.Id}")).StatusCode);
    }

    [Fact]
    public async Task Effective_AppliesExceptionsOverWeeklySchedule()
    {
        var client = await _factory.CreateUserClientAsync();
        var monday = new DateOnly(2030, 1, 7);
        Assert.Equal(DayOfWeek.Monday, monday.DayOfWeek);

        await client.PostJsonAsync("/api/availability/windows", Window(DayOfWeek.Monday, 17, 20));
        await client.PostJsonAsync("/api/availability/windows", Window(DayOfWeek.Tuesday, 18, 22));
        await client.PostJsonAsync("/api/availability/exceptions", new AvailabilityExceptionRequest(
            monday.AddDays(7), AvailabilityExceptionKind.Unavailable, null, null, null));
        await client.PostJsonAsync("/api/availability/exceptions", new AvailabilityExceptionRequest(
            monday.AddDays(1), AvailabilityExceptionKind.CustomHours, new TimeOnly(9, 0), new TimeOnly(12, 0), null));

        var days = await (await client.GetAsync("/api/availability/effective?from=2030-01-07&to=2030-01-14"))
            .ReadAsync<List<EffectiveAvailabilityDayDto>>();

        Assert.Equal(8, days.Count);
        Assert.Equal("Weekly", days[0].Source);
        Assert.Equal(new TimeOnly(17, 0), Assert.Single(days[0].Intervals).StartTime);
        Assert.Equal("CustomHours", days[1].Source);
        Assert.Equal(new TimeOnly(9, 0), Assert.Single(days[1].Intervals).StartTime);
        Assert.Empty(days[2].Intervals);
        Assert.Equal("Unavailable", days[7].Source);
        Assert.Empty(days[7].Intervals);
    }

    [Fact]
    public async Task Effective_InvalidRange_ReturnsBadRequest()
    {
        var client = await _factory.CreateUserClientAsync();

        var reversed = await client.GetAsync("/api/availability/effective?from=2030-02-01&to=2030-01-01");
        var tooLong = await client.GetAsync("/api/availability/effective?from=2030-01-01&to=2031-01-01");

        Assert.Equal(HttpStatusCode.BadRequest, reversed.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, tooLong.StatusCode);
    }
}
