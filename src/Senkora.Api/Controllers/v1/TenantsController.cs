using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Senkora.Application.Common.Models;
using Senkora.Application.Features.Tenants.Commands;
using Senkora.Application.Features.Tenants.Queries;

namespace Senkora.Api.Controllers.v1;

/// <summary>Firma (Tenant) yonetimi — SuperAdmin yetkisi gerektirir</summary>
[ApiController]
[Route("api/v1/tenants")]
[Authorize(Policy = "SuperAdmin")]
[Produces("application/json")]
public sealed class TenantsController(IMediator mediator) : ControllerBase
{
    /// <summary>Tum firmalari listeler</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<TenantDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetTenantsQuery(page, pageSize, search), ct);
        return Ok(ApiResponse<PagedResult<TenantDto>>.Ok(result.Data!));
    }

    /// <summary>Yeni firma olusturur</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateTenantCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);

        if (!result.IsSuccess)
            return UnprocessableEntity(ApiResponse<object>.Fail(result.Error!));

        return CreatedAtAction(nameof(GetAll),
            ApiResponse<Guid>.Ok(result.Data!, "Firma olusturuldu."));
    }
}
