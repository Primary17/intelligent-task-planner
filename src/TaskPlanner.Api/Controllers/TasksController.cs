using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPlanner.Application.DTOs.StudyTasks;
using TaskPlanner.Application.Security;
using TaskPlanner.Application.Services;

namespace TaskPlanner.Api.Controllers;


[ApiController]
[Authorize(Policy = AuthPolicies.AuthenticatedUser)]
[Route("api/tasks")]
[Produces("application/json")]
public sealed class TasksController : ControllerBase
{
    private readonly IStudyTaskService _studyTasks;

    public TasksController(IStudyTaskService studyTasks)
    {
        _studyTasks = studyTasks;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<StudyTaskDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<StudyTaskDto>>> GetAll(
        [FromQuery] StudyTaskQuery query,
        CancellationToken cancellationToken)
    {
        var items = await _studyTasks.GetAllAsync(query, cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(StudyTaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StudyTaskDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await _studyTasks.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [ProducesResponseType(typeof(StudyTaskDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StudyTaskDto>> Create(
        [FromBody] CreateStudyTaskRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _studyTasks.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(StudyTaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StudyTaskDto>> Update(
        Guid id,
        [FromBody] UpdateStudyTaskRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _studyTasks.UpdateAsync(id, request, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _studyTasks.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
