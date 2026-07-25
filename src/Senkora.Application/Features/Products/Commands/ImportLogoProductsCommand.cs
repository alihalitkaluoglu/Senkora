using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Senkora.Application.Common.Interfaces;
using Senkora.Application.Common.Models;
using Senkora.Application.Common.Services;
using Senkora.Domain.Entities.Catalog;
using Senkora.Domain.Enums;

namespace Senkora.Application.Features.Products.Commands;

/// <summary>
/// Logo'daki aktif TM(1) ve MM(12) malzemelerini tarar,
/// Senkora'da OLMAYAN kayitlari ekler. Mevcut kayitlara dokunmaz.
/// </summary>
public sealed record ImportLogoProductsCommand(
    Guid TenantId,
    Guid LogoConnectionId,
    Guid WooStoreId,
    int  MaxScan = 0)   // 0 = tum katalog
    : IRequest<Result<ImportResult>>;

public sealed record ImportResult(
    int     Scanned,
    int     Created,
    int     AlreadyExists,
    int     PricesMatched,
    int     StockMatched,
    bool    Completed,
    string? Warning);

public sealed class ImportLogoProductsCommandHandler(
    IApplicationDbContext db,
    ILogoConnectionResolver resolver,
    ILogoProductService logoService,
    ILogger<ImportLogoProductsCommandHandler> logger)
    : IRequestHandler<ImportLogoProductsCommand, Result<ImportResult>>
{
    private const int ScanPerRound = 200;
    private const int MaxRounds    = 500;

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
                $"Logo baglantisi kurulamadi: {Unwrap(ex)}", "CONNECTION_FAILED");
        }

        var store = await db.WooStores.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.WooStoreId
                                   && s.TenantId == request.TenantId, ct);
        if (store is null)
            return Result<ImportResult>.Failure("Magaza bulunamadi.", "STORE_NOT_FOUND");

        // Fiyat kartlari
        Dictionary<long, List<LogoItemPriceDto>> priceGroups;
        try
        {
            var prices = await logoService.FetchSalesPricesAsync(conn.RestUrl, conn.AccessToken, ct);
            priceGroups = prices.GroupBy(p => p.ItemRef).ToDictionary(g => g.Key, g => g.ToList());
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Fiyat kartlari alinamadi");
            priceGroups = [];
        }

        // Stok
        Dictionary<long, decimal> stockMap;
        try
        {
            stockMap = await logoService.FetchStockAsync(
                conn.RestUrl, conn.AccessToken, conn.FirmNo, conn.PeriodNo, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Stok alinamadi");
            stockMap = [];
        }

        // Mevcut kayitlar — silinmisler dahil (unique index onlari da kapsar)
        // Silme islemi kalici oldugu icin normal sorgu yeterli
        var existing = await db.ProductMappings
            .Where(p => p.TenantId == request.TenantId && p.WooStoreId == request.WooStoreId)
            .Select(p => p.LogoItemRef)
            .ToListAsync(ct);
        var known = new HashSet<long>(existing);

        int scanned = 0, created = 0, exists = 0, priced = 0, stocked = 0;
        var offset = 0; var rounds = 0;
        var completed = false;
        string? warning = null;

        while (rounds < MaxRounds)
        {
            ct.ThrowIfCancellationRequested();

            if (request.MaxScan > 0 && scanned >= request.MaxScan)
            {
                warning = $"{request.MaxScan} kayit tarama siniri asildi.";
                break;
            }

            var scanNow = request.MaxScan > 0
                ? Math.Min(ScanPerRound, request.MaxScan - scanned)
                : ScanPerRound;

            LogoItemPage page;
            try
            {
                page = await logoService.FetchItemsAsync(
                    conn.RestUrl, conn.AccessToken, conn.FirmNo, offset, scanNow, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Logo tarama hatasi (offset={Offset})", offset);
                if (created == 0 && scanned == 0)
                    return Result<ImportResult>.Failure(
                        $"Logo'dan urun cekilemedi: {Unwrap(ex)}", "FETCH_FAILED");
                warning = $"Tarama offset={offset} noktasinda kesildi: {Unwrap(ex)}";
                break;
            }

            scanned += page.RawScanned;

            foreach (var item in page.Items)
            {
                // Zaten varsa atla — duplicate key hatasini onler
                if (!known.Add(item.LogicalRef)) { exists++; continue; }

                var sellPrice = item.SellPrice;
                var vatRate   = item.VatRate;

                if (priceGroups.TryGetValue(item.LogicalRef, out var candidates))
                {
                    var chosen = PriceSelector.Select(
                        candidates,
                        store.PriceProjectCode,
                        store.PriceTradingGroupCode,
                        store.PriceCostCenterCode);

                    if (chosen is not null && chosen.Price > 0)
                    {
                        sellPrice = chosen.Price;
                        priced++;
                        if (vatRate == 0 && chosen.VatRate > 0) vatRate = chosen.VatRate;
                    }
                }

                var stock = item.Stock;
                if (stockMap.TryGetValue(item.LogicalRef, out var qty)) { stock = qty; stocked++; }

                db.ProductMappings.Add(new ProductMapping
                {
                    TenantId         = request.TenantId,
                    WooStoreId       = request.WooStoreId,
                    LogoConnectionId = request.LogoConnectionId,
                    LogoItemRef      = item.LogicalRef,
                    LogoItemCode     = Cut(item.Code, 100)!,
                    LogoItemName     = Cut(item.Name, 500)!,
                    LogoGroupCode    = Cut(item.GroupCode, 100),
                    LogoSpecode      = Cut(item.Specode, 100),
                    LogoAuxDesc      = Cut(item.AuxDesc, 1000),
                    LogoDescription  = item.Description,
                    LogoSellPrice    = sellPrice,
                    LogoSellPrice2   = item.SellPrice2,
                    LogoVatRate      = vatRate,
                    LogoStock        = stock,
                    LogoWeight       = item.Weight,
                    LogoUnitCode     = Cut(item.UnitCode, 50),
                    LogoMarkRef      = item.MarkRef,
                    LogoCardType     = item.CardType,
                    LogoLastFetched  = DateTime.UtcNow,
                    WooSku           = Cut(item.Code, 200),
                    Status           = SyncMappingStatus.Draft,
                    CreatedBy        = request.TenantId.ToString()
                });

                created++;
            }

            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                var detail = Unwrap(ex);
                logger.LogError(ex, "Kayit hatasi (offset={Offset})", offset);
                return Result<ImportResult>.Failure(
                    $"Kayitlar veritabanina yazilamadi: {detail}", "DB_SAVE_FAILED");
            }

            offset = page.NextOffset;
            rounds++;

            if (!page.HasMore) { completed = true; break; }
        }

        if (rounds >= MaxRounds) warning = "Guvenlik siniri asildi.";

        if (stockMap.Count == 0)
            warning = (warning is null ? "" : warning + " ") +
                "Stok bilgisi alinamadi.";

        logger.LogInformation(
            "Import: {Scanned} tarandi, {Created} yeni, {Exists} mevcut, {Priced} fiyat, {Stocked} stok",
            scanned, created, exists, priced, stocked);

        return Result<ImportResult>.Success(new ImportResult(
            scanned, created, exists, priced, stocked, completed, warning));
    }

    private static string? Cut(string? v, int max)
        => v is null ? null : v.Length <= max ? v : v[..max];

    private static string Unwrap(Exception ex)
    {
        var cur = ex; var last = ex.Message; var d = 0;
        while (cur.InnerException is not null && d < 5)
        { cur = cur.InnerException; last = cur.Message; d++; }
        return last;
    }
}
