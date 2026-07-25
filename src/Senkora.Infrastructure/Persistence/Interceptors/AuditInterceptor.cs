using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Senkora.Application.Common.Interfaces;
using Senkora.Domain.Entities.Common;
using Senkora.Infrastructure.Persistence;

namespace Senkora.Infrastructure.Persistence.Interceptors;

public sealed class AuditInterceptor(ICurrentUser currentUser) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateAuditFields(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result,
        CancellationToken ct = default)
    {
        UpdateAuditFields(eventData.Context);
        return base.SavingChangesAsync(eventData, result, ct);
    }

    private void UpdateAuditFields(DbContext? context)
    {
        if (context is null) return;

        // Kalici silme bayragi (ApplicationDbContext uzerinden set edilir)
        var suppressSoftDelete = context is ApplicationDbContext { SuppressSoftDelete: true };

        var actor = (currentUser.IsAuthenticated
            ? currentUser.UserId.ToString()
            : null) ?? "system";

        var now = DateTime.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = actor;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = actor;
                    break;

                case EntityState.Deleted:
                    // Kalici silme istenmisse kaydi gercekten sil
                    if (suppressSoftDelete) break;

                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted  = true;
                    entry.Entity.DeletedAt  = now;
                    entry.Entity.DeletedBy  = actor;
                    break;
            }
        }
    }
}
