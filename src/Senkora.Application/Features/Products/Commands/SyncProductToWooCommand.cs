using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Senkora.Application.Common.Interfaces;
using Senkora.Application.Common.Models;
using Senkora.Domain.Enums;
using Senkora.Domain.ValueObjects;

namespace Senkora.Application.Features.Products.Commands;

public sealed record SyncProductToWooCommand(
    Guid TenantId,
    Guid ProductMappingId) : IRequest<Result<long>>;

public sealed class SyncProductToWooCommandHandler(
    IApplicationDbContext db,
    IWooConnectionResolver wooResolver,
    IWooProductService wooService,
    IWooMediaService mediaService,
    IFileStorageService fileStorage,
    ICurrentUser currentUser,
    ILogger<SyncProductToWooCommandHandler> logger)
    : IRequestHandler<SyncProductToWooCommand, Result<long>>
{
    public async Task<Result<long>> Handle(
        SyncProductToWooCommand request, CancellationToken ct)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var mapping = await db.ProductMappings.FirstOrDefaultAsync(
            p => p.Id == request.ProductMappingId && p.TenantId == request.TenantId, ct);

        if (mapping is null)
            return Result<long>.Failure("Urun eslemesi bulunamadi.", "NOT_FOUND");

        if (mapping.Status == SyncMappingStatus.Draft)
            return Result<long>.Failure(
                "Urun zenginlestirilmedi. Once Duzenle ile eslemeyi tamamlayin.",
                "NOT_ENRICHED");

        WooConnectionInfo woo;
        try
        {
            woo = await wooResolver.ResolveAsync(request.TenantId, mapping.WooStoreId, ct);
        }
        catch (Exception ex)
        {
            return Result<long>.Failure(
                $"WooCommerce baglantisi kurulamadi: {ex.Message}", "CONNECTION_FAILED");
        }

        var enrichment = mapping.EnrichmentJson is not null
            ? JsonConvert.DeserializeObject<ProductEnrichment>(mapping.EnrichmentJson)
              ?? new ProductEnrichment()
            : new ProductEnrichment();

        // ── Gorselleri WordPress medya kutuphanesine yukle ───────────────────
        var imageUrls = new List<string>();
        var mediaWarnings = new List<string>();

        if (enrichment.Images.Count > 0)
        {
            if (!woo.CanUploadMedia)
            {
                mediaWarnings.Add(
                    "Gorseller gonderilemedi: WooCommerce magaza ayarlarinda " +
                    "WordPress kullanici adi ve Application Password tanimli degil.");
            }
            else
            {
                // Once one cikan gorsel, sonra sira numarasina gore
                var ordered = enrichment.Images
                    .OrderBy(i => i.IsFeatured ? 0 : 1)
                    .ThenBy(i => i.SortOrder)
                    .ToList();

                foreach (var img in ordered)
                {
                    // Daha once yuklenmis mi? (WordPress URL'i saklanmis olabilir)
                    if (!string.IsNullOrWhiteSpace(img.RemoteUrl))
                    {
                        imageUrls.Add(img.RemoteUrl);
                        continue;
                    }

                    Stream? stream = null;
                    try
                    {
                        stream = await fileStorage.OpenReadAsync(img.StoredPath, ct);
                        var fileName    = Path.GetFileName(img.StoredPath);
                        var contentType = GuessContentType(fileName);

                        var res = await mediaService.UploadAsync(
                            woo.StoreUrl, woo.WpUsername!, woo.WpAppPassword!,
                            stream, fileName, contentType, ct);

                        if (res.IsSuccess && res.SourceUrl is not null)
                        {
                            imageUrls.Add(res.SourceUrl);
                            img.RemoteUrl = res.SourceUrl; // tekrar yuklemeyi engelle
                        }
                        else
                        {
                            mediaWarnings.Add(res.ErrorMessage ?? "Gorsel yuklenemedi.");
                        }
                    }
                    catch (FileNotFoundException)
                    {
                        mediaWarnings.Add($"Gorsel dosyasi bulunamadi: {img.StoredPath}");
                    }
                    catch (Exception ex)
                    {
                        mediaWarnings.Add($"Gorsel yukleme hatasi: {ex.Message}");
                    }
                    finally
                    {
                        if (stream is not null) await stream.DisposeAsync();
                    }
                }

                // Yuklenen WordPress URL'lerini kaydet
                mapping.EnrichmentJson = JsonConvert.SerializeObject(enrichment);
            }
        }

        var payload = BuildPayload(mapping, enrichment, imageUrls);

        try
        {
            mapping.Status = SyncMappingStatus.Pending;
            await db.SaveChangesAsync(ct);

            long wooId;
            if (mapping.WooProductId.HasValue)
            {
                await wooService.UpdateProductAsync(
                    woo.StoreUrl, woo.ConsumerKey, woo.ConsumerSecret,
                    mapping.WooProductId.Value, payload, ct);
                wooId = mapping.WooProductId.Value;
            }
            else
            {
                wooId = await wooService.CreateProductAsync(
                    woo.StoreUrl, woo.ConsumerKey, woo.ConsumerSecret, payload, ct);
            }

            mapping.WooProductId    = wooId;
            mapping.WooSku          = payload.Sku;
            mapping.WooProductName  = payload.Name;
            mapping.Status          = SyncMappingStatus.Synced;
            mapping.LastSyncedAt    = DateTime.UtcNow;
            mapping.LastSyncedPrice = mapping.LogoSellPrice;
            mapping.LastSyncedStock = mapping.LogoStock;
            mapping.LastSyncError   = mediaWarnings.Count > 0
                ? string.Join(" | ", mediaWarnings)
                : null;

            sw.Stop();
            db.ProductSyncHistories.Add(new Domain.Entities.Catalog.ProductSyncHistory
            {
                TenantId         = request.TenantId,
                ProductMappingId = mapping.Id,
                Action           = mapping.WooProductId.HasValue ? "WooUpdate" : "WooCreate",
                IsSuccess        = true,
                Message          = $"{imageUrls.Count} gorsel, " +
                                   $"{payload.Categories.Count} kategori, " +
                                   $"fiyat {payload.RegularPrice}" +
                                   (mediaWarnings.Count > 0
                                       ? $" | Uyari: {string.Join("; ", mediaWarnings)}" : ""),
                WooProductId     = wooId,
                DurationMs       = (int)sw.ElapsedMilliseconds,
                PerformedBy      = currentUser.Email,
                CreatedBy        = currentUser.Email,
            });
            await db.SaveChangesAsync(ct);

            logger.LogInformation("Urun gonderildi: {Code} -> WC #{WooId} ({ImgCount} gorsel)",
                mapping.LogoItemCode, wooId, imageUrls.Count);

            return Result<long>.Success(wooId);
        }
        catch (Exception ex)
        {
            sw.Stop();
            mapping.Status = SyncMappingStatus.Error;
            mapping.LastSyncError = mediaWarnings.Count > 0
                ? $"{ex.Message} | {string.Join(" | ", mediaWarnings)}"
                : ex.Message;

            db.ProductSyncHistories.Add(new Domain.Entities.Catalog.ProductSyncHistory
            {
                TenantId         = request.TenantId,
                ProductMappingId = mapping.Id,
                Action           = "Error",
                IsSuccess        = false,
                Message          = mapping.LastSyncError,
                DurationMs       = (int)sw.ElapsedMilliseconds,
                PerformedBy      = currentUser.Email,
                CreatedBy        = currentUser.Email,
            });
            await db.SaveChangesAsync(ct);

            logger.LogError(ex, "Urun gonderimi basarisiz: {Code}", mapping.LogoItemCode);
            return Result<long>.Failure(ex.Message, "SYNC_ERROR");
        }
    }

    private static string GuessContentType(string fileName)
        => Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".png"  => "image/png",
            ".webp" => "image/webp",
            ".gif"  => "image/gif",
            _       => "image/jpeg",
        };

    private static WooProductPayload BuildPayload(
        Domain.Entities.Catalog.ProductMapping m,
        ProductEnrichment e,
        List<string> imageUrls)
    {
        return new WooProductPayload
        {
            Name             = e.OverrideName ?? m.LogoItemName,
            Sku              = m.LogoItemCode,
            Type             = m.LogoCardType == 2 ? "virtual" : "simple",
            Status           = "publish",
            Description      = e.OverrideDescription ?? m.LogoDescription ?? "",
            ShortDescription = e.OverrideShortDesc   ?? m.LogoAuxDesc     ?? "",
            RegularPrice     = (e.RegularPriceOverride ?? m.LogoSellPrice).ToString("F2"),
            SalePrice        = e.SalePriceOverride?.ToString("F2"),
            DateOnSaleFrom   = e.SaleFrom?.ToString("yyyy-MM-ddTHH:mm:ss"),
            DateOnSaleTo     = e.SaleTo?.ToString("yyyy-MM-ddTHH:mm:ss"),
            ManageStock      = e.ManageStock,
            StockQuantity    = (int)m.LogoStock,
            StockStatus      = m.LogoStock > 0 ? "instock" : "outofstock",
            Backorders       = e.BackorderPolicy,
            Weight           = m.LogoWeight > 0 ? m.LogoWeight.ToString("F2") : null,
            Dimensions       = e.Dimensions is not null
                ? new WooDimensions(e.Dimensions.Length, e.Dimensions.Width, e.Dimensions.Height)
                : null,
            Categories       = e.WooCategoryIds.Select(id => new WooCatRef(id)).ToList(),
            Tags             = e.Tags.Select(t => new WooTagRef(t)).ToList(),
            // Yalnizca WordPress'e yuklenmis gercek URL'ler gonderilir
            Images           = imageUrls.Select((u, i) => new WooImage(u, null, i)).ToList(),
            Attributes       = e.Attributes
                .Select(a => new WooAttribute(a.Name, a.Options, a.Visible, a.Variation))
                .ToList(),
            ShippingClass    = e.ShippingClass,
            CatalogVisibility= e.CatalogVisibility ?? "visible",
            Featured         = e.Featured,
            Slug             = e.OverrideSlug,
            MetaData         = e.CustomMeta
                .Select(x => new WooMeta(x.Key, x.Value))
                .Append(new WooMeta("_logo_ref", m.LogoItemRef.ToString()))
                .ToList(),
        };
    }
}
