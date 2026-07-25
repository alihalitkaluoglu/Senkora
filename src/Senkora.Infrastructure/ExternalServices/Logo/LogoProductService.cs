using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Senkora.Application.Common.Interfaces;

namespace Senkora.Infrastructure.ExternalServices.Logo;

/// <summary>
/// Logo REST malzeme, stok ve fiyat entegrasyonu.
/// Logo surumune gore desteklenen parametreler degistigi icin
/// kademeli deneme (fields+q → q → sade) uygulanir.
/// </summary>
public sealed class LogoProductService(
    LogoRestClient client,
    ILogger<LogoProductService> logger) : ILogoProductService
{
    private static readonly int[] AllowedCardTypes = [1, 12];
    private const int LogoMaxPageSize = 25;

    private enum QueryMode { FieldsAndFilter, FilterOnly, Plain }
    private QueryMode? _mode;

    private const string ItemFields =
        "INTERNAL_REFERENCE,CODE,NAME,CARD_TYPE,RECORD_STATUS,VAT,GROUP_CODE," +
        "SPECIAL_CODE,UNIT_CODE,AUXIL_CODE,MARKREF,UNITWEIGHT";

    private static string CardTypeFilter =>
        string.Join(" or ", AllowedCardTypes.Select(t => $"CARD_TYPE eq {t}"));

    public async Task<int> GetItemCountAsync(
        string restUrl, string accessToken, CancellationToken ct = default)
    {
        try
        {
            var url  = $"{restUrl.TrimEnd('/')}/api/v1/items?limit=1&withCount=true";
            var json = await client.GetAsync(url, accessToken, ct);
            var obj  = JObject.Parse(json);
            return obj.Value<int?>("totalCount") ?? obj.Value<int?>("total") ?? 0;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Logo toplam kayit sayisi alinamadi");
            return 0;
        }
    }

    public async Task<List<LogoItemDto>> FetchItemsAsync(
        string restUrl, string accessToken,
        int firmNo, int offset = 0, int limit = 100,
        CancellationToken ct = default)
    {
        var result   = new List<LogoItemDto>();
        var current  = offset;
        var received = 0;

        while (received < limit)
        {
            ct.ThrowIfCancellationRequested();
            var take = Math.Min(LogoMaxPageSize, limit - received);

            var (ok, rawCount, items) =
                await FetchPageAsync(restUrl, accessToken, current, take, ct);

            if (!ok) break;

            result.AddRange(items);
            received += rawCount;
            current  += rawCount;

            if (rawCount < take || rawCount == 0) break;
        }

        logger.LogInformation(
            "Logo items: {Valid} gecerli (offset={Offset}, mod={Mode})",
            result.Count, offset, _mode);
        return result;
    }

    private async Task<(bool Ok, int RawCount, List<LogoItemDto> Items)> FetchPageAsync(
        string restUrl, string accessToken, int offset, int take, CancellationToken ct)
    {
        var modes = _mode.HasValue
            ? new[] { _mode.Value }
            : [QueryMode.FieldsAndFilter, QueryMode.FilterOnly, QueryMode.Plain];

        Exception? lastError = null;

        foreach (var mode in modes)
        {
            var url = BuildUrl(restUrl, offset, take, mode);
            try
            {
                var raw = await client.GetAsync(url, accessToken, ct);
                if (string.IsNullOrWhiteSpace(raw)) return (false, 0, []);

                var arr = ExtractArray(raw, out var apiError);
                if (apiError is not null) throw new InvalidOperationException(apiError);

                var items = new List<LogoItemDto>();
                foreach (var t in arr)
                {
                    var item = ParseItem(t, logger);
                    if (item is not null) items.Add(item);
                }

                if (_mode != mode)
                {
                    _mode = mode;
                    logger.LogInformation("Logo sorgu bicimi: {Mode}", mode);
                }
                return (true, arr.Count, items);
            }
            catch (Exception ex)
            {
                lastError = ex;
                logger.LogWarning("Logo {Mode} basarisiz: {Msg}", mode, ex.Message);
            }
        }

        throw new InvalidOperationException(
            $"Logo REST'ten veri alinamadi. Son hata: {lastError?.Message}", lastError);
    }

    private static string BuildUrl(string restUrl, int offset, int take, QueryMode mode)
    {
        var b = $"{restUrl.TrimEnd('/')}/api/v1/items?offset={offset}&limit={take}";
        return mode switch
        {
            QueryMode.FieldsAndFilter =>
                b + $"&fields={Uri.EscapeDataString(ItemFields)}&q={Uri.EscapeDataString(CardTypeFilter)}",
            QueryMode.FilterOnly => b + $"&q={Uri.EscapeDataString(CardTypeFilter)}",
            _ => b,
        };
    }

    public async Task<LogoItemDto?> FetchItemByRefAsync(
        string restUrl, string accessToken, long itemRef, CancellationToken ct = default)
    {
        try
        {
            var url  = $"{restUrl.TrimEnd('/')}/api/v1/items/{itemRef}";
            var json = await client.GetAsync(url, accessToken, ct);
            return ParseItem(JObject.Parse(json), logger, skipFilters: true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Logo item {Ref} okunamadi", itemRef);
            return null;
        }
    }

    public async Task<Dictionary<long, decimal>> FetchStockAsync(
        string restUrl, string accessToken,
        int firmNo, int periodNo, CancellationToken ct = default)
    {
        var map    = new Dictionary<long, decimal>();
        var firm   = firmNo.ToString("D3");
        var period = periodNo.ToString("D2");

        var queries = new[]
        {
            $"SELECT STOCKREF, SUM(ONHAND) AS ONHAND FROM LV_{firm}_{period}_STINVTOT " +
            $"WHERE INVENNO = -1 GROUP BY STOCKREF",
            $"SELECT STOCKREF, SUM(ONHAND) AS ONHAND FROM LV_{firm}_{period}_STINVTOT " +
            $"GROUP BY STOCKREF",
        };

        foreach (var sql in queries)
        {
            try
            {
                var url  = $"{restUrl.TrimEnd('/')}/api/v1/queries?tsql={Uri.EscapeDataString(sql)}";
                var json = await client.GetAsync(url, accessToken, ct);
                var arr  = ExtractArray(json, out var apiError);
                if (apiError is not null || arr.Count == 0) continue;

                foreach (var row in arr)
                {
                    var refId = row.Value<long?>("STOCKREF") ?? 0;
                    if (refId == 0) continue;
                    map[refId] = row.Value<decimal?>("ONHAND") ?? 0;
                }

                logger.LogInformation("Logo stok: {Count} malzeme", map.Count);
                return map;
            }
            catch (Exception ex) { logger.LogDebug(ex, "Stok sorgusu basarisiz"); }
        }

        logger.LogWarning("Stok alinamadi — Logo REST SQL yetkisi kapali olabilir.");
        return map;
    }

    public async Task<List<LogoItemPriceDto>> FetchSalesPricesAsync(
        string restUrl, string accessToken, CancellationToken ct = default)
    {
        var list   = new List<LogoItemPriceDto>();
        var offset = 0;
        const int take = 25;

        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var url = $"{restUrl.TrimEnd('/')}/api/v1/salesItemPrices?offset={offset}&limit={take}";

            string raw;
            try { raw = await client.GetAsync(url, accessToken, ct); }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Fiyat kartlari alinamadi (offset={Offset})", offset);
                break;
            }

            if (string.IsNullOrWhiteSpace(raw)) break;

            JArray arr;
            try
            {
                arr = ExtractArray(raw, out var apiError);
                if (apiError is not null)
                {
                    logger.LogWarning("Fiyat karti hatasi: {Msg}", apiError);
                    break;
                }
            }
            catch { break; }

            if (arr.Count == 0) break;

            foreach (var t in arr)
            {
                var p = ParsePrice(t);
                if (p is not null && p.ItemRef > 0) list.Add(p);
            }

            offset += arr.Count;
            if (arr.Count < take) break;
        }

        logger.LogInformation("Logo fiyat kartlari: {Count} kayit", list.Count);
        return list;
    }

    private static LogoItemPriceDto? ParsePrice(JToken t)
    {
        try
        {
            var itemRef = t.Value<long?>("CARDREF")
                       ?? t.Value<long?>("CARD_REFERENCE")
                       ?? t.Value<long?>("ITEMREF")
                       ?? t.Value<long?>("ITEM_REFERENCE")
                       ?? t.Value<long?>("MASTER_REFERENCE") ?? 0;
            if (itemRef == 0) return null;

            DateTime? D(params string[] keys)
            {
                foreach (var k in keys)
                {
                    var v = t.Value<DateTime?>(k);
                    if (v is not null && v.Value.Year >= 1950) return v;
                }
                return null;
            }

            string? S(params string[] keys)
            {
                foreach (var k in keys)
                {
                    var v = t.Value<string>(k);
                    if (!string.IsNullOrWhiteSpace(v)) return v.Trim();
                }
                return null;
            }

            return new LogoItemPriceDto(
                ItemRef:          itemRef,
                ItemCode:         (t.Value<string>("CODE") ?? "").Trim(),
                Price:            t.Value<decimal?>("PRICE") ?? t.Value<decimal?>("UNIT_PRICE") ?? 0,
                VatRate:          t.Value<decimal?>("VAT") ?? 0,
                CurrencyCode:     S("CURRENCY", "CURR_CODE"),
                PriceListRef:     t.Value<int?>("PRIORITY") ?? t.Value<int?>("INTERNAL_REFERENCE") ?? 0,
                BeginDate:        D("BEGDATE", "BEGIN_DATE", "BEGINNING_DATE"),
                EndDate:          D("ENDDATE", "END_DATE", "ENDING_DATE"),
                ProjectCode:      S("PROJECT_CODE", "PROJECTCODE", "PRJCODE"),
                TradingGroupCode: S("TRADING_GROUP", "TRADINGGRP", "TRADING_GROUP_CODE", "TRGRPCODE"),
                CostCenterCode:   S("COST_CENTER", "OHPCODE", "COSTCENTER_CODE", "CCENTERCODE"));
        }
        catch { return null; }
    }

    private static JArray ExtractArray(string raw, out string? apiError)
    {
        apiError = null;
        if (string.IsNullOrWhiteSpace(raw)) return [];
        var trimmed = raw.TrimStart();
        if (trimmed.StartsWith('[')) return JArray.Parse(raw);

        var obj = JObject.Parse(raw);
        if (obj["Message"] != null)
        {
            var msg   = obj["Message"]!.ToString();
            var state = obj["ModelState"]?.ToString();
            apiError  = state is null ? msg : $"{msg} {state}";
            return [];
        }
        return obj["items"] as JArray ?? obj["Items"] as JArray
            ?? obj["value"] as JArray ?? obj["data"] as JArray ?? [];
    }

    private static LogoItemDto? ParseItem(JToken t, ILogger logger, bool skipFilters = false)
    {
        try
        {
            var reference = t.Value<long?>("INTERNAL_REFERENCE")
                         ?? t.Value<long?>("LOGICALREF") ?? 0;
            if (reference == 0) return null;

            var cardType = t.Value<int?>("CARD_TYPE") ?? t.Value<int?>("CARDTYPE") ?? 0;

            if (!skipFilters)
            {
                if (!AllowedCardTypes.Contains(cardType)) return null;
                var active = t.Value<int?>("ACTIVE") ?? t.Value<int?>("RECORD_STATUS") ?? 0;
                if (active != 0) return null;
            }

            var code = (t.Value<string>("CODE") ?? "").Trim();
            if (string.IsNullOrWhiteSpace(code)) return null;

            var name = (t.Value<string>("NAME") ?? t.Value<string>("DESCRIPTION") ?? "").Trim();

            return new LogoItemDto(
                LogicalRef:  reference,
                Code:        code,
                Name:        string.IsNullOrWhiteSpace(name) ? code : name,
                AuxDesc:     t.Value<string>("AUXIL_CODE"),
                Description: t.Value<string>("SPECIAL_DESC"),
                GroupCode:   t.Value<string>("GROUP_CODE") ?? t.Value<string>("STGRPCODE"),
                Specode:     t.Value<string>("SPECIAL_CODE") ?? t.Value<string>("SPECODE"),
                UnitCode:    t.Value<string>("UNIT_CODE") ?? t.Value<string>("UNITSETCODE"),
                MarkRef:     t.Value<long?>("MARKREF"),
                CardType:    cardType,
                SellPrice:   t.Value<decimal?>("SELLPRICE") ?? 0,
                SellPrice2:  t.Value<decimal?>("SELLPRICE2") ?? 0,
                VatRate:     t.Value<decimal?>("VAT") ?? 0,
                Stock:       t.Value<decimal?>("ONHAND") ?? 0,
                Weight:      t.Value<decimal?>("UNITWEIGHT") ?? 0);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Logo kayit ayristirilamadi");
            return null;
        }
    }
}
