using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Senkora.Application.Common.Interfaces;
using Senkora.Application.Common.Models;
using Senkora.Domain.Entities.Catalog;
using Senkora.Domain.Enums;

namespace Senkora.Application.Features.Products.Commands;

/// <summary>
/// Logo'daki TUM malzeme kartlarini tarar, veritabaninda olmayanlari ekler.
/// Mevcut kayitlara dokunmaz (zenginlestirme verisi korunur).
/// Buyuk katalog icin otomatik sayfalama yapar.
/// </summary>
public sealed record ImportLogoProductsCommand(
    Guid TenantId,
    Guid LogoConnectionId,
    Guid WooStoreId,
    /// <summary>0 = sinirsiz (tum katalog)</summary>
    int  MaxItems = 0) : IRequest<Result<ImportResult>>;

public sealed record ImportResult(
    int  Scanned,
    int  Created,
    int  AlreadyExists,
    int  PricesMatched,
    bool Completed,
    string? Warning);

public sealed class ImportLogoProductsCommandHandler(
    IApplicationDbContext db,
    ILogoConnectionResolver resolver,
    ILogoProductService logoService,
    ILogger<ImportLogoProductsCommandHandler> logger)
    : IRequestHandler<ImportLogoProductsCommand, Result<ImportResult>>
{
    private const int BatchSize      = 100;   // her turda Logo'dan istenecek kayit
    private const int MaxTotalBatches = 200;  // guvenlik siniri (200 x 100 = 20.000)

    public async Task<Result<ImportResult>> Handle(
        ImportLogoProductsCommand request, CancellationToken ct)
    {
        LogoConnectionInfo conn;
        try
        {
            conn = await resolver.ResolveAsync(request.TenantId, request.LogoConnectionId, ct);
        }
        catch (Exception ex)
        {
            return Result<ImportResult>.Failure(
                $"Logo baglantisi kurulamadi: {ex.Message}", "CONNECTION_FAILED");
        }

        // 1. Fiyat kartlarini bir kez cek (tum urunler icin ortak)
        Dictionary<long, LogoItemPriceDto> priceMap;
        try
        {
            priceMap = await logoService.FetchSalesPricesAsync(
                conn.RestUrl, conn.AccessToken, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Fiyat kartlari alinamadi, fiyatlar bos gelecek");
            priceMap = [];
        }

        // 2. Mevcut kayitlari yukle
        var existing = await db.ProductMappings
            .Where(p => p.TenantId == request.TenantId && p.WooStoreId == request.WooStoreId)
            .Select(p => p.LogoItemRef)
            .ToListAsync(ct);
        var existingSet = new HashSet<long>(existing);

        int scanned = 0, created = 0, exists = 0, priced = 0;
        var offset = 0;
        var batches = 0;
        var completed = false;
        string? warning = null;

        while (batches < MaxTotalBatches)
        {
            ct.ThrowIfCancellationRequested();

            if (request.MaxItems > 0 && scanned >= request.MaxItems)
            {
                warning = $"{request.MaxItems} kayit siniri asildi, tarama durduruldu.";
                break;
            }

            var take = request.MaxItems > 0
                ? Math.Min(BatchSize, request.MaxItems - scanned)
                : BatchSize;

            List<LogoItemDto> items;
            try
            {
                items = await logoService.FetchItemsAsync(
                    conn.RestUrl, conn.AccessToken, conn.FirmNo, offset, take, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Logo tarama hatasi (offset={Offset})", offset);
                if (created == 0)
                    return Result<ImportResult>.Failure(
                        $"Logo'dan urun cekilemedi: {ex.Message}", "FETCH_FAILED");

                warning = $"Tarama offset={offset} noktasinda kesildi: {ex.Message}";
                break;
            }

            // Logo daha kayit dondurmuyorsa katalog bitti
            if (items.Count == 0)
            {
                // Filtrelenmis kayitlar olabilir, bir batch daha dene
                var probe = await logoService.FetchItemsAsync(
                    conn.RestUrl, conn.AccessToken, conn.FirmNo, offset + take, 1, ct);
                if (probe.Count == 0) { completed = true; break; }
            }

            foreach (var item in items)
            {
                scanned++;

                if (existingSet.Contains(item.LogicalRef)) { exists++; continue; }

                var sellPrice = item.SellPrice;
                var vatRate   = item.VatRate;

                if (priceMap.TryGetValue(item.LogicalRef, out var p))
                {
                    if (p.Price > 0) { sellPrice = p.Price; priced++; }
                    if (vatRate == 0 && p.VatRate > 0) vatRate = p.VatRate;
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
                    LogoSellPrice    = sellPrice,
                    LogoSellPrice2   = item.SellPrice2,
                    LogoVatRate      = vatRate,
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

                existingSet.Add(item.LogicalRef);
                created++;
            }

            // Her batch sonunda kaydet — uzun islemde bellek sismesin
            if (created > 0 && created % BatchSize == 0)
                await db.SaveChangesAsync(ct);

            offset += Math.Max(items.Count, take);
            batches++;

            if (items.Count < take) { completed = true; break; }
        }

        await db.SaveChangesAsync(ct);

        if (batches >= MaxTotalBatches)
            warning = "Guvenlik siniri (20.000 kayit) asildi, tarama durduruldu.";

        logger.LogInformation(
            "Import tamamlandi: {Scanned} tarandi, {Created} yeni, {Exists} mevcut, {Priced} fiyat eslendi",
            scanned, created, exists, priced);

        return Result<ImportResult>.Success(
            new ImportResult(scanned, created, exists, priced, completed, warning));
    }
}
