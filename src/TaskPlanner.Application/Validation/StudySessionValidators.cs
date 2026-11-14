using FluentValidation;
using TaskPlanner.Application.DTOs.StudySessions;
using TaskPlanner.Domain.Enums;

namespace TaskPlanner.Application.Validation;

public static class StudySessionLimits
{
    public const int MinMinutes = 5;
    public const int MaxMinutes = 12 * 60;
}

public sealed class CreateStudySessionRequestValidator : AbstractValidator<CreateStudySessionRequest>
{
    public CreateStudySessionRequestValidator()
    {
        RuleFor(x => x.StudyTaskId).NotEmpty();
        RuleFor(x => x).Must(x => x.EndAtUtc > x.StartAtUtc)
            .WithMessage("Session end must be after its start.")
            .WithName("EndAtUtc");
        RuleFor(x => x).Must(x => IsLengthValid(x.StartAtUtc, x.EndAtUtc))
            .WithMessage($"Session must last between {StudySessionLimits.MinMinutes} minutes and 12 hours.")
            .WithName("EndAtUtc")
            .When(x => x.EndAtUtc > x.StartAtUtc);
    }

    internal static bool IsLengthValid(DateTime start, DateTime end)
    {
        var minutes = (end - start).TotalMinutes;
        return minutes >= StudySessionLimits.MinMinutes && minutes <= StudySessionLimits.MaxMinutes;
    }
}

public sealed class UpdateStudySessionRequestValidator : AbstractValidator<UpdateStudySessionRequest>
{
    public UpdateStudySessionRequestValidator()
    {
        RuleFor(x => x).Must(x => x.EndAtUtc > x.StartAtUtc)
            .WithMessage("Session end must be after its start.")
            .WithName("EndAtUtc");
        RuleFor(x => x).Must(x => CreateStudySessionRequestValidator.IsLengthValid(x.StartAtUtc, x.EndAtUtc))
            .WithMessage($"Session must last between {StudySessionLimits.MinMinutes} minutes and 12 hours.")
            .WithName("EndAtUtc")
            .When(x => x.EndAtUtc > x.StartAtUtc);
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.ActualMinutes).InclusiveBetween(0, 24 * 60).When(x => x.ActualMinutes.HasValue);
        RuleFor(x => x.ActualMinutes).Null()
            .When(x => x.Status != SessionStatus.Completed)
            .WithMessage("Actual minutes can only be set for a completed session.");
    }
}
