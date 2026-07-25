using Senkora.Domain.Entities.Sync;
using Senkora.Domain.Enums;

namespace Senkora.Domain.Interfaces.Repositories;

public interface ISyncJobRepository : IRepository<SyncJob>
{
    Task<IReadOnlyList<SyncJob>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<SyncJob>> GetByStatusAsync(Guid tenantId, SyncStatus status, CancellationToken ct = default);
    Task<SyncJob?> GetLatestByTypeAsync(Guid tenantId, SyncJobType type, CancellationToken ct = default);
}
