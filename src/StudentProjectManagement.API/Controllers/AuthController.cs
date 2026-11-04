using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentProjectManagement.Application.Auth.DTOs;
using StudentProjectManagement.Application.Auth.Interfaces;
using StudentProjectManagement.Application.Common.Interfaces;

namespace StudentProjectManagement.API.Controllers;

public sealed class AuthController : BaseApiController
{
    private readonly IAuthService _authService;

    public AuthController(
        ICurrentUserService currentUserService,
        IAuthService authService) : base(currentUserService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request, cancellationToken);
        return OkResponse(result, "Login successful.");
    }

    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterAsync(request, cancellationToken);
        return CreatedResponse(result, "Student account registered successfully.");
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.RefreshAsync(request.RefreshToken, cancellationToken);
        return OkResponse(result, "Tokens refreshed successfully.");
    }

    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (CurrentUser.UserId is not Guid userId)
        {
            return UnauthorizedResponse("The access token does not contain a valid user ID.");
        }

        await _authService.RevokeRefreshTokenAsync(userId, cancellationToken);
        return OkResponse<object?>(null, "Refresh token revoked successfully.");
    }
}
