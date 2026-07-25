using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Senkora.Application.Common.Interfaces;
using Senkora.Application.Common.Models;
using Senkora.Domain.Entities.Catalog;
using Senkora.Domain.Enums;

namespace Senkora.Application.Features.Products.Commands;

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
    ILogoConnectionResolver logoResolver,
    ILogoProductService productService,
    ILogger<FetchLogoProductsCommandHandler> logger)
    : IRequestHandler<FetchLogoProductsCommand, Result<FetchLogoProductsResult>>
{
    public async Task<Result<FetchLogoProductsResult>> Handle(
        FetchLogoProductsCommand request, CancellationToken ct)
    {
        // 1. Baglanti bilgilerini al (sifre cozme + token alma)
        LogoConnectionInfo connInfo;
        try
        {
            connInfo = await logoResolver.ResolveAsync(
                request.TenantId, request.LogoConnectionId, ct);
        }
        catch (Exception ex)
        {
            return Result<FetchLogoProductsResult>.Failure(
                $"Logo baglantisi kurulamadi: {ex.Message}", "CONNECTION_FAILED");
        }

        // 2. Logo'dan urunleri cek
        List<LogoItemDto> items;
        try
        {
            items = await productService.FetchItemsAsync(
                connInfo.RestUrl, connInfo.AccessToken,
                connInfo.FirmNo, request.Offset, request.Limit, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Logo urun cekme hatasi");
            return Result<FetchLogoProductsResult>.Failure(
                $"Logo'dan urun cekilemedi: {ex.Message}", "FETCH_FAILED");
        }

        if (items.Count == 0)
            return Result<FetchLogoProductsResult>.Success(
                new FetchLogoProductsResult(0, 0, 0, 0));

        // 3. Mevcut mapping'leri yukle
        var existing = await db.ProductMappings
            .Where(p => p.TenantId == request.TenantId
                && p.WooStoreId == request.WooStoreId)
            .Select(p => p.LogoItemRef)
            .ToListAsync(ct);
        var existingSet = new HashSet<long>(existing);

        int created = 0, updated = 0;
        const int skipped = 0;

        foreach (var item in items)
        {
            if (existingSet.Contains(item.LogicalRef))
            {
                var mapping = await db.ProductMappings.FirstOrDefaultAsync(
                    p => p.TenantId == request.TenantId
                      && p.LogoItemRef == item.LogicalRef
                      && p.WooStoreId == request.WooStoreId, ct);

                if (mapping is not null)
                {
                    mapping.LogoItemCode    = item.Code;
                    mapping.LogoItemName    = item.Name;
                    mapping.LogoGroupCode   = item.GroupCode;
                    mapping.LogoSpecode     = item.Specode;
                    mapping.LogoAuxDesc     = item.AuxDesc;
                    mapping.LogoDescription = item.Description;
                    mapping.LogoSellPrice   = item.SellPrice;
                    mapping.LogoSellPrice2  = item.SellPrice2;
                    mapping.LogoVatRate     = item.VatRate;
                    mapping.LogoStock       = item.Stock;
                    mapping.LogoWeight      = item.Weight;
                    mapping.LogoUnitCode    = item.UnitCode;
                    mapping.LogoMarkRef     = item.MarkRef;
                    mapping.LogoCardType    = item.CardType;
                    mapping.LogoLastFetched = DateTime.UtcNow;
                    updated++;
                }
            }
            else
            {
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
                created++;
            }
        }

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "FetchLogoProducts: {Fetched} fetched, {Created} new, {Updated} updated",
            items.Count, created, updated);

        return Result<FetchLogoProductsResult>.Success(
            new FetchLogoProductsResult(items.Count, created, updated, skipped));
    }
}
