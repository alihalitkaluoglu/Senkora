using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Senkora.Application.Common.Interfaces;
using Senkora.Application.Common.Models;

namespace Senkora.Application.Features.Products.Commands;

/// <summary>
/// Daha once soft-delete edilmis urun eslemelerini veritabanindan tamamen kaldirir.
/// Unique index silinmis satirlari da kapsadigi icin bu temizlik
/// yeniden ice aktarim sorunlarini onler.
/// </summary>
public sealed record PurgeDeletedProductsCommand(Guid TenantId) : IRequest<Result<int>>;

public sealed class PurgeDeletedProductsCommandHandler(
    IApplicationDbContext db,
    ILogger<PurgeDeletedProductsCommandHandler> logger)
    : IRequestHandler<PurgeDeletedProductsCommand, Result<int>>
{
    public async Task<Result<int>> Handle(
        PurgeDeletedProductsCommand request, CancellationToken ct)
    {
        var dead = await db.ProductMappings
            .IgnoreQueryFilters()
            .Where(p => p.TenantId == request.TenantId && p.IsDeleted)
            .ToListAsync(ct);

        if (dead.Count == 0) return Result<int>.Success(0);

        var ids = dead.Select(d => d.Id).ToList();

        var histories = await db.ProductSyncHistories
            .IgnoreQueryFilters()
            .Where(h => h.TenantId == request.TenantId && ids.Contains(h.ProductMappingId))
            .ToListAsync(ct);

        if (histories.Count > 0) db.ProductSyncHistories.RemoveRange(histories);
        db.ProductMappings.RemoveRange(dead);

        db.SuppressSoftDelete = true;
        try { await db.SaveChangesAsync(ct); }
        finally { db.SuppressSoftDelete = false; }

        logger.LogInformation("{Count} silinmis urun kaydi temizlendi", dead.Count);
        return Result<int>.Success(dead.Count);
    }
}
