using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPlanner.Application.DTOs.Auth;
using TaskPlanner.Application.Security;
using TaskPlanner.Application.Services;

namespace TaskPlanner.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly ICurrentUserContext _currentUser;

    public AuthController(IAuthService auth, ICurrentUserContext currentUser)
    {
        _auth = auth;
        _currentUser = currentUser;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _auth.RegisterAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Me), response);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _auth.LoginAsync(request, cancellationToken);
        if (response is null)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Invalid credentials",
                Detail = "Email or password is incorrect.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        return Ok(response);
    }

    [HttpGet("me")]
    [Authorize(Policy = AuthPolicies.AuthenticatedUser)]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileDto>> Me(CancellationToken cancellationToken)
    {
        var profile = await _auth.GetProfileAsync(_currentUser.UserId, cancellationToken);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpPut("me")]
    [Authorize(Policy = AuthPolicies.AuthenticatedUser)]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserProfileDto>> UpdateMe(
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await _auth.UpdateProfileAsync(_currentUser.UserId, request, cancellationToken);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpPost("change-password")]
    [Authorize(Policy = AuthPolicies.AuthenticatedUser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var changed = await _auth.ChangePasswordAsync(_currentUser.UserId, request, cancellationToken);
        return changed ? NoContent() : NotFound();
    }
}
