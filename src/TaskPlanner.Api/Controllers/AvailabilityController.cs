using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPlanner.Application.DTOs.Availability;
using TaskPlanner.Application.Security;
using TaskPlanner.Application.Services;

namespace TaskPlanner.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthPolicies.AuthenticatedUser)]
[Route("api/availability")]
[Produces("application/json")]
public sealed class AvailabilityController : ControllerBase
{
    private readonly IAvailabilityService _availability;

    public AvailabilityController(IAvailabilityService availability)
    {
        _availability = availability;
    }

    [HttpGet("windows")]
    public async Task<ActionResult<IReadOnlyList<AvailabilityWindowDto>>> GetWindows(CancellationToken cancellationToken) =>
        Ok(await _availability.GetWindowsAsync(cancellationToken));

    [HttpPost("windows")]
    [ProducesResponseType(typeof(AvailabilityWindowDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AvailabilityWindowDto>> CreateWindow(
        [FromBody] AvailabilityWindowRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _availability.CreateWindowAsync(request, cancellationToken);
        return Created($"/api/availability/windows/{created.Id}", created);
    }

    [HttpPut("windows/{id:guid}")]
    [ProducesResponseType(typeof(AvailabilityWindowDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AvailabilityWindowDto>> UpdateWindow(
        Guid id,
        [FromBody] AvailabilityWindowRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _availability.UpdateWindowAsync(id, request, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("windows/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteWindow(Guid id, CancellationToken cancellationToken) =>
        await _availability.DeleteWindowAsync(id, cancellationToken) ? NoContent() : NotFound();

    [HttpGet("exceptions")]
    public async Task<ActionResult<IReadOnlyList<AvailabilityExceptionDto>>> GetExceptions(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken) =>
        Ok(await _availability.GetExceptionsAsync(from, to, cancellationToken));

    [HttpPost("exceptions")]
    [ProducesResponseType(typeof(AvailabilityExceptionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AvailabilityExceptionDto>> CreateException(
        [FromBody] AvailabilityExceptionRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _availability.CreateExceptionAsync(request, cancellationToken);
        return Created($"/api/availability/exceptions/{created.Id}", created);
    }

    [HttpPut("exceptions/{id:guid}")]
    [ProducesResponseType(typeof(AvailabilityExceptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AvailabilityExceptionDto>> UpdateException(
        Guid id,
        [FromBody] AvailabilityExceptionRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _availability.UpdateExceptionAsync(id, request, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("exceptions/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteException(Guid id, CancellationToken cancellationToken) =>
        await _availability.DeleteExceptionAsync(id, cancellationToken) ? NoContent() : NotFound();

    [HttpGet("effective")]
    public async Task<ActionResult<IReadOnlyList<EffectiveAvailabilityDayDto>>> GetEffective(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken) =>
        Ok(await _availability.GetEffectiveAsync(from, to, cancellationToken));
}
