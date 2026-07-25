using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Senkora.Application.Common.Interfaces;
using Senkora.Application.Common.Models;
using Senkora.Application.Features.Integration.Logo.Commands;
using Senkora.Application.Features.Integration.Logo.Queries;

namespace Senkora.Api.Controllers.v1;

/// <summary>Logo ERP REST baglanti yonetimi</summary>
[ApiController]
[Route("api/v1/logo")]
[Authorize]
[Produces("application/json")]
public sealed class LogoController(
    IMediator mediator,
    ICurrentUser currentUser) : ControllerBase
{
    private Guid TenantId => HttpContext.Items["TenantId"] as Guid? ?? currentUser.TenantId;

    /// <summary>Tenant logo baglantilarini listeler</summary>
    [HttpGet("connections")]
    [ProducesResponseType(typeof(ApiResponse<List<LogoConnectionDto>>), 200)]
    public async Task<IActionResult> GetConnections(CancellationToken ct)
    {
        var result = await mediator.Send(new GetLogoConnectionsQuery(TenantId), ct);
        return Ok(ApiResponse<List<LogoConnectionDto>>.Ok(result.Data!));
    }

    /// <summary>Yeni Logo ERP baglantisi olusturur</summary>
    [HttpPost("connections")]
    [Authorize(Policy = "TenantAdmin")]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 201)]
    public async Task<IActionResult> Create(
        [FromBody] CreateLogoConnectionRequest request, CancellationToken ct)
    {
        var cmd = new CreateLogoConnectionCommand(
            TenantId, request.Name, request.RestUrl,
            request.ClientId, request.ClientSecret,
            request.Username, request.Password,
            request.FirmNo, request.PeriodNo, request.TimeoutSeconds);

        var result = await mediator.Send(cmd, ct);
        if (!result.IsSuccess)
            return UnprocessableEntity(ApiResponse<object>.Fail(result.Error!));

        return StatusCode(201, ApiResponse<Guid>.Ok(result.Data!, "Baglanti olusturuldu."));
    }

    /// <summary>Logo ERP baglantiyi gunceller</summary>
    [HttpPut("connections/{id:guid}")]
    [Authorize(Policy = "TenantAdmin")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateLogoConnectionRequest request, CancellationToken ct)
    {
        var cmd = new UpdateLogoConnectionCommand(
            id, TenantId, request.Name, request.RestUrl,
            request.ClientId, request.ClientSecret, request.Password,
            request.FirmNo, request.PeriodNo, request.TimeoutSeconds, request.IsActive);

        var result = await mediator.Send(cmd, ct);
        if (!result.IsSuccess)
            return NotFound(ApiResponse<object>.Fail(result.Error!));

        return Ok(ApiResponse.Ok("Baglanti guncellendi."));
    }

    /// <summary>Logo ERP baglantiyi siler</summary>
    [HttpDelete("connections/{id:guid}")]
    [Authorize(Policy = "TenantAdmin")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new DeleteLogoConnectionCommand(id, TenantId), ct);
        if (!result.IsSuccess)
            return NotFound(ApiResponse<object>.Fail(result.Error!));
        return Ok(ApiResponse.Ok("Baglanti silindi."));
    }

    /// <summary>Logo ERP REST servisine baglanti testi yapar</summary>
    [HttpPost("connections/test")]
    [Authorize(Policy = "TenantAdmin")]
    [ProducesResponseType(typeof(ApiResponse<LogoConnectionTestResult>), 200)]
    public async Task<IActionResult> TestConnection(
        [FromBody] TestLogoConnectionCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return Ok(ApiResponse<LogoConnectionTestResult>.Ok(result.Data!));
    }

    /// <summary>Fiyat kriterleri icin Logo secim listeleri (proje, TIG, masraf merkezi)</summary>
    /// <summary>Logo veritabaninda ada gore tablo arar (sema kesfi).</summary>
    [HttpGet("connections/{id:guid}/find-tables")]
    public async Task<IActionResult> FindTables(
        Guid id, [FromQuery] string pattern, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return BadRequest(ApiResponse<object>.Fail("Arama metni gerekli."));

        var result = await mediator.Send(
            new FindLogoTablesQuery(TenantId, id, pattern), ct);

        if (!result.IsSuccess)
            return BadRequest(ApiResponse<object>.Fail(result.Error!));
        return Ok(ApiResponse<List<string>>.Ok(result.Data!));
    }

    /// <summary>
    /// Logo REST'in hangi SQL cagri bicimini kabul ettigini test eder.
    /// Stok ve ticari islem grubu sorgulari calismiyorsa buradan tespit edilir.
    /// </summary>
    [HttpGet("connections/{id:guid}/probe-sql")]
    public async Task<IActionResult> ProbeSql(
        Guid id, [FromQuery] string? sql = null, CancellationToken ct = default)
    {
        var result = await mediator.Send(new ProbeLogoSqlQuery(TenantId, id, sql), ct);
        if (!result.IsSuccess)
            return BadRequest(ApiResponse<object>.Fail(result.Error!));
        return Ok(ApiResponse<List<LogoSqlProbe>>.Ok(result.Data!));
    }

    [HttpGet("connections/{id:guid}/lookups")]
    public async Task<IActionResult> GetLookups(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetLogoLookupsQuery(TenantId, id), ct);
        if (!result.IsSuccess)
            return BadRequest(ApiResponse<object>.Fail(result.Error!));
        return Ok(ApiResponse<LogoLookupResult>.Ok(result.Data!));
    }
}

// ── Request DTO'lari ─────────────────────────────────────────────────────────

public sealed record CreateLogoConnectionRequest(
    string Name,
    string RestUrl,
    string ClientId,
    string ClientSecret,
    string Username,
    string Password,
    int    FirmNo,
    int    PeriodNo       = 1,
    int    TimeoutSeconds = 30);

public sealed record UpdateLogoConnectionRequest(
    string  Name,
    string  RestUrl,
    string? ClientId,
    string? ClientSecret,
    string? Password,
    int     FirmNo,
    int     PeriodNo,
    int     TimeoutSeconds,
    bool    IsActive);
