using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Senkora.Application.Common.Interfaces;
using Senkora.Application.Common.Models;
using Senkora.Application.Features.Licensing.Commands;
using Senkora.Application.Features.Licensing.Queries;

namespace Senkora.Api.Controllers.v1;

/// <summary>Lisans yonetimi</summary>
[ApiController]
[Route("api/v1/license")]
[Authorize]
[Produces("application/json")]
public sealed class LicenseController(
    IMediator mediator,
    ICurrentUser currentUser) : ControllerBase
{
    /// <summary>Mevcut tenant lisans durumunu dondurur</summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(ApiResponse<LicenseStatusDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        var tenantId = HttpContext.Items["TenantId"] as Guid? ?? currentUser.TenantId;
        var result   = await mediator.Send(new GetLicenseStatusQuery(tenantId), ct);
        return Ok(ApiResponse<LicenseStatusDto>.Ok(result.Data!));
    }

    /// <summary>Belirli bir tenant icin lisans olusturur (SuperAdmin)</summary>
    [HttpPost("generate")]
    [Authorize(Policy = "SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<GenerateLicenseResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Generate(
        [FromBody] GenerateLicenseCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        if (!result.IsSuccess)
            return UnprocessableEntity(ApiResponse<object>.Fail(result.Error!));
        return Ok(ApiResponse<GenerateLicenseResult>.Ok(result.Data!));
    }

    /// <summary>Lisans aktivasyonu yapar</summary>
    [HttpPost("activate")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Activate(
        [FromBody] ActivateLicenseRequest request, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await mediator.Send(new ActivateLicenseCommand(
            request.LicenseKey,
            request.Domain,
            request.HardwareFingerprint,
            ip), ct);

        if (!result.IsSuccess)
            return BadRequest(ApiResponse<object>.Fail(result.Error!));
        return Ok(ApiResponse<string>.Ok(result.Data!));
    }
}

public sealed record ActivateLicenseRequest(
    string LicenseKey,
    string Domain,
    string HardwareFingerprint);
