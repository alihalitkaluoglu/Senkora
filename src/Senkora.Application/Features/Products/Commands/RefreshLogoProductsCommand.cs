using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Senkora.Application.Common.Interfaces;
using Senkora.Application.Common.Models;
using Senkora.Application.Common.Services;
using Senkora.Domain.Entities.Catalog;

namespace Senkora.Application.Features.Products.Commands;

/// <summary>
/// Mevcut urunlerin Logo verilerini gunceller.
/// Zenginlestirme verisine (gorsel, kategori, etiket) DOKUNMAZ.
/// </summary>
public sealed record RefreshLogoProductsCommand(
    Guid TenantId,
    Guid LogoConnectionId,
    Guid WooStoreId,
    bool PreviewOnly = false)   // true = sadece degisiklik ozeti
    : IRequest<Result<RefreshResult>>;

public sealed record RefreshResult(
    int Total,
    int Updated,
    int Unchanged,
    int NotFoundInLogo,
    int PricesMatched,
    List<ProductChangePreview> Changes);

public sealed record ProductChangePreview(
    Guid    MappingId,
    string  Code,
    string  Name,
    string  Field,
    string? OldValue,
    string? NewValue);

public sealed class RefreshLogoProductsCommandHandler(
    IApplicationDbContext db,
    ILogoConnectionResolver resolver,
    ILogoProductService logoService,
    ICurrentUser currentUser,
    ILogger<RefreshLogoProductsCommandHandler> logger)
    : IRequestHandler<RefreshLogoProductsCommand, Result<RefreshResult>>
{
    public async Task<Result<RefreshResult>> Handle(
        RefreshLogoProductsCommand request, CancellationToken ct)
    {
        LogoConnectionInfo conn;
        try
        {
            conn = await resolver.ResolveAsync(request.TenantId, request.LogoConnectionId, ct);
        }
        catch (Exception ex)
        {
            return Result<RefreshResult>.Failure(
                $"Logo baglantisi kurulamadi: {ex.Message}", "CONNECTION_FAILED");
        }

        var mappings = await db.ProductMappings
            .Where(p => p.TenantId == request.TenantId && p.WooStoreId == request.WooStoreId)
            .ToListAsync(ct);

        if (mappings.Count == 0)
            return Result<RefreshResult>.Success(new RefreshResult(0, 0, 0, 0, 0, []));

        var store = await db.WooStores.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.WooStoreId
                                   && s.TenantId == request.TenantId, ct);

        Dictionary<long, List<LogoItemPriceDto>> priceGroups;
        try
        {
            var prices = await logoService.FetchSalesPricesAsync(conn.RestUrl, conn.AccessToken, ct);
            priceGroups = prices.GroupBy(p => p.ItemRef).ToDictionary(g => g.Key, g => g.ToList());
        }
        catch { priceGroups = []; }

        Dictionary<long, decimal> stockMap;
        try
        {
            stockMap = await logoService.FetchStockAsync(
                conn.RestUrl, conn.AccessToken, conn.FirmNo, conn.PeriodNo, ct);
        }
        catch { stockMap = []; }

        int updated = 0, unchanged = 0, notFound = 0, priced = 0;
        var changes = new List<ProductChangePreview>();
        var actor   = currentUser.Email;

        foreach (var m in mappings)
        {
            ct.ThrowIfCancellationRequested();

            var item = await logoService.FetchItemByRefAsync(
                conn.RestUrl, conn.AccessToken, m.LogoItemRef, ct);

            if (item is null) { notFound++; continue; }

            var newPrice = item.SellPrice;
            var newVat   = item.VatRate;
            var newStock = item.Stock;

            if (priceGroups.TryGetValue(m.LogoItemRef, out var candidates))
            {
                var chosen = PriceSelector.Select(
                    candidates,
                    store?.PriceProjectCode,
                    store?.PriceTradingGroupCode,
                    store?.PriceCostCenterCode);

                if (chosen is not null && chosen.Price > 0)
                {
                    newPrice = chosen.Price;
                    priced++;
                    if (newVat == 0 && chosen.VatRate > 0) newVat = chosen.VatRate;
                }
            }

            if (stockMap.TryGetValue(m.LogoItemRef, out var qty)) newStock = qty;

            var diff = new Dictionary<string, (string? Old, string? New)>();

            // Metin alanlari
            void Track(string field, string? oldV, string? newV)
            {
                var o = (oldV ?? "").Trim();
                var n = (newV ?? "").Trim();
                if (!string.Equals(o, n, StringComparison.Ordinal))
                    diff[field] = (o, n);
            }

            // Sayisal alanlar — 0.0000 ile 0 ayni sayilir
            void TrackNum(string field, decimal oldV, decimal newV)
            {
                if (decimal.Round(oldV, 4) == decimal.Round(newV, 4)) return;
                diff[field] = (
                    oldV.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture),
                    newV.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture));
            }

            Track("Ad",      m.LogoItemName,  item.Name);
            TrackNum("Fiyat",   m.LogoSellPrice, newPrice);
            TrackNum("KDV",     m.LogoVatRate,   newVat);
            TrackNum("Stok",    m.LogoStock,     newStock);
            Track("Grup",    m.LogoGroupCode, item.GroupCode);
            TrackNum("Agirlik", m.LogoWeight,    item.Weight);
            Track("Birim",   m.LogoUnitCode,  item.UnitCode);

            if (diff.Count == 0) { unchanged++; continue; }

            foreach (var (field, val) in diff)
                changes.Add(new ProductChangePreview(
                    m.Id, m.LogoItemCode, m.LogoItemName, field, val.Old, val.New));

            if (!request.PreviewOnly)
            {
                m.LogoItemName    = Cut(item.Name, 500)!;
                m.LogoSellPrice   = newPrice;
                m.LogoSellPrice2  = item.SellPrice2;
                m.LogoVatRate     = newVat;
                m.LogoStock       = newStock;
                m.LogoGroupCode   = Cut(item.GroupCode, 100);
                m.LogoSpecode     = Cut(item.Specode, 100);
                m.LogoAuxDesc     = Cut(item.AuxDesc, 1000);
                m.LogoDescription = item.Description;
                m.LogoWeight      = item.Weight;
                m.LogoUnitCode    = Cut(item.UnitCode, 50);
                m.LogoMarkRef     = item.MarkRef;
                m.LogoCardType    = item.CardType;
                m.LogoLastFetched = DateTime.UtcNow;

                db.ProductSyncHistories.Add(new ProductSyncHistory
                {
                    TenantId         = request.TenantId,
                    ProductMappingId = m.Id,
                    Action           = "LogoRefresh",
                    IsSuccess        = true,
                    Message          = $"{diff.Count} alan guncellendi",
                    ChangesJson      = JsonConvert.SerializeObject(
                        diff.ToDictionary(k => k.Key,
                            v => new { old = v.Value.Old, @new = v.Value.New })),
                    PerformedBy      = actor,
                    CreatedBy        = actor,
                });
            }

            updated++;
        }

        if (!request.PreviewOnly)
            await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Refresh {Mode}: {Total} urun, {Updated} guncellendi",
            request.PreviewOnly ? "ONIZLEME" : "UYGULANDI", mappings.Count, updated);

        return Result<RefreshResult>.Success(new RefreshResult(
            mappings.Count, updated, unchanged, notFound, priced,
            changes.Take(200).ToList()));
    }

    private static string? Cut(string? v, int max)
        => v is null ? null : v.Length <= max ? v : v[..max];
}
