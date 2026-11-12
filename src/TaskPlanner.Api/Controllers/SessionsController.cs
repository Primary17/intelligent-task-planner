using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPlanner.Application.DTOs.StudySessions;
using TaskPlanner.Application.Security;
using TaskPlanner.Application.Services;

namespace TaskPlanner.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthPolicies.AuthenticatedUser)]
[Route("api/sessions")]
[Produces("application/json")]
public sealed class SessionsController : ControllerBase
{
    private readonly IStudySessionService _sessions;

    public SessionsController(IStudySessionService sessions)
    {
        _sessions = sessions;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<StudySessionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<StudySessionDto>>> GetAll(
        [FromQuery] StudySessionQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await _sessions.GetAllAsync(query, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(StudySessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StudySessionDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await _sessions.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [ProducesResponseType(typeof(StudySessionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StudySessionDto>> Create(
        [FromBody] CreateStudySessionRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _sessions.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(StudySessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StudySessionDto>> Update(
        Guid id,
        [FromBody] UpdateStudySessionRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _sessions.UpdateAsync(id, request, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _sessions.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
