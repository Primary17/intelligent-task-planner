using TaskPlanner.Application.DTOs.Auth;
using TaskPlanner.Application.Validation;

namespace TaskPlanner.Tests.Validation;

public sealed class ProfileValidatorTests
{
    [Theory]
    [InlineData("Olena", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    public void DisplayName_MustNotBeBlank(string name, bool valid)
    {
        Assert.Equal(valid, new UpdateProfileRequestValidator().Validate(new UpdateProfileRequest(name)).IsValid);
    }

    [Fact]
    public void DisplayName_MaxLengthIs128()
    {
        var validator = new UpdateProfileRequestValidator();

        Assert.True(validator.Validate(new UpdateProfileRequest(new string('a', 128))).IsValid);
        Assert.False(validator.Validate(new UpdateProfileRequest(new string('a', 129))).IsValid);
    }

    [Theory]
    [InlineData("Password123!", "NewPassword1!", true)]
    [InlineData("Password123!", "short", false)]
    [InlineData("Password123!", "Password123!", false)]
    [InlineData("", "NewPassword1!", false)]
    public void ChangePassword_Rules(string current, string next, bool valid)
    {
        var result = new ChangePasswordRequestValidator().Validate(new ChangePasswordRequest(current, next));

        Assert.Equal(valid, result.IsValid);
    }
}
