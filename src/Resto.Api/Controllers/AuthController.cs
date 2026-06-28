using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Resto.Application.Auth.CreateStaff;
using Resto.Application.Auth.DeactivateStaff;
using Resto.Application.Auth.GetCurrentUser;
using Resto.Application.Auth.ListStaff;
using Resto.Application.Auth.Login;
using Resto.Infrastructure.Identity;

namespace Resto.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new LoginCommand(request.Email, request.Password), cancellationToken);
        if (result is null)
            return Unauthorized(new { error = "Email o contraseña incorrectos." });

        return Ok(result);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var profile = await _sender.Send(new GetCurrentUserQuery(), cancellationToken);
        return profile is null ? NotFound() : Ok(profile);
    }

    [Authorize(Policy = AuthPolicies.StaffManagement)]
    [HttpGet("users")]
    public async Task<IActionResult> ListUsers(CancellationToken cancellationToken)
    {
        var users = await _sender.Send(new ListStaffUsersQuery(), cancellationToken);
        return Ok(users);
    }

    [Authorize(Policy = AuthPolicies.StaffManagement)]
    [HttpPost("users")]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateStaffUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new CreateStaffUserCommand(request.Email, request.Password, request.DisplayName, request.Role),
            cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(new { userId = result.Value });
    }

    [Authorize(Policy = AuthPolicies.StaffManagement)]
    [HttpPost("users/{userId:guid}/deactivate")]
    public async Task<IActionResult> DeactivateUser(Guid userId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeactivateStaffUserCommand(userId), cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok();
    }
}

public sealed record LoginRequest(string Email, string Password);

public sealed record CreateStaffUserRequest(string Email, string Password, string DisplayName, string Role);
