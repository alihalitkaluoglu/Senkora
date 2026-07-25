using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Senkora.Api.Hubs;

[Authorize]
public sealed class SyncHub : Hub
{
    public async Task JoinTenantGroup(string tenantId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant:{tenantId}");

    public async Task LeaveTenantGroup(string tenantId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tenant:{tenantId}");
}
