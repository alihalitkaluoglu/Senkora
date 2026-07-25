using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Senkora.Application.Common.Interfaces;
using Senkora.Application.Common.Models;
using Senkora.Domain.Entities.Catalog;
using Senkora.Domain.Enums;

namespace Senkora.Application.Features.Products.Commands;

/// <summary>
/// ESKI API — geriye donuk uyumluluk icin korunuyor.
/// Yeni gelistirmelerde <see cref="ImportLogoProductsCommand"/> kullanilmalidir.
/// Belirli bir offset/limit araligindaki kayitlari ceker.
/// </summary>
public sealed record FetchLogoProductsCommand(
    Guid TenantId,
    Guid LogoConnectionId,
    Guid WooStoreId,
    int  Offset = 0,
    int  Limit  = 100) : IRequest<Result<FetchLogoProductsResult>>;

public sealed record FetchLogoProductsResult(
    int Fetched, int Created, int Updated, int Skipped);

public sealed class FetchLogoProductsCommandHandler(
    IApplicationDbContext db,
    ILogoConnectionResolver resolver,
    ILogoProductService productService,
    ILogger<FetchLogoProductsCommandHandler> logger)
    : IRequestHandler<FetchLogoProductsCommand, Result<FetchLogoProductsResult>>
{
    public async Task<Result<FetchLogoProductsResult>> Handle(
        FetchLogoProductsCommand request, CancellationToken ct)
    {
        LogoConnectionInfo conn;
        try
        {
            conn = await resolver.ResolveAsync(request.TenantId, request.LogoConnectionId, ct);
        }
        catch (Exception ex)
        {
            return Result<FetchLogoProductsResult>.Failure(
                $"Logo baglantisi kurulamadi: {ex.Message}", "CONNECTION_FAILED");
        }

        LogoItemPage page;
        try
        {
            page = await productService.FetchItemsAsync(
                conn.RestUrl, conn.AccessToken, conn.FirmNo,
                request.Offset, request.Limit, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Logo urun cekme hatasi");
            return Result<FetchLogoProductsResult>.Failure(
                $"Logo'dan urun cekilemedi: {ex.Message}", "FETCH_FAILED");
        }

        if (page.Items.Count == 0)
            return Result<FetchLogoProductsResult>.Success(
                new FetchLogoProductsResult(page.RawScanned, 0, 0, 0));

        var existing = await db.ProductMappings
            .IgnoreQueryFilters()
            .Where(p => p.TenantId == request.TenantId && p.WooStoreId == request.WooStoreId)
            .Select(p => p.LogoItemRef)
            .ToListAsync(ct);
        var known = new HashSet<long>(existing);

        int created = 0, updated = 0;

        foreach (var item in page.Items)
        {
            if (known.Contains(item.LogicalRef))
            {
                var m = await db.ProductMappings.FirstOrDefaultAsync(
                    p => p.TenantId == request.TenantId
                      && p.LogoItemRef == item.LogicalRef
                      && p.WooStoreId == request.WooStoreId, ct);

                if (m is not null)
                {
                    m.LogoItemName    = item.Name;
                    m.LogoSellPrice   = item.SellPrice;
                    m.LogoStock       = item.Stock;
                    m.LogoVatRate     = item.VatRate;
                    m.LogoLastFetched = DateTime.UtcNow;
                    updated++;
                }
                continue;
            }

            db.ProductMappings.Add(new ProductMapping
            {
                TenantId         = request.TenantId,
                WooStoreId       = request.WooStoreId,
                LogoConnectionId = request.LogoConnectionId,
                LogoItemRef      = item.LogicalRef,
                LogoItemCode     = item.Code,
                LogoItemName     = item.Name,
                LogoGroupCode    = item.GroupCode,
                LogoSpecode      = item.Specode,
                LogoAuxDesc      = item.AuxDesc,
                LogoDescription  = item.Description,
                LogoSellPrice    = item.SellPrice,
                LogoSellPrice2   = item.SellPrice2,
                LogoVatRate      = item.VatRate,
                LogoStock        = item.Stock,
                LogoWeight       = item.Weight,
                LogoUnitCode     = item.UnitCode,
                LogoMarkRef      = item.MarkRef,
                LogoCardType     = item.CardType,
                LogoLastFetched  = DateTime.UtcNow,
                WooSku           = item.Code,
                Status           = SyncMappingStatus.Draft,
                CreatedBy        = request.TenantId.ToString()
            });

            known.Add(item.LogicalRef);
            created++;
        }

        await db.SaveChangesAsync(ct);

        return Result<FetchLogoProductsResult>.Success(
            new FetchLogoProductsResult(page.RawScanned, created, updated, 0));
    }
}
