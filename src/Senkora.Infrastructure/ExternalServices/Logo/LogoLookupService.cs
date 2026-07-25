using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Senkora.Application.Common.Interfaces;

namespace Senkora.Infrastructure.ExternalServices.Logo;

/// <summary>
/// Logo secim listeleri (fiyat kriterleri icin).
///
///   Projeler        → /api/v1/projects          (REST)
///   Masraf merkezi  → /api/v1/overheadAccounts  (REST)
///   Ticari islem gr → L_TRADGRP tablosu, SQL (queries) ile
///                     REST endpoint'i yoktur.
/// </summary>
public sealed class LogoLookupService(
    LogoRestClient client,
    ILogger<LogoLookupService> logger) : ILogoLookupService
{
    public async Task<LogoLookupResult> GetAllAsync(
        string restUrl, string accessToken, int firmNo, CancellationToken ct = default)
    {
        var projects = await FetchRestAsync(restUrl, accessToken, "projects", "Projeler", ct);
        var costs    = await FetchRestAsync(restUrl, accessToken, "overheadAccounts",
                                            "Masraf Merkezleri", ct);
        var trading  = await FetchTradingGroupsAsync(restUrl, accessToken, firmNo, ct);

        return new LogoLookupResult(projects, trading, costs);
    }

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

    private async Task<LogoLookupSet> FetchTradingGroupsAsync(
        string restUrl, string accessToken, int firmNo, CancellationToken ct)
    {
        var firm = firmNo.ToString("D3");

        // L_TRADGRP firma bagimsiz ortak tablodur — once o denenir.
        var attempts = new (string Table, string Sql)[]
        {
            ("L_TRADGRP",
             "SELECT CODE, DEFINITION_ FROM L_TRADGRP ORDER BY CODE"),

            ($"LG_{firm}_TRADGRP",
             $"SELECT CODE, DEFINITION_ FROM LG_{firm}_TRADGRP ORDER BY CODE"),

            ($"LG_{firm}_TRADINGGRP",
             $"SELECT CODE, DEFINITION_ FROM LG_{firm}_TRADINGGRP ORDER BY CODE"),
        };

        var errors = new List<string>();

        foreach (var (table, sql) in attempts)
        {
            try
            {
                var url  = $"{restUrl.TrimEnd('/')}/api/v1/queries?tsql={Uri.EscapeDataString(sql)}";
                var json = await client.GetAsync(url, accessToken, ct);

                var arr = Extract(json, out var apiErr);
                if (apiErr is not null) { errors.Add($"{table}: {Short(apiErr)}"); continue; }
                if (arr.Count == 0)     { errors.Add($"{table}: bos"); continue; }

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
                    logger.LogInformation(
                        "Ticari islem gruplari: {Count} kayit ({Table})", list.Count, table);
                    return new LogoLookupSet(list, $"SQL: {table}", null);
                }

                errors.Add($"{table}: gecerli kod yok");
            }
            catch (Exception ex) { errors.Add($"{table}: {Short(ex.Message)}"); }
        }

        var detail = errors.Count > 0 ? " Denenen: " + string.Join(" | ", errors) : "";
        logger.LogWarning("Ticari islem grubu alinamadi.{Detail}", detail);

        return new LogoLookupSet([], "SQL (queries)",
            "Ticari islem grubu alinamadi. Logo REST'te SQL sorgu yetkisi kapali " +
            "olabilir — kodu elle yazabilirsiniz." + detail);
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

    private static string Short(string s) => s.Length > 160 ? s[..160] + "..." : s;
}
