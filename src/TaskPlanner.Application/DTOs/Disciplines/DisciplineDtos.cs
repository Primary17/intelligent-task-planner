namespace TaskPlanner.Application.DTOs.Disciplines;

public sealed record DisciplineDto(
    Guid Id,
    Guid UserId,
    string Name,
    string? Description,
    string Color,
    int TaskCount);

public sealed record CreateDisciplineRequest(
    string Name,
    string? Description,
    string Color = "#3B82F6");

public sealed record UpdateDisciplineRequest(
    string Name,
    string? Description,
    string Color);

public sealed record DisciplineQuery(
    Guid? UserId = null,
    string? Search = null,
    string Sort = "name");
