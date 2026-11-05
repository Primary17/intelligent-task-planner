using System.Net;
using TaskPlanner.Application.DTOs.Disciplines;

namespace TaskPlanner.Tests.Api;

[Collection(ApiCollection.Name)]
public sealed class DisciplineTests
{
    private readonly ApiFactory _factory;

    public DisciplineTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Crud_FullCycle()
    {
        var client = await _factory.CreateUserClientAsync();

        var created = await client.PostJsonAsync("/api/disciplines",
            new CreateDisciplineRequest("Операційні системи", "Лекції", "#112233"));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var discipline = await created.ReadAsync<DisciplineDto>();

        var updated = await client.PutJsonAsync($"/api/disciplines/{discipline.Id}",
            new UpdateDisciplineRequest("ОС", null, "#445566"));
        Assert.Equal("ОС", (await updated.ReadAsync<DisciplineDto>()).Name);

        var list = await (await client.GetAsync("/api/disciplines")).ReadAsync<List<DisciplineDto>>();
        Assert.Single(list);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/disciplines/{discipline.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/disciplines/{discipline.Id}")).StatusCode);
    }

    [Fact]
    public async Task Create_DuplicateName_ReturnsBadRequest()
    {
        var client = await _factory.CreateUserClientAsync();
        var request = new CreateDisciplineRequest("Math", null, "#112233");

        await client.PostJsonAsync("/api/disciplines", request);
        var second = await client.PostJsonAsync("/api/disciplines", request);

        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task Create_InvalidColor_ReturnsBadRequest()
    {
        var client = await _factory.CreateUserClientAsync();

        var response = await client.PostJsonAsync("/api/disciplines", new CreateDisciplineRequest("Math", null, "red"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task OtherUsersDiscipline_IsForbiddenAndHiddenFromList()
    {
        var owner = await _factory.CreateUserClientAsync();
        var stranger = await _factory.CreateUserClientAsync();
        var created = await (await owner.PostJsonAsync("/api/disciplines",
            new CreateDisciplineRequest("Private", null, "#112233"))).ReadAsync<DisciplineDto>();

        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.GetAsync($"/api/disciplines/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.DeleteAsync($"/api/disciplines/{created.Id}")).StatusCode);
        Assert.Empty(await (await stranger.GetAsync("/api/disciplines")).ReadAsync<List<DisciplineDto>>());
    }

    [Fact]
    public async Task Requests_WithoutToken_ReturnUnauthorized()
    {
        var response = await _factory.CreateClient().GetAsync("/api/disciplines");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
