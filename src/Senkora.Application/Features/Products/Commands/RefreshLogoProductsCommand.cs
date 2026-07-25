using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Senkora.Application.Common.Interfaces;
using Senkora.Application.Common.Models;
using Senkora.Domain.Entities.Catalog;

namespace Senkora.Application.Features.Products.Commands;

/// <summary>
/// Veritabanindaki mevcut urunlerin Logo verilerini gunceller.
/// Zenginlestirme verisine (gorsel, kategori, etiket, ozellik) DOKUNMAZ.
/// Sadece Logo kaynakli alanlar guncellenir: ad, fiyat, stok, KDV, grup, aciklama.
/// </summary>
public sealed record RefreshLogoProductsCommand(
    Guid TenantId,
    Guid LogoConnectionId,
    Guid WooStoreId,
    bool PreviewOnly = false)   // true = sadece degisiklik ozeti, kayit yapmaz
    : IRequest<Result<RefreshResult>>;

public sealed record RefreshResult(
    int Total,
    int Updated,
    int Unchanged,
    int NotFoundInLogo,
    int PricesMatched,
    List<ProductChangePreview> Changes);

public sealed record ProductChangePreview(
    Guid     MappingId,
    string   Code,
    string   Name,
    string   Field,
    string?  OldValue,
    string?  NewValue);

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
            return Result<RefreshResult>.Success(
                new RefreshResult(0, 0, 0, 0, 0, []));

        // Fiyat kartlarini cek
        Dictionary<long, LogoItemPriceDto> priceMap;
        try
        {
            priceMap = await logoService.FetchSalesPricesAsync(conn.RestUrl, conn.AccessToken, ct);
        }
        catch { priceMap = []; }

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

            if (priceMap.TryGetValue(m.LogoItemRef, out var p))
            {
                if (p.Price > 0) { newPrice = p.Price; priced++; }
                if (newVat == 0 && p.VatRate > 0) newVat = p.VatRate;
            }

            var diff = new Dictionary<string, (string? Old, string? New)>();

            void Track(string field, object? oldV, object? newV)
            {
                var o = oldV?.ToString() ?? "";
                var n = newV?.ToString() ?? "";
                if (o != n) diff[field] = (o, n);
            }

            Track("Ad",       m.LogoItemName,   item.Name);
            Track("Fiyat",    m.LogoSellPrice,  newPrice);
            Track("KDV",      m.LogoVatRate,    newVat);
            Track("Stok",     m.LogoStock,      item.Stock);
            Track("Grup",     m.LogoGroupCode,  item.GroupCode);
            Track("Agirlik",  m.LogoWeight,     item.Weight);
            Track("Birim",    m.LogoUnitCode,   item.UnitCode);

            if (diff.Count == 0) { unchanged++; continue; }

            foreach (var (field, val) in diff)
                changes.Add(new ProductChangePreview(
                    m.Id, m.LogoItemCode, m.LogoItemName, field, val.Old, val.New));

            if (!request.PreviewOnly)
            {
                // SADECE Logo alanlari guncellenir — EnrichmentJson korunur
                m.LogoItemName    = item.Name;
                m.LogoSellPrice   = newPrice;
                m.LogoSellPrice2  = item.SellPrice2;
                m.LogoVatRate     = newVat;
                m.LogoStock       = item.Stock;
                m.LogoGroupCode   = item.GroupCode;
                m.LogoSpecode     = item.Specode;
                m.LogoAuxDesc     = item.AuxDesc;
                m.LogoDescription = item.Description;
                m.LogoWeight      = item.Weight;
                m.LogoUnitCode    = item.UnitCode;
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
            "Refresh {Mode}: {Total} urun, {Updated} guncellendi, {Unchanged} degismedi",
            request.PreviewOnly ? "ONIZLEME" : "UYGULANDI",
            mappings.Count, updated, unchanged);

        return Result<RefreshResult>.Success(new RefreshResult(
            Total:          mappings.Count,
            Updated:        updated,
            Unchanged:      unchanged,
            NotFoundInLogo: notFound,
            PricesMatched:  priced,
            Changes:        changes.Take(200).ToList()));
    }
}
