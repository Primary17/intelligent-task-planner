using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPlanner.Application.DTOs.Planning;
using TaskPlanner.Application.Security;
using TaskPlanner.Application.Services;

namespace TaskPlanner.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthPolicies.AuthenticatedUser)]
[Route("api/planning")]
[Produces("application/json")]
public sealed class PlanningController : ControllerBase
{
    private readonly IPlanningService _planning;

    public PlanningController(IPlanningService planning)
    {
        _planning = planning;
    }

    [HttpGet("capacity")]
    [ProducesResponseType(typeof(CapacityReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CapacityReportDto>> Capacity(
        [FromQuery] int utcOffsetMinutes,
        CancellationToken cancellationToken)
    {
        return Ok(await _planning.GetCapacityAsync(utcOffsetMinutes, cancellationToken));
    }
}
