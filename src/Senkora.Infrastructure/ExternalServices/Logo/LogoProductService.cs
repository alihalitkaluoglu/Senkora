using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Senkora.Application.Common.Interfaces;

namespace Senkora.Infrastructure.ExternalServices.Logo;

/// <summary>
/// Logo REST malzeme karti ve fiyat karti entegrasyonu.
///
/// Alan isimleri (Logo REST v1 yanitindan dogrulandi):
///   items          → INTERNAL_REFERENCE, CODE, NAME, CARD_TYPE, VAT, GROUP_CODE
///   salesItemPrices → ITEMREF/INTERNAL_REFERENCE, CODE, PRICE, CURRENCY, PRICELISTREF
/// </summary>
public sealed class LogoProductService(
    LogoRestClient client,
    ILogger<LogoProductService> logger) : ILogoProductService
{
    /// <summary>20=Malzeme Sinifi, 22=Sistem kaydi → urun degil</summary>
    private static readonly int[] ExcludedCardTypes = [20, 22];

    private const int PageSize = 25;

    public async Task<int> GetItemCountAsync(
        string restUrl, string accessToken, CancellationToken ct = default)
    {
        try
        {
            // Logo liste yanitinda "count" alani sayfadaki kayit sayisini verir,
            // toplam icin buyuk bir limit ile son sayfaya bakmak gerekir.
            // Pratik yaklasim: limit=1 ile Meta bilgisini oku.
            var url  = $"{restUrl.TrimEnd('/')}/api/v1/items?offset=0&limit=1";
            var json = await client.GetAsync(url, accessToken, ct);
            var obj  = JObject.Parse(json);

            // Logo bazi surumlerde totalCount dondurur
            var total = obj.Value<int?>("totalCount")
                     ?? obj.Value<int?>("total")
                     ?? 0;

            return total;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Logo item count alinamadi");
            return 0;
        }
    }

    public async Task<List<LogoItemDto>> FetchItemsAsync(
        string restUrl, string accessToken,
        int firmNo, int offset = 0, int limit = 100,
        CancellationToken ct = default)
    {
        var result  = new List<LogoItemDto>();
        var scanned = 0;
        var current = offset;

        while (scanned < limit)
        {
            ct.ThrowIfCancellationRequested();

            var take = Math.Min(PageSize, limit - scanned);
            var url  = $"{restUrl.TrimEnd('/')}/api/v1/items?offset={current}&limit={take}";

            string raw;
            try
            {
                raw = await client.GetAsync(url, accessToken, ct);
            }
            catch (Exception ex)
            {
                if (result.Count == 0 && scanned == 0)
                    throw new InvalidOperationException(
                        $"Logo REST'ten veri alinamadi: {ex.Message}", ex);

                logger.LogWarning(ex,
                    "Logo sayfa offset={Offset} alinamadi, {Count} kayitla devam", current, result.Count);
                break;
            }

            if (string.IsNullOrWhiteSpace(raw)) break;

            var (items, rawCount) = ParsePage(raw, logger);
            result.AddRange(items);

            scanned += rawCount;
            current += rawCount;

            if (rawCount < take) break;   // liste bitti
            if (rawCount == 0) break;     // sonsuz dongu koruması
        }

        logger.LogInformation(
            "Logo items: {Valid} gecerli / {Scanned} taranan (offset={Offset})",
            result.Count, scanned, offset);

        return result;
    }

    public async Task<LogoItemDto?> FetchItemByRefAsync(
        string restUrl, string accessToken,
        long itemRef, CancellationToken ct = default)
    {
        try
        {
            var url  = $"{restUrl.TrimEnd('/')}/api/v1/items/{itemRef}";
            var json = await client.GetAsync(url, accessToken, ct);
            return ParseItem(JObject.Parse(json), logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Logo item {Ref} okunamadi", itemRef);
            return null;
        }
    }

    // ── Satis fiyat kartlari ──────────────────────────────────────────────────
    public async Task<Dictionary<long, LogoItemPriceDto>> FetchSalesPricesAsync(
        string restUrl, string accessToken, CancellationToken ct = default)
    {
        var map     = new Dictionary<long, LogoItemPriceDto>();
        var offset  = 0;
        const int take = 100;   // fiyat kartlari hafif, daha buyuk sayfa alinabilir

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            var url = $"{restUrl.TrimEnd('/')}/api/v1/salesItemPrices" +
                      $"?offset={offset}&limit={take}";

            string raw;
            try
            {
                raw = await client.GetAsync(url, accessToken, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Logo satis fiyat kartlari alinamadi (offset={Offset}). " +
                    "Fiyatlar bos gelecek.", offset);
                break;
            }

            if (string.IsNullOrWhiteSpace(raw)) break;

            JArray arr;
            try
            {
                var trimmed = raw.TrimStart();
                if (trimmed.StartsWith('['))
                    arr = JArray.Parse(raw);
                else
                {
                    var obj = JObject.Parse(raw);
                    if (obj["Message"] != null)
                    {
                        logger.LogWarning("Logo fiyat karti hatasi: {Msg}", obj["Message"]);
                        break;
                    }
                    arr = obj["items"] as JArray ?? obj["Items"] as JArray ?? [];
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Logo fiyat karti yaniti ayristirilamadi");
                break;
            }

            if (arr.Count == 0) break;

            foreach (var t in arr)
            {
                var price = ParsePrice(t);
                if (price is null) continue;

                // Ayni malzeme icin birden fazla fiyat karti olabilir.
                // En guncel/gecerli olani tut: tarih araligi uygun ve en yuksek liste no.
                if (map.TryGetValue(price.ItemRef, out var existing))
                {
                    if (IsBetterPrice(price, existing))
                        map[price.ItemRef] = price;
                }
                else
                {
                    map[price.ItemRef] = price;
                }
            }

            offset += arr.Count;
            if (arr.Count < take) break;
        }

        logger.LogInformation("Logo satis fiyat kartlari: {Count} malzeme icin fiyat bulundu", map.Count);
        return map;
    }

    private static bool IsBetterPrice(LogoItemPriceDto candidate, LogoItemPriceDto current)
    {
        var now = DateTime.UtcNow.Date;

        bool CandidateValid() =>
            (candidate.BeginDate is null || candidate.BeginDate <= now) &&
            (candidate.EndDate   is null || candidate.EndDate   >= now);
        bool CurrentValid() =>
            (current.BeginDate is null || current.BeginDate <= now) &&
            (current.EndDate   is null || current.EndDate   >= now);

        // Gecerli tarih araligindaki fiyat tercih edilir
        if (CandidateValid() && !CurrentValid()) return true;
        if (!CandidateValid() && CurrentValid()) return false;

        // Ikisi de gecerliyse fiyati 0 olmayan tercih edilir
        if (candidate.Price > 0 && current.Price == 0) return true;
        if (candidate.Price == 0 && current.Price > 0) return false;

        // Son olarak daha yuksek liste referansi (genelde daha yeni tanim)
        return candidate.PriceListRef > current.PriceListRef;
    }

    private static LogoItemPriceDto? ParsePrice(JToken t)
    {
        try
        {
            var itemRef = t.Value<long?>("ITEMREF")
                       ?? t.Value<long?>("ITEM_REFERENCE")
                       ?? t.Value<long?>("CARDREF")
                       ?? 0;
            if (itemRef == 0) return null;

            var price = t.Value<decimal?>("PRICE")
                     ?? t.Value<decimal?>("UNIT_PRICE")
                     ?? 0;

            DateTime? Parse(string key)
            {
                var v = t.Value<DateTime?>(key);
                // Logo bos tarihi 1899-12-30 olarak dondurur
                return v is null || v.Value.Year < 1950 ? null : v;
            }

            return new LogoItemPriceDto(
                ItemRef:      itemRef,
                ItemCode:     (t.Value<string>("CODE") ?? "").Trim(),
                Price:        price,
                VatRate:      t.Value<decimal?>("VAT") ?? t.Value<decimal?>("VAT_RATE") ?? 0,
                CurrencyCode: t.Value<string>("CURRENCY") ?? t.Value<string>("CURR_CODE"),
                PriceListRef: t.Value<int?>("PRICELISTREF")
                           ?? t.Value<int?>("INTERNAL_REFERENCE") ?? 0,
                BeginDate:    Parse("BEGDATE") ?? Parse("BEGIN_DATE"),
                EndDate:      Parse("ENDDATE") ?? Parse("END_DATE"));
        }
        catch { return null; }
    }

    // ── Ayristirma ────────────────────────────────────────────────────────────
    private static (List<LogoItemDto> Items, int RawCount) ParsePage(string raw, ILogger logger)
    {
        JArray arr;
        var trimmed = raw.TrimStart();

        if (trimmed.StartsWith('['))
        {
            arr = JArray.Parse(raw);
        }
        else
        {
            var obj = JObject.Parse(raw);
            if (obj["Message"] != null)
                throw new InvalidOperationException($"Logo REST hatasi: {obj["Message"]}");
            arr = obj["items"] as JArray ?? obj["Items"] as JArray ?? [];
        }

        var list = new List<LogoItemDto>();
        foreach (var t in arr)
        {
            var item = ParseItem(t, logger);
            if (item is not null) list.Add(item);
        }
        return (list, arr.Count);
    }

    private static LogoItemDto? ParseItem(JToken t, ILogger logger)
    {
        try
        {
            var reference = t.Value<long?>("INTERNAL_REFERENCE")
                         ?? t.Value<long?>("LOGICALREF") ?? 0;
            if (reference == 0) return null;

            var cardType = t.Value<int?>("CARD_TYPE")
                        ?? t.Value<int?>("CARDTYPE") ?? 1;
            if (ExcludedCardTypes.Contains(cardType)) return null;

            var code = (t.Value<string>("CODE") ?? "").Trim();
            if (string.IsNullOrWhiteSpace(code) || code.Length < 2) return null;

            var name = (t.Value<string>("NAME")
                     ?? t.Value<string>("DESCRIPTION") ?? "").Trim();

            return new LogoItemDto(
                LogicalRef:  reference,
                Code:        code,
                Name:        string.IsNullOrWhiteSpace(name) ? code : name,
                AuxDesc:     t.Value<string>("AUXIL_CODE")   ?? t.Value<string>("AUXDESC"),
                Description: t.Value<string>("SPECIAL_DESC") ?? t.Value<string>("DEFINITION_"),
                GroupCode:   t.Value<string>("GROUP_CODE")   ?? t.Value<string>("STGRPCODE"),
                Specode:     t.Value<string>("SPECIAL_CODE") ?? t.Value<string>("SPECODE"),
                UnitCode:    t.Value<string>("UNIT_CODE")    ?? t.Value<string>("UNITSETCODE"),
                MarkRef:     t.Value<long?>("MARKREF")       ?? t.Value<long?>("MARK_REFERENCE"),
                CardType:    cardType,
                SellPrice:   t.Value<decimal?>("SELLPRICE")  ?? 0,
                SellPrice2:  t.Value<decimal?>("SELLPRICE2") ?? 0,
                VatRate:     t.Value<decimal?>("VAT") ?? t.Value<decimal?>("VAT_RATE") ?? 0,
                Stock:       t.Value<decimal?>("ONHAND") ?? 0,
                Weight:      t.Value<decimal?>("UNITWEIGHT") ?? t.Value<decimal?>("WEIGHT") ?? 0);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Logo kayit ayristirilamadi");
            return null;
        }
    }
}
