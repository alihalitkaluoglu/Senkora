using Senkora.Domain.Entities.Tenants;

namespace Senkora.Domain.Interfaces.Repositories;

public interface ITenantRepository : IRepository<Tenant>
{
    Task<Tenant?> GetBySubdomainAsync(string subdomain, CancellationToken ct = default);
    Task<bool> SubdomainExistsAsync(string subdomain, CancellationToken ct = default);
}
