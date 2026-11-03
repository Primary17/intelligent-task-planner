using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPlanner.Application.DTOs.Disciplines;
using TaskPlanner.Application.Security;
using TaskPlanner.Application.Services;

namespace TaskPlanner.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthPolicies.AuthenticatedUser)]
[Route("api/disciplines")]
[Produces("application/json")]
public sealed class DisciplinesController : ControllerBase
{
    private readonly IDisciplineService _disciplines;

    public DisciplinesController(IDisciplineService disciplines)
    {
        _disciplines = disciplines;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DisciplineDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DisciplineDto>>> GetAll(
        [FromQuery] DisciplineQuery query,
        CancellationToken cancellationToken)
    {
        var items = await _disciplines.GetAllAsync(query, cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DisciplineDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DisciplineDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await _disciplines.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [ProducesResponseType(typeof(DisciplineDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DisciplineDto>> Create(
        [FromBody] CreateDisciplineRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _disciplines.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(DisciplineDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DisciplineDto>> Update(
        Guid id,
        [FromBody] UpdateDisciplineRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _disciplines.UpdateAsync(id, request, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _disciplines.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
