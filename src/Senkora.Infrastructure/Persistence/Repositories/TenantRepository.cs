using Microsoft.EntityFrameworkCore;
using Senkora.Domain.Entities.Tenants;
using Senkora.Domain.Interfaces.Repositories;

namespace Senkora.Infrastructure.Persistence.Repositories;

public sealed class TenantRepository(ApplicationDbContext db)
    : GenericRepository<Tenant>(db), ITenantRepository
{
    public async Task<Tenant?> GetBySubdomainAsync(string subdomain, CancellationToken ct = default)
        => await DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Subdomain == subdomain && t.IsActive, ct);

    public async Task<bool> SubdomainExistsAsync(string subdomain, CancellationToken ct = default)
        => await DbSet.AnyAsync(t => t.Subdomain == subdomain, ct);
}
