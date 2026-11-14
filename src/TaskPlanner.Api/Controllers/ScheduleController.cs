using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPlanner.Application.DTOs.Scheduling;
using TaskPlanner.Application.Security;
using TaskPlanner.Application.Services;

namespace TaskPlanner.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthPolicies.AuthenticatedUser)]
[Route("api/schedule")]
[Produces("application/json")]
public sealed class ScheduleController : ControllerBase
{
    private readonly IScheduleService _schedule;

    public ScheduleController(IScheduleService schedule)
    {
        _schedule = schedule;
    }

    [HttpPost("generate")]
    [ProducesResponseType(typeof(GenerateScheduleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GenerateScheduleResponse>> Generate(
        [FromBody] GenerateScheduleRequest request,
        CancellationToken cancellationToken)
    {
        var plan = await _schedule.GenerateAsync(request, cancellationToken);
        return Ok(plan);
    }

    [HttpPost("replan")]
    [ProducesResponseType(typeof(ReplanScheduleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReplanScheduleResponse>> Replan(
        [FromBody] GenerateScheduleRequest request,
        CancellationToken cancellationToken)
    {
        var plan = await _schedule.ReplanAsync(request, cancellationToken);
        return Ok(plan);
    }
}
