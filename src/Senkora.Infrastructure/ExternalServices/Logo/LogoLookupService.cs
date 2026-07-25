using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Senkora.Application.Common.Interfaces;

namespace Senkora.Infrastructure.ExternalServices.Logo;

/// <summary>
/// Logo secim listeleri (fiyat kriterleri icin).
///   Projeler       → REST /api/v1/projects
///   Masraf merkezi → REST /api/v1/overheadAccounts
///   Ticari islem gr→ REST endpoint yok; tablo adi kesfedilip SQL ile okunur
/// </summary>
public sealed class LogoLookupService(
    LogoRestClient client,
    ILogoSqlService sql,
    ILogoSchemaService schema,
    ILogger<LogoLookupService> logger) : ILogoLookupService
{
    public async Task<LogoLookupResult> GetAllAsync(
        string restUrl, string accessToken, int firmNo, CancellationToken ct = default)
    {
        var firm = firmNo.ToString("D3");

        // Projeler — once REST, bos donerse SQL
        var projects = await FetchRestAsync(restUrl, accessToken, "projects", "Projeler", ct);
        if (projects.Items.Count == 0)
            projects = await FetchViaSqlAsync(restUrl, accessToken, "Projeler",
                [$"LG_{firm}_PROJECT", "L_PROJECT", $"LG_{firm}_PROJECTS"], ct)
                ?? projects;

        // Masraf merkezleri
        var costs = await FetchRestAsync(restUrl, accessToken, "overheadAccounts",
                                         "Masraf Merkezleri", ct);
        if (costs.Items.Count == 0)
            costs = await FetchViaSqlAsync(restUrl, accessToken, "Masraf Merkezleri",
                [$"LG_{firm}_EMCENTER", $"LG_{firm}_OHPCODE", "L_EMCENTER"], ct)
                ?? costs;

        // Ticari islem grubu — REST endpoint'i yok
        var trading = await FetchTradingGroupsAsync(restUrl, accessToken, firmNo, ct);

        return new LogoLookupResult(projects, trading, costs);
    }

    /// <summary>Verilen tablo adaylarindan ilk dolu olani SQL ile okur.</summary>
    private async Task<LogoLookupSet?> FetchViaSqlAsync(
        string restUrl, string accessToken, string label,
        string[] candidates, CancellationToken ct)
    {
        foreach (var table in candidates)
        {
            try
            {
                var q   = $"SELECT CODE, DEFINITION_ FROM {table} ORDER BY CODE";
                var res = await sql.QueryAsync(restUrl, accessToken, q, 60, ct);
                if (!res.Success || res.RawJson is null) continue;

                var arr  = LogoSqlService.ExtractArray(res.RawJson);
                var list = arr
                    .Select(t => new LogoLookupItem(
                        (t.Value<string>("CODE") ?? "").Trim(),
                        (t.Value<string>("DEFINITION_") ?? "").Trim()))
                    .Where(x => !string.IsNullOrWhiteSpace(x.Code))
                    .DistinctBy(x => x.Code)
                    .OrderBy(x => x.Code)
                    .ToList();

                if (list.Count > 0)
                {
                    logger.LogInformation("{Label}: {Count} kayit ({Table})",
                        label, list.Count, table);
                    return new LogoLookupSet(list, $"SQL: {table}", null);
                }
            }
            catch { /* sonraki aday */ }
        }

        return null;
    }

    // ── REST listeleri ────────────────────────────────────────────────────────
    private async Task<LogoLookupSet> FetchRestAsync(
        string restUrl, string accessToken, string endpoint,
        string label, CancellationToken ct)
    {
        var result = new List<LogoLookupItem>();
        var offset = 0;
        const int take = 25;

        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var url = $"{restUrl.TrimEnd('/')}/api/v1/{endpoint}?offset={offset}&limit={take}";

            string json;
            try { json = await client.GetAsync(url, accessToken, ct); }
            catch (Exception ex)
            {
                if (offset == 0)
                {
                    logger.LogWarning(ex, "{Label} alinamadi", label);
                    return new LogoLookupSet([], $"/api/v1/{endpoint}",
                        $"{label} alinamadi: {Short(ex.Message)}");
                }
                break;
            }

            JArray arr;
            try
            {
                arr = Extract(json, out var apiErr);
                if (apiErr is not null)
                {
                    if (offset == 0)
                        return new LogoLookupSet([], $"/api/v1/{endpoint}",
                            $"{label}: {Short(apiErr)}");
                    break;
                }
            }
            catch { break; }

            if (arr.Count == 0) break;

            foreach (var t in arr)
            {
                var code = (t.Value<string>("CODE") ?? "").Trim();
                if (string.IsNullOrWhiteSpace(code)) continue;
                var name = (t.Value<string>("DEFINITION_")
                         ?? t.Value<string>("NAME")
                         ?? t.Value<string>("DESCRIPTION") ?? "").Trim();
                result.Add(new LogoLookupItem(code, name));
            }

            offset += arr.Count;
            if (arr.Count < take || result.Count > 3000) break;
        }

        var list = result.DistinctBy(x => x.Code).OrderBy(x => x.Code).ToList();
        logger.LogInformation("{Label}: {Count} kayit", label, list.Count);

        return new LogoLookupSet(list, $"/api/v1/{endpoint}",
            list.Count == 0 ? $"{label} listesi bos. Logo'da tanim var mi?" : null);
    }

    // ── Ticari islem grubu ────────────────────────────────────────────────────
    private async Task<LogoLookupSet> FetchTradingGroupsAsync(
        string restUrl, string accessToken, int firmNo, CancellationToken ct)
    {
        var table = await schema.ResolveTradingGroupTableAsync(restUrl, accessToken, firmNo, ct);

        if (table is null)
        {
            // Son care: fiyat kartlarindaki TRADINGGRP degerlerinden turet
            var derived = await DeriveFromPriceCardsAsync(restUrl, accessToken, firmNo, ct);
            if (derived.Count > 0)
                return new LogoLookupSet(derived, "Fiyat kartlarindan turetildi",
                    "Ticari islem grubu tanim tablosu bulunamadi; " +
                    "listede yalnizca fiyat kartlarinda kullanilan kodlar var.");

            return new LogoLookupSet([], "SQL",
                "Ticari islem grubu tablosu bulunamadi. Kodu elle yazabilirsiniz.");
        }

        var query = $"SELECT CODE, DEFINITION_ FROM {table} ORDER BY CODE";
        var res   = await sql.QueryAsync(restUrl, accessToken, query, 60, ct);

        if (!res.Success || res.RawJson is null)
            return new LogoLookupSet([], $"SQL: {table}",
                $"Sorgu calistirilamadi: {Short(res.Error ?? "bilinmeyen hata")}");

        var arr  = LogoSqlService.ExtractArray(res.RawJson);
        var list = arr
            .Select(t => new LogoLookupItem(
                (t.Value<string>("CODE") ?? "").Trim(),
                (t.Value<string>("DEFINITION_") ?? "").Trim()))
            .Where(x => !string.IsNullOrWhiteSpace(x.Code))
            .DistinctBy(x => x.Code)
            .OrderBy(x => x.Code)
            .ToList();

        logger.LogInformation("Ticari islem gruplari: {Count} kayit ({Table})", list.Count, table);

        return new LogoLookupSet(list, $"SQL: {table}",
            list.Count == 0 ? $"{table} tablosu bos." : null);
    }

    /// <summary>Tanim tablosu yoksa fiyat kartlarinda kullanilan kodlari topla.</summary>
    private async Task<List<LogoLookupItem>> DeriveFromPriceCardsAsync(
        string restUrl, string accessToken, int firmNo, CancellationToken ct)
    {
        var firm = firmNo.ToString("D3");
        var queries = new[]
        {
            $"SELECT DISTINCT TRADINGGRP AS CODE FROM LG_{firm}_PRCLIST " +
            $"WHERE TRADINGGRP <> '' ORDER BY TRADINGGRP",

            $"SELECT DISTINCT TRADINGGRP AS CODE FROM LG_{firm}_01_INVOICE " +
            $"WHERE TRADINGGRP <> '' ORDER BY TRADINGGRP",
        };

        foreach (var q in queries)
        {
            try
            {
                var res = await sql.QueryAsync(restUrl, accessToken, q, 60, ct);
                if (!res.Success || res.RawJson is null) continue;

                var arr = LogoSqlService.ExtractArray(res.RawJson);
                var list = arr
                    .Select(t => (t.Value<string>("CODE") ?? "").Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .OrderBy(x => x)
                    .Select(x => new LogoLookupItem(x, ""))
                    .ToList();

                if (list.Count > 0) return list;
            }
            catch { /* sonraki sorgu */ }
        }

        return [];
    }

    private static JArray Extract(string raw, out string? apiError)
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

    private static string Short(string s) => s.Length > 200 ? s[..200] + "..." : s;
}
