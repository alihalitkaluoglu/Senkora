using Microsoft.EntityFrameworkCore;
using Senkora.Application.Common.Interfaces;
using Senkora.Domain.Interfaces.Services;
using Senkora.Infrastructure.Persistence;

namespace Senkora.Infrastructure.ExternalServices.WooCommerce;

public sealed class WooConnectionResolver(
    ApplicationDbContext db,
    IEncryptionService encryption) : IWooConnectionResolver
{
    public async Task<WooConnectionInfo> ResolveAsync(
        Guid tenantId, Guid storeId, CancellationToken ct = default)
    {
        var store = await db.WooStores.FirstOrDefaultAsync(
            s => s.Id == storeId && s.TenantId == tenantId && s.IsActive, ct)
            ?? throw new InvalidOperationException("WooCommerce magazasi bulunamadi.");

        var ck = encryption.Decrypt(store.ConsumerKeyEncrypted);
        var cs = encryption.Decrypt(store.ConsumerSecretEncrypted);

        string? wpPass = null;
        if (!string.IsNullOrWhiteSpace(store.WpAppPasswordEncrypted))
        {
            try { wpPass = encryption.Decrypt(store.WpAppPasswordEncrypted); }
            catch { wpPass = null; }
        }

        return new WooConnectionInfo(
            store.StoreUrl, ck, cs, store.WpUsername, wpPass);
    }
}
