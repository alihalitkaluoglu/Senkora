using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Senkora.Application.Common.Interfaces;

namespace Senkora.Infrastructure.Security;

public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public Guid UserId
    {
        get
        {
            var sub = Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? Principal?.FindFirstValue("sub");
            return sub is not null && Guid.TryParse(sub, out var id) ? id : Guid.Empty;
        }
    }

    public Guid TenantId
    {
        get
        {
            var tid = Principal?.FindFirstValue("tenantId");
            return tid is not null && Guid.TryParse(tid, out var id) ? id : Guid.Empty;
        }
    }

    public string Email
        => Principal?.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

    public IEnumerable<string> Roles
        => Principal?.FindAll(ClaimTypes.Role).Select(c => c.Value) ?? [];

    public bool IsAuthenticated
        => Principal?.Identity?.IsAuthenticated ?? false;

    public bool IsGlobalAdmin
        => Principal?.IsInRole("SuperAdmin") ?? false;
}
