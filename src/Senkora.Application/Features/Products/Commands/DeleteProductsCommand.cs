using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Senkora.Application.Common.Interfaces;
using Senkora.Application.Common.Models;

namespace Senkora.Application.Features.Products.Commands;

/// <summary>
/// Secilen urun eslemelerini KALICI olarak siler.
///
/// ExecuteDeleteAsync kullanilir cunku AuditInterceptor normal silmeleri
/// soft delete'e cevirir. ProductMappings uzerindeki
/// (TenantId, LogoItemRef, WooStoreId) unique index silinmis kayitlari da
/// kapsadigi icin soft delete yapilirsa ayni urun tekrar ice aktarilamaz.
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

        // Silinecek ID'leri al (tarihce temizligi icin)
        var ids = await query.Select(p => p.Id).ToListAsync(ct);
        if (ids.Count == 0) return Result<int>.Success(0);

        // Bagli tarihce kayitlarini kalici sil
        await db.ProductSyncHistories
            .IgnoreQueryFilters()
            .Where(h => h.TenantId == request.TenantId && ids.Contains(h.ProductMappingId))
            .ExecuteDeleteAsync(ct);

        // Eslemeleri kalici sil — AuditInterceptor'i bypass eder
        var deleted = await query.ExecuteDeleteAsync(ct);

        logger.LogInformation(
            "{Count} urun eslemesi kalici olarak silindi ({Actor})",
            deleted, currentUser.Email);

        return Result<int>.Success(deleted);
    }
}
