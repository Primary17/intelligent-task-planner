using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPlanner.Application.DTOs.Auth;
using TaskPlanner.Application.Security;
using TaskPlanner.Application.Services;

namespace TaskPlanner.Api.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = AuthPolicies.AdminOnly)]
[Produces("application/json")]
public sealed class AdminUsersController : ControllerBase
{
    private readonly IAuthService _auth;

    public AdminUsersController(IAuthService auth)
    {
        _auth = auth;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AdminUserListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<AdminUserListItemDto>>> GetAll(CancellationToken cancellationToken)
    {
        var users = await _auth.ListUsersForAdminAsync(cancellationToken);
        return Ok(users);
    }
}
