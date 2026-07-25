using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Senkora.Application.Common.Interfaces;
using Senkora.Application.Common.Models;
using Senkora.Domain.Enums;

namespace Senkora.Api.Controllers.v1;

[ApiController]
[Route("api/v1/dashboard")]
[Authorize]
[Produces("application/json")]
public sealed class DashboardController(IApplicationDbContext db) : ControllerBase
{
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        var tenantId = HttpContext.Items["TenantId"] as Guid? ?? Guid.Empty;

        var totalJobs     = await db.SyncJobs.CountAsync(j => j.TenantId == tenantId, ct);
        var successJobs   = await db.SyncJobs.CountAsync(j => j.TenantId == tenantId && j.Status == SyncStatus.Completed, ct);
        var failedJobs    = await db.SyncJobs.CountAsync(j => j.TenantId == tenantId && j.Status == SyncStatus.Failed, ct);
        var pendingJobs   = await db.SyncJobs.CountAsync(j => j.TenantId == tenantId && j.Status == SyncStatus.Pending, ct);
        var totalProducts = await db.ProductMappings.CountAsync(p => p.TenantId == tenantId, ct);
        var totalOrders   = await db.OrderMappings.CountAsync(o => o.TenantId == tenantId, ct);
        var lastSync      = await db.SyncJobs
            .Where(j => j.TenantId == tenantId && j.CompletedAt.HasValue)
            .OrderByDescending(j => j.CompletedAt)
            .Select(j => j.CompletedAt)
            .FirstOrDefaultAsync(ct);

        return Ok(ApiResponse<DashboardStatsDto>.Ok(new DashboardStatsDto(
            totalJobs, successJobs, failedJobs, pendingJobs, totalProducts, totalOrders, lastSync)));
    }
}

public sealed record DashboardStatsDto(
    int TotalSyncJobs, int SuccessfulJobs, int FailedJobs,
    int PendingJobs, int TotalProducts, int TotalOrders,
    DateTime? LastSyncAt);
