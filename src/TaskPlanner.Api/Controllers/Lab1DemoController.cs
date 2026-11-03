using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPlanner.Application.Security;
using TaskPlanner.Infrastructure.Persistence;
using TaskPlanner.Infrastructure.Services;

namespace TaskPlanner.Api.Controllers;

[ApiController]
[Authorize(Roles = AuthRoles.Admin)]
[Route("api/lab1")]
public sealed class Lab1DemoController : ControllerBase
{
    private readonly Lab1ScenarioService _scenario;
    private readonly AppDbContextBase _db;
    private readonly IConfiguration _configuration;

    public Lab1DemoController(Lab1ScenarioService scenario, AppDbContextBase db, IConfiguration configuration)
    {
        _scenario = scenario;
        _db = db;
        _configuration = configuration;
    }

    [HttpGet("provider")]
    public ActionResult<object> Provider() => Ok(new
    {
        configured = _configuration["Database:Provider"],
        ef = _db.Database.ProviderName,
        canConnect = _db.Database.CanConnect()
    });

    [HttpPost("scenario")]
    public async Task<ActionResult<Lab1DemoReport>> RunScenario(CancellationToken cancellationToken)
    {
        var report = await _scenario.RunAsync(cancellationToken);
        return Ok(report);
    }
}
