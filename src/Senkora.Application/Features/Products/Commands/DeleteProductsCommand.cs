using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Senkora.Application.Common.Interfaces;
using Senkora.Application.Common.Models;

namespace Senkora.Application.Features.Products.Commands;

/// <summary>
/// Secilen urun eslemelerini KALICI olarak siler.
///
/// ProductMappings uzerindeki (TenantId, LogoItemRef, WooStoreId) unique index
/// silinmis kayitlari da kapsadigi icin soft delete yapilirsa ayni urun
/// tekrar ice aktarilamaz. Bu yuzden SuppressSoftDelete bayragi kullanilir.
///
/// WooCommerce'e gonderilmis urunler magazada kalir, yalnizca esleme kaydi silinir.
/// </summary>
public sealed record DeleteProductsCommand(
    Guid       TenantId,
    List<Guid> ProductMappingIds,
    bool       DeleteAll    = false,
    string?    StatusFilter = null) : IRequest<Result<int>>;

public sealed class DeleteProductsCommandHandler(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    ILogger<DeleteProductsCommandHandler> logger)
    : IRequestHandler<DeleteProductsCommand, Result<int>>
{
    public async Task<Result<int>> Handle(
        DeleteProductsCommand request, CancellationToken ct)
    {
        var query = db.ProductMappings
            .IgnoreQueryFilters()
            .Where(p => p.TenantId == request.TenantId);

        if (!request.DeleteAll)
        {
            if (request.ProductMappingIds.Count == 0)
                return Result<int>.Failure("Silinecek urun secilmedi.", "NO_SELECTION");

            query = query.Where(p => request.ProductMappingIds.Contains(p.Id));
        }
        else if (!string.IsNullOrWhiteSpace(request.StatusFilter)
                 && Enum.TryParse<Domain.Enums.SyncMappingStatus>(
                        request.StatusFilter, out var status))
        {
            query = query.Where(p => p.Status == status);
        }

        var items = await query.ToListAsync(ct);
        if (items.Count == 0) return Result<int>.Success(0);

        var ids = items.Select(i => i.Id).ToList();

        // Bagli tarihce kayitlari
        var histories = await db.ProductSyncHistories
            .IgnoreQueryFilters()
            .Where(h => h.TenantId == request.TenantId && ids.Contains(h.ProductMappingId))
            .ToListAsync(ct);

        if (histories.Count > 0)
            db.ProductSyncHistories.RemoveRange(histories);

        db.ProductMappings.RemoveRange(items);

        // Bu SaveChanges'te soft delete devre disi — kayitlar gercekten silinir
        db.SuppressSoftDelete = true;
        try
        {
            await db.SaveChangesAsync(ct);
        }
        finally
        {
            db.SuppressSoftDelete = false;
        }

        logger.LogInformation(
            "{Count} urun eslemesi kalici olarak silindi ({Actor})",
            items.Count, currentUser.Email);

        return Result<int>.Success(items.Count);
    }
}
