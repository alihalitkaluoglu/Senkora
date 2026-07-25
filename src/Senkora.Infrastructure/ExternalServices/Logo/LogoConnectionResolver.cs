using Microsoft.EntityFrameworkCore;
using Senkora.Application.Common.Interfaces;
using Senkora.Domain.Interfaces.Services;
using Senkora.Infrastructure.Persistence;

namespace Senkora.Infrastructure.ExternalServices.Logo;

public sealed class LogoConnectionResolver(
    ApplicationDbContext db,
    ILogoTokenManager tokenManager) : ILogoConnectionResolver
{
    public async Task<LogoConnectionInfo> ResolveAsync(
        Guid tenantId, Guid connectionId, CancellationToken ct = default)
    {
        var conn = await db.LogoConnections.FirstOrDefaultAsync(
            c => c.Id == connectionId && c.TenantId == tenantId && c.IsActive, ct)
            ?? throw new InvalidOperationException("Logo baglantisi bulunamadi.");

        // LogoTokenManager tum sifre cozme ve token alma islemini iceride halleder
        var token = await tokenManager.GetTokenAsync(connectionId, ct);

        return new LogoConnectionInfo(
            conn.RestUrl, token.AccessToken, conn.FirmNo, conn.Username);
    }
}
