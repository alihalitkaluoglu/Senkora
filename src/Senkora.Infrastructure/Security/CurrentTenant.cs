using Microsoft.AspNetCore.Http;
using Senkora.Application.Common.Interfaces;

namespace Senkora.Infrastructure.Security;

public sealed class CurrentTenant(IHttpContextAccessor httpContextAccessor) : ICurrentTenant
{
    private const string TenantIdKey = "X-Tenant-Id";
    private const string TenantSubdomainKey = "X-Tenant-Subdomain";

    public Guid TenantId
    {
        get
        {
            var ctx = httpContextAccessor.HttpContext;
            if (ctx is null) return Guid.Empty;

            // 1) From header
            if (ctx.Request.Headers.TryGetValue(TenantIdKey, out var hdr)
                && Guid.TryParse(hdr, out var hdrId))
                return hdrId;

            // 2) From items (set by TenantMiddleware)
            if (ctx.Items.TryGetValue("TenantId", out var item)
                && item is Guid itemId)
                return itemId;

            return Guid.Empty;
        }
    }

    public string Subdomain
    {
        get
        {
            var ctx = httpContextAccessor.HttpContext;
            if (ctx is null) return string.Empty;

            if (ctx.Items.TryGetValue("TenantSubdomain", out var sub) && sub is string s)
                return s;

            // Extract from host: "acme.senkora.io" -> "acme"
            var host = ctx.Request.Host.Host;
            var parts = host.Split('.');
            return parts.Length > 2 ? parts[0] : string.Empty;
        }
    }

    public bool IsResolved => TenantId != Guid.Empty;
}
