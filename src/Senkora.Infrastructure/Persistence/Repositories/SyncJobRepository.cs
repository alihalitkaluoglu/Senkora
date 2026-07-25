using Microsoft.EntityFrameworkCore;
using Senkora.Domain.Entities.Sync;
using Senkora.Domain.Enums;
using Senkora.Domain.Interfaces.Repositories;

namespace Senkora.Infrastructure.Persistence.Repositories;

public sealed class SyncJobRepository(ApplicationDbContext db)
    : GenericRepository<SyncJob>(db), ISyncJobRepository
{
    public async Task<IReadOnlyList<SyncJob>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default)
        => await DbSet
            .Where(j => j.TenantId == tenantId)
            .OrderByDescending(j => j.CreatedAt)
            .Take(100)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<SyncJob>> GetByStatusAsync(
        Guid tenantId, SyncStatus status, CancellationToken ct = default)
        => await DbSet
            .Where(j => j.TenantId == tenantId && j.Status == status)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync(ct);

    public async Task<SyncJob?> GetLatestByTypeAsync(
        Guid tenantId, SyncJobType type, CancellationToken ct = default)
        => await DbSet
            .Where(j => j.TenantId == tenantId && j.JobType == type)
            .OrderByDescending(j => j.CreatedAt)
            .FirstOrDefaultAsync(ct);
}
