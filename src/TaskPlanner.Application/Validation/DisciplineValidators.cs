using FluentValidation;
using TaskPlanner.Application.DTOs.Disciplines;

namespace TaskPlanner.Application.Validation;

public sealed class CreateDisciplineRequestValidator : AbstractValidator<CreateDisciplineRequest>
{
    public CreateDisciplineRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
        RuleFor(x => x.Color).NotEmpty().MaximumLength(16)
            .Matches("^#[0-9A-Fa-f]{6}$").WithMessage("Color must be a hex code like #3B82F6.");
    }
}

public sealed class UpdateDisciplineRequestValidator : AbstractValidator<UpdateDisciplineRequest>
{
    public UpdateDisciplineRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
        RuleFor(x => x.Color).NotEmpty().MaximumLength(16)
            .Matches("^#[0-9A-Fa-f]{6}$").WithMessage("Color must be a hex code like #3B82F6.");
    }
}
