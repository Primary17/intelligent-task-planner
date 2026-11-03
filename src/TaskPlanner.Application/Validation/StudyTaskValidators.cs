using FluentValidation;
using TaskPlanner.Application.DTOs.StudyTasks;

namespace TaskPlanner.Application.Validation;

public sealed class CreateStudyTaskRequestValidator : AbstractValidator<CreateStudyTaskRequest>
{
    public CreateStudyTaskRequestValidator()
    {
        RuleFor(x => x.DisciplineId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.DeadlineUtc).Must(d => d > DateTime.UtcNow.AddMinutes(-1))
            .WithMessage("Deadline must be in the future or present (UTC).");
        RuleFor(x => x.Complexity).InclusiveBetween(1, 5);
        RuleFor(x => x.EstimatedMinutes).GreaterThan(0);
        RuleFor(x => x.Priority).IsInEnum();
        RuleFor(x => x.Status).IsInEnum();
    }
}

public sealed class UpdateStudyTaskRequestValidator : AbstractValidator<UpdateStudyTaskRequest>
{
    public UpdateStudyTaskRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.DeadlineUtc).Must(d => d > DateTime.UtcNow.AddMinutes(-1))
            .WithMessage("Deadline must be in the future or present (UTC).");
        RuleFor(x => x.Complexity).InclusiveBetween(1, 5);
        RuleFor(x => x.EstimatedMinutes).GreaterThan(0);
        RuleFor(x => x.ActualMinutes).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Priority).IsInEnum();
        RuleFor(x => x.Status).IsInEnum();
    }
}
