using TaskPlanner.Application.DTOs.Availability;
using TaskPlanner.Application.DTOs.Disciplines;
using TaskPlanner.Application.DTOs.StudyTasks;
using TaskPlanner.Application.Validation;
using TaskPlanner.Domain.Enums;

namespace TaskPlanner.Tests.Validation;

public sealed class ValidatorTests
{
    [Theory]
    [InlineData("#3B82F6", true)]
    [InlineData("#abcdef", true)]
    [InlineData("3B82F6", false)]
    [InlineData("#FFF", false)]
    [InlineData("blue", false)]
    public void DisciplineColor_MustBeSixDigitHex(string color, bool valid)
    {
        var result = new CreateDisciplineRequestValidator().Validate(new CreateDisciplineRequest("Math", null, color));

        Assert.Equal(valid, result.IsValid);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(5, true)]
    [InlineData(0, false)]
    [InlineData(6, false)]
    public void TaskComplexity_MustBeBetweenOneAndFive(int complexity, bool valid)
    {
        var request = new CreateStudyTaskRequest(Guid.NewGuid(), "Lab", DateTime.UtcNow.AddDays(1), complexity, TaskPriority.Medium, 60);

        Assert.Equal(valid, new CreateStudyTaskRequestValidator().Validate(request).IsValid);
    }

    [Fact]
    public void TaskEstimate_MustBePositive()
    {
        var request = new CreateStudyTaskRequest(Guid.NewGuid(), "Lab", DateTime.UtcNow.AddDays(1), 3, TaskPriority.Medium, 0);

        Assert.False(new CreateStudyTaskRequestValidator().Validate(request).IsValid);
    }

    [Fact]
    public void UpdateTask_AllowsPastDeadline()
    {
        var request = new UpdateStudyTaskRequest("Lab", DateTime.UtcNow.AddDays(-2), 3, TaskPriority.Medium, 60, 60, StudyTaskStatus.Completed);

        Assert.True(new UpdateStudyTaskRequestValidator().Validate(request).IsValid);
    }

    [Theory]
    [InlineData(17, 20, true)]
    [InlineData(20, 17, false)]
    [InlineData(17, 17, false)]
    public void Window_EndMustBeAfterStart(int from, int to, bool valid)
    {
        var request = new AvailabilityWindowRequest(DayOfWeek.Monday, new TimeOnly(from, 0), new TimeOnly(to, 0));

        Assert.Equal(valid, new AvailabilityWindowRequestValidator().Validate(request).IsValid);
    }

    [Fact]
    public void Exception_UnavailableWithoutTimes_IsValid()
    {
        var request = new AvailabilityExceptionRequest(new DateOnly(2030, 1, 1), AvailabilityExceptionKind.Unavailable, null, null, "Exam");

        Assert.True(new AvailabilityExceptionRequestValidator().Validate(request).IsValid);
    }

    [Fact]
    public void Exception_CustomHoursNeedsOrderedTimes()
    {
        var validator = new AvailabilityExceptionRequestValidator();
        var date = new DateOnly(2030, 1, 1);

        Assert.True(validator.Validate(new AvailabilityExceptionRequest(date, AvailabilityExceptionKind.CustomHours, new TimeOnly(9, 0), new TimeOnly(10, 0), null)).IsValid);
        Assert.False(validator.Validate(new AvailabilityExceptionRequest(date, AvailabilityExceptionKind.CustomHours, new TimeOnly(10, 0), new TimeOnly(9, 0), null)).IsValid);
        Assert.False(validator.Validate(new AvailabilityExceptionRequest(date, AvailabilityExceptionKind.CustomHours, null, null, null)).IsValid);
    }

    [Fact]
    public void Exception_ReasonIsLimitedTo500Characters()
    {
        var request = new AvailabilityExceptionRequest(new DateOnly(2030, 1, 1), AvailabilityExceptionKind.Unavailable, null, null, new string('x', 501));

        Assert.False(new AvailabilityExceptionRequestValidator().Validate(request).IsValid);
    }
}
