using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Senkora.Application.Common.Interfaces;
using Senkora.Application.Common.Models;
using Senkora.Application.Features.Integration.WooCommerce.Commands;
using Senkora.Application.Features.Integration.WooCommerce.Queries;

namespace Senkora.Api.Controllers.v1;

/// <summary>WooCommerce magaza yonetimi</summary>
[ApiController]
[Route("api/v1/woo")]
[Authorize]
[Produces("application/json")]
public sealed class WooController(
    IMediator mediator,
    ICurrentUser currentUser) : ControllerBase
{
    private Guid TenantId => HttpContext.Items["TenantId"] as Guid? ?? currentUser.TenantId;

    [HttpGet("stores")]
    public async Task<IActionResult> GetStores(CancellationToken ct)
    {
        var result = await mediator.Send(new GetWooStoresQuery(TenantId), ct);
        return Ok(ApiResponse<List<WooStoreDto>>.Ok(result.Data!));
    }

    [HttpPost("stores")]
    [Authorize(Policy = "TenantAdmin")]
    public async Task<IActionResult> Create(
        [FromBody] CreateWooStoreRequest req, CancellationToken ct)
    {
        var cmd = new CreateWooStoreCommand(
            TenantId, req.Name, req.StoreUrl,
            req.ConsumerKey, req.ConsumerSecret,
            req.WpUsername, req.WpAppPassword,
            req.PriceProjectCode, req.PriceTradingGroupCode, req.PriceCostCenterCode);

        var result = await mediator.Send(cmd, ct);
        if (!result.IsSuccess)
            return UnprocessableEntity(ApiResponse<object>.Fail(result.Error!));
        return StatusCode(201, ApiResponse<Guid>.Ok(result.Data!, "Magaza olusturuldu."));
    }

    [HttpPut("stores/{id:guid}")]
    [Authorize(Policy = "TenantAdmin")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateWooStoreRequest req, CancellationToken ct)
    {
        var cmd = new UpdateWooStoreCommand(
            id, TenantId, req.Name, req.StoreUrl,
            req.ConsumerKey, req.ConsumerSecret, req.IsActive,
            req.WpUsername, req.WpAppPassword,
            req.PriceProjectCode, req.PriceTradingGroupCode, req.PriceCostCenterCode);

        var result = await mediator.Send(cmd, ct);
        if (!result.IsSuccess)
            return NotFound(ApiResponse<object>.Fail(result.Error!));
        return Ok(ApiResponse.Ok("Magaza guncellendi."));
    }

    [HttpDelete("stores/{id:guid}")]
    [Authorize(Policy = "TenantAdmin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new DeleteWooStoreCommand(id, TenantId), ct);
        if (!result.IsSuccess)
            return NotFound(ApiResponse<object>.Fail(result.Error!));
        return Ok(ApiResponse.Ok("Magaza silindi."));
    }

    [HttpPost("stores/test")]
    [Authorize(Policy = "TenantAdmin")]
    public async Task<IActionResult> Test(
        [FromBody] TestWooConnectionCommand cmd, CancellationToken ct)
    {
        var result = await mediator.Send(cmd, ct);
        return Ok(ApiResponse<WooConnectionTestResult>.Ok(result.Data!));
    }
}

public sealed record CreateWooStoreRequest(
    string  Name,
    string  StoreUrl,
    string  ConsumerKey,
    string  ConsumerSecret,
    string? WpUsername            = null,
    string? WpAppPassword         = null,
    string? PriceProjectCode      = null,
    string? PriceTradingGroupCode = null,
    string? PriceCostCenterCode   = null);

public sealed record UpdateWooStoreRequest(
    string  Name,
    string  StoreUrl,
    string? ConsumerKey,
    string? ConsumerSecret,
    bool    IsActive,
    string? WpUsername            = null,
    string? WpAppPassword         = null,
    string? PriceProjectCode      = null,
    string? PriceTradingGroupCode = null,
    string? PriceCostCenterCode   = null);
