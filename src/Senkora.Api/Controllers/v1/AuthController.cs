using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Senkora.Application.Common.Models;
using Senkora.Application.Features.Auth.Commands;
using Senkora.Application.Features.Auth.Queries;

namespace Senkora.Api.Controllers.v1;

/// <summary>Kimlik dogrulama islemleri</summary>
[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public sealed class AuthController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Kullanici girisi yapar ve JWT token dondurur.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var command = new LoginCommand(request.Email, request.Password, request.TotpCode, ip);
        var result = await mediator.Send(command, ct);

        if (!result.IsSuccess)
        {
            var statusCode = result.ErrorCode switch
            {
                "ACCOUNT_LOCKED"    => StatusCodes.Status423Locked,
                "MFA_REQUIRED"      => StatusCodes.Status200OK,
                _                   => StatusCodes.Status401Unauthorized
            };
            return StatusCode(statusCode, ApiResponse<object>.Fail(result.Error!));
        }

        return Ok(ApiResponse<LoginResult>.Ok(result.Data!));
    }

    /// <summary>
    /// Refresh token ile yeni access token alir.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var command = new RefreshTokenCommand(request.RefreshToken, ip);
        var result = await mediator.Send(command, ct);

        if (!result.IsSuccess)
            return Unauthorized(ApiResponse<object>.Fail(result.Error!));

        return Ok(ApiResponse<LoginResult>.Ok(result.Data!));
    }

    /// <summary>
    /// Cikis yapar (refresh token iptal edilir).
    /// </summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken ct)
    {
        await mediator.Send(new LogoutCommand(request.RefreshToken), ct);
        return Ok(ApiResponse.Ok("Cikis basarili."));
    }

    /// <summary>
    /// Mevcut giris yapan kullanicinin bilgilerini dondurur.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<CurrentUserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var result = await mediator.Send(new GetCurrentUserQuery(), ct);

        if (!result.IsSuccess)
            return Unauthorized(ApiResponse<object>.Fail(result.Error!));

        return Ok(ApiResponse<CurrentUserDto>.Ok(result.Data!));
    }
}

// ── Request DTO'lari ─────────────────────────────────────────────────────────

/// <summary>Login istegi</summary>
public sealed record LoginRequest(
    string Email,
    string Password,
    string? TotpCode = null);

/// <summary>Token yenileme istegi</summary>
public sealed record RefreshRequest(string RefreshToken);

/// <summary>Cikis istegi</summary>
public sealed record LogoutRequest(string RefreshToken);
