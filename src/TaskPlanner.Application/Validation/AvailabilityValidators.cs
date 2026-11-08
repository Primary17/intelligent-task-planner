using FluentValidation;
using TaskPlanner.Application.DTOs.Availability;
using TaskPlanner.Domain.Enums;

namespace TaskPlanner.Application.Validation;

public sealed class AvailabilityWindowRequestValidator : AbstractValidator<AvailabilityWindowRequest>
{
    public AvailabilityWindowRequestValidator()
    {
        RuleFor(x => x.DayOfWeek).IsInEnum();
        RuleFor(x => x.EndTime).Must((x, end) => end > x.StartTime)
            .WithMessage("End time must be later than start time (windows cannot cross midnight).");
    }
}

public sealed class AvailabilityExceptionRequestValidator : AbstractValidator<AvailabilityExceptionRequest>
{
    public AvailabilityExceptionRequestValidator()
    {
        RuleFor(x => x.Kind).IsInEnum();
        RuleFor(x => x.Reason).MaximumLength(500).When(x => x.Reason is not null);

        When(x => x.Kind == AvailabilityExceptionKind.CustomHours, () =>
        {
            RuleFor(x => x.StartTime).NotNull().WithMessage("Start time is required for custom hours.");
            RuleFor(x => x.EndTime).NotNull().WithMessage("End time is required for custom hours.");
            RuleFor(x => x.EndTime).Must((x, end) => end is null || x.StartTime is null || end > x.StartTime)
                .WithMessage("End time must be later than start time.");
        });

        When(x => x.Kind == AvailabilityExceptionKind.Unavailable, () =>
        {
            RuleFor(x => x.StartTime).Null().WithMessage("Start time must be empty when the day is unavailable.");
            RuleFor(x => x.EndTime).Null().WithMessage("End time must be empty when the day is unavailable.");
        });
    }
}
