using Microsoft.EntityFrameworkCore;
using Senkora.Application.Common.Interfaces;

namespace Senkora.Api.Middleware;

public sealed class TenantMiddleware(RequestDelegate next)
{
    private static readonly string[] PublicPaths =
    [
        "/api/v1/auth",
        "/swagger",
        "/health",
        "/hangfire",
        "/hubs"
    ];

    public async Task InvokeAsync(HttpContext context, IApplicationDbContext db)
    {
        var path = context.Request.Path.Value ?? "";

        if (PublicPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await next(context);
            return;
        }

        // 1. Header
        if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var hdr)
            && Guid.TryParse(hdr, out var tenantId))
        {
            context.Items["TenantId"] = tenantId;
            await next(context);
            return;
        }

        // 2. Subdomain
        var host  = context.Request.Host.Host;
        var parts = host.Split('.');
        if (parts.Length > 2)
        {
            var sub    = parts[0];
            var tenant = await db.Tenants
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Subdomain == sub && t.IsActive && !t.IsDeleted);
            if (tenant is not null)
            {
                context.Items["TenantId"]        = tenant.Id;
                context.Items["TenantSubdomain"] = sub;
                await next(context);
                return;
            }
        }

        // 3. JWT claim
        var claim = context.User?.FindFirst("tenantId")?.Value;
        if (claim is not null && Guid.TryParse(claim, out var claimTenant))
            context.Items["TenantId"] = claimTenant;

        await next(context);
    }
}
