using TaskPlanner.Application.DTOs.StudySessions;
using TaskPlanner.Application.Validation;
using TaskPlanner.Domain.Enums;

namespace TaskPlanner.Tests.Validation;

public sealed class StudySessionValidatorTests
{
    private static readonly DateTime Start = new(2030, 1, 1, 10, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(4, false)]
    [InlineData(5, true)]
    [InlineData(120, true)]
    [InlineData(720, true)]
    [InlineData(721, false)]
    [InlineData(0, false)]
    [InlineData(-30, false)]
    public void Create_LengthMustBeBetweenFiveMinutesAndTwelveHours(int minutes, bool valid)
    {
        var request = new CreateStudySessionRequest(Guid.NewGuid(), Start, Start.AddMinutes(minutes));

        Assert.Equal(valid, new CreateStudySessionRequestValidator().Validate(request).IsValid);
    }

    [Fact]
    public void Create_RequiresTask()
    {
        var request = new CreateStudySessionRequest(Guid.Empty, Start, Start.AddHours(1));

        Assert.False(new CreateStudySessionRequestValidator().Validate(request).IsValid);
    }

    [Theory]
    [InlineData(SessionStatus.Planned, null, true)]
    [InlineData(SessionStatus.Planned, 30, false)]
    [InlineData(SessionStatus.Missed, 30, false)]
    [InlineData(SessionStatus.Completed, null, true)]
    [InlineData(SessionStatus.Completed, 0, true)]
    [InlineData(SessionStatus.Completed, 90, true)]
    [InlineData(SessionStatus.Completed, -1, false)]
    [InlineData(SessionStatus.Completed, 1441, false)]
    public void Update_ActualMinutesOnlyForCompletedAndInRange(SessionStatus status, int? actual, bool valid)
    {
        var request = new UpdateStudySessionRequest(Start, Start.AddHours(2), status, actual, false);

        Assert.Equal(valid, new UpdateStudySessionRequestValidator().Validate(request).IsValid);
    }
}
