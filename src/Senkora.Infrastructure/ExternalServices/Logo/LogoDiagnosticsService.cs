using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Senkora.Application.Common.Interfaces;
using Senkora.Application.Features.Products.Queries;

namespace Senkora.Infrastructure.ExternalServices.Logo;

/// <summary>
/// Logo REST'in hangi sorgu bicimlerini destekledigini tespit eder,
/// ham yanitlari ve alan adlarini gosterir.
/// </summary>
public sealed class LogoDiagnosticsService(
    LogoRestClient client,
    ILogger<LogoDiagnosticsService> logger) : ILogoDiagnosticsService
{
    private const string CardTypeFilter = "CARD_TYPE eq 1 or CARD_TYPE eq 12";
    private const string ItemFields =
        "INTERNAL_REFERENCE,CODE,NAME,CARD_TYPE,RECORD_STATUS,VAT,GROUP_CODE,UNITWEIGHT";

    public async Task<LogoFetchDiagnostics> ProbeAsync(
        string restUrl, string accessToken,
        int firmNo, int periodNo, int limit,
        CancellationToken ct = default)
    {
        var baseUrl = restUrl.TrimEnd('/');

        // ── 1. Malzeme kartlari: hangi sorgu bicimi calisiyor? ───────────────
        var attempts = new (string Label, string Url)[]
        {
            ("fields + q",
             $"{baseUrl}/api/v1/items?offset=0&limit={limit}" +
             $"&fields={Uri.EscapeDataString(ItemFields)}" +
             $"&q={Uri.EscapeDataString(CardTypeFilter)}"),

            ("sadece q",
             $"{baseUrl}/api/v1/items?offset=0&limit={limit}" +
             $"&q={Uri.EscapeDataString(CardTypeFilter)}"),

            ("sade",
             $"{baseUrl}/api/v1/items?offset=0&limit={limit}"),
        };

        string? usedUrl   = null, rawItems = null, firstItem = null;
        string? itemError = null, itemStage = null;
        var     itemCount = 0;
        var     modeLabel = "";

        foreach (var (label, url) in attempts)
        {
            try
            {
                var raw = await client.GetAsync(url, accessToken, ct);
                var arr = ExtractArray(raw, out var apiErr);

                if (apiErr is not null)
                {
                    itemError = $"[{label}] {apiErr}";
                    itemStage = "LOGO_API_ERROR";
                    continue;
                }

                usedUrl = url; rawItems = raw; modeLabel = label;
                itemCount = arr.Count;
                firstItem = Trunc(arr.FirstOrDefault()?.ToString(), 2500);
                itemError = null; itemStage = null;
                break;
            }
            catch (Exception ex)
            {
                itemError = $"[{label}] {ex.Message}";
                itemStage = "HTTP_REQUEST";
                logger.LogWarning("Diagnostics {Label} basarisiz: {Msg}", label, ex.Message);
            }
        }

        if (usedUrl is null)
        {
            return new LogoFetchDiagnostics(
                true, null, attempts[0].Url, false, null, 0, null,
                itemError ?? "Hicbir sorgu bicimi calismadi.", itemStage ?? "ALL_FAILED");
        }

        // ── 2. Fiyat kartlari ────────────────────────────────────────────────
        var priceUrl = $"{baseUrl}/api/v1/salesItemPrices?offset=0&limit={limit}";
        bool    priceOk    = false;
        var     priceCount = 0;
        string? firstPrice = null, priceError = null;

        try
        {
            var rawPrice = await client.GetAsync(priceUrl, accessToken, ct);
            var arr = ExtractArray(rawPrice, out var apiErr);
            if (apiErr is not null) priceError = apiErr;
            else
            {
                priceOk    = true;
                priceCount = arr.Count;
                firstPrice = Trunc(arr.FirstOrDefault()?.ToString(), 1800);
                if (arr.Count == 0)
                    priceError = "Fiyat karti gelmedi. Logo'da satis fiyat karti tanimli mi?";
            }
        }
        catch (Exception ex) { priceError = ex.Message; }

        // ── 3. Stok sorgusu (SQL yetkisi var mi?) ────────────────────────────
        var firm   = firmNo.ToString("D3");
        var period = periodNo.ToString("D2");
        var stockSql = $"SELECT TOP 5 STOCKREF, ONHAND FROM LG_{firm}_{period}_STINVTOT";

        bool    stockOk    = false;
        var     stockCount = 0;
        string? stockError = null;

        try
        {
            var url  = $"{baseUrl}/api/v1/queries?tsql={Uri.EscapeDataString(stockSql)}";
            var json = await client.GetAsync(url, accessToken, ct);
            var arr  = ExtractArray(json, out var apiErr);

            if (apiErr is not null)
                stockError = $"SQL reddedildi: {apiErr}";
            else
            {
                stockOk    = true;
                stockCount = arr.Count;
                if (arr.Count == 0)
                    stockError = $"LV_{firm}_{period}_STINVTOT bos veya erisilemedi.";
            }
        }
        catch (Exception ex)
        {
            stockError = $"SQL sorgu servisi calismadi: {ex.Message}";
        }

        return new LogoFetchDiagnostics(
            TokenObtained:      true,
            TokenPreview:       null,
            RequestUrl:         $"[Calisan bicim: {modeLabel}]  {usedUrl}",
            RequestSucceeded:   true,
            RawResponsePreview: Trunc(rawItems, 1500),
            ParsedItemCount:    itemCount,
            FirstItemJson:      firstItem,
            ErrorMessage:       itemCount == 0 ? "Yanit alindi ancak kayit gelmedi." : null,
            ErrorStage:         itemCount == 0 ? "PARSE_EMPTY" : null,
            PriceRequestUrl:    priceUrl,
            PriceRequestOk:     priceOk,
            PriceRecordCount:   priceCount,
            FirstPriceJson:     firstPrice,
            PriceErrorMessage:  priceError,
            StockQueryOk:       stockOk,
            StockRecordCount:   stockCount,
            StockErrorMessage:  stockError);
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

    private static string? Trunc(string? s, int max)
        => s is null ? null : s.Length > max ? s[..max] + " ...[kesildi]" : s;
}
