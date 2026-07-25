using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Senkora.Application.Common.Interfaces;
using Senkora.Application.Common.Models;

namespace Senkora.Application.Features.Products.Commands;

/// <summary>
/// Secilen urun eslemelerini siler (soft delete).
/// WooCommerce'e gonderilmis urunler WooCommerce'de kalir, sadece esleme kaydi silinir.
/// </summary>
public sealed record DeleteProductsCommand(
    Guid       TenantId,
    List<Guid> ProductMappingIds,
    bool       DeleteAll = false,
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
        if (items.Count == 0)
            return Result<int>.Success(0);

        var now   = DateTime.UtcNow;
        var actor = currentUser.Email;

        foreach (var item in items)
        {
            item.IsDeleted = true;
            item.DeletedAt = now;
            item.DeletedBy = actor;
        }

        await db.SaveChangesAsync(ct);

        logger.LogInformation("{Count} urun eslemesi silindi ({Actor})", items.Count, actor);
        return Result<int>.Success(items.Count);
    }
}
