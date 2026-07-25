using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Senkora.Application.Common.Models;
using Senkora.Application.Features.Sync.Commands;

namespace Senkora.Api.Controllers.v1;

[ApiController]
[Route("api/v1/sync")]
[Authorize]
[Produces("application/json")]
public sealed class SyncController(IMediator mediator) : ControllerBase
{
    /// <summary>Trigger product synchronization (Logo -> WooCommerce)</summary>
    [HttpPost("products")]
    [Authorize(Policy = "SyncManager")]
    public async Task<IActionResult> SyncProducts([FromBody] TriggerProductSyncCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return result.IsSuccess
            ? Accepted(ApiResponse<Guid>.Ok(result.Data!, "Sync job queued."))
            : BadRequest(ApiResponse<Guid>.Fail(result.Error!));
    }

    /// <summary>Trigger stock synchronization (Logo -> WooCommerce)</summary>
    [HttpPost("stock")]
    [Authorize(Policy = "SyncManager")]
    public async Task<IActionResult> SyncStock([FromBody] TriggerStockSyncCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return result.IsSuccess
            ? Accepted(ApiResponse<Guid>.Ok(result.Data!, "Stock sync queued."))
            : BadRequest(ApiResponse<Guid>.Fail(result.Error!));
    }

    /// <summary>Transfer WooCommerce orders to Logo ERP</summary>
    [HttpPost("orders")]
    [Authorize(Policy = "SyncManager")]
    public async Task<IActionResult> SyncOrders([FromBody] TriggerOrderSyncCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return result.IsSuccess
            ? Accepted(ApiResponse<Guid>.Ok(result.Data!, "Order sync queued."))
            : BadRequest(ApiResponse<Guid>.Fail(result.Error!));
    }
}
