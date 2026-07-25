using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Senkora.Application.Common.Interfaces;

namespace Senkora.Infrastructure.ExternalServices.Logo;

/// <summary>
/// Logo veritabani sema kesfi.
/// Tablo adlari Logo surumune ve kuruluma gore degistigi icin
/// sabit isim varsaymak yerine sys.tables sorgulanir.
/// Bulunan isimler bellekte tutulur.
/// </summary>
public sealed class LogoSchemaService(
    ILogoSqlService sql,
    ILogger<LogoSchemaService> logger) : ILogoSchemaService
{
    private static readonly Dictionary<string, string?> Cache = [];
    private static readonly SemaphoreSlim Lock = new(1, 1);

    public async Task<List<string>> FindTablesAsync(
        string restUrl, string accessToken, string pattern,
        CancellationToken ct = default)
    {
        var safe = pattern.Replace("'", "''");
        var query =
            "SELECT name FROM sys.tables WHERE name LIKE '%" + safe + "%' " +
            "UNION SELECT name FROM sys.views WHERE name LIKE '%" + safe + "%' " +
            "ORDER BY name";

        var res = await sql.QueryAsync(restUrl, accessToken, query, 60, ct);
        if (!res.Success || res.RawJson is null)
        {
            logger.LogWarning("Tablo aramasi basarisiz ({Pattern}): {Error}", pattern, res.Error);
            return [];
        }

        var arr = LogoSqlService.ExtractArray(res.RawJson);
        return arr.Select(t => t.Value<string>("name") ?? "")
                  .Where(n => !string.IsNullOrWhiteSpace(n))
                  .ToList();
    }

    public async Task<string?> ResolveTradingGroupTableAsync(
        string restUrl, string accessToken, int firmNo, CancellationToken ct = default)
    {
        var key = $"TRADGRP_{firmNo}";
        if (Cache.TryGetValue(key, out var cached)) return cached;

        await Lock.WaitAsync(ct);
        try
        {
            if (Cache.TryGetValue(key, out cached)) return cached;

            var firm = firmNo.ToString("D3");

            // Once bilinen adlari dene
            var candidates = new[]
            {
                $"LG_{firm}_TRADGRP", "L_TRADGRP", $"LG_{firm}_TRADINGGRP",
                $"LG_{firm}_TRDGRP",
            };

            foreach (var t in candidates)
            {
                if (await TableHasRowsAsync(restUrl, accessToken, t, ct))
                {
                    Cache[key] = t;
                    logger.LogInformation("Ticari islem grubu tablosu: {Table}", t);
                    return t;
                }
            }

            // Bulunamadiysa sys.tables'ta ara
            var found = await FindTablesAsync(restUrl, accessToken, "TRAD", ct);
            foreach (var t in found)
            {
                if (await TableHasRowsAsync(restUrl, accessToken, t, ct))
                {
                    Cache[key] = t;
                    logger.LogInformation("Ticari islem grubu tablosu bulundu: {Table}", t);
                    return t;
                }
            }

            logger.LogWarning(
                "Ticari islem grubu tablosu bulunamadi. Aranan: {Found}",
                found.Count > 0 ? string.Join(", ", found) : "(sonuc yok)");

            Cache[key] = null;
            return null;
        }
        finally { Lock.Release(); }
    }

    public async Task<string?> ResolveStockTableAsync(
        string restUrl, string accessToken,
        int firmNo, int periodNo, CancellationToken ct = default)
    {
        var key = $"STOCK_{firmNo}_{periodNo}";
        if (Cache.TryGetValue(key, out var cached)) return cached;

        await Lock.WaitAsync(ct);
        try
        {
            if (Cache.TryGetValue(key, out cached)) return cached;

            var firm   = firmNo.ToString("D3");
            var period = periodNo.ToString("D2");

            var candidates = new[]
            {
                $"LV_{firm}_{period}_STINVTOT",
                $"LG_{firm}_{period}_STINVTOT",
                $"LV_{firm}_{period}_STINVENS",
            };

            foreach (var t in candidates)
            {
                if (await TableHasRowsAsync(restUrl, accessToken, t, ct))
                {
                    Cache[key] = t;
                    logger.LogInformation("Stok tablosu: {Table}", t);
                    return t;
                }
            }

            var found = await FindTablesAsync(restUrl, accessToken, "STINVTOT", ct);
            foreach (var t in found)
            {
                if (await TableHasRowsAsync(restUrl, accessToken, t, ct))
                {
                    Cache[key] = t;
                    logger.LogInformation("Stok tablosu bulundu: {Table}", t);
                    return t;
                }
            }

            logger.LogWarning(
                "Stok tablosu bulunamadi. Aranan: {Found}",
                found.Count > 0 ? string.Join(", ", found) : "(sonuc yok)");

            Cache[key] = null;
            return null;
        }
        finally { Lock.Release(); }
    }

    private async Task<bool> TableHasRowsAsync(
        string restUrl, string accessToken, string table, CancellationToken ct)
    {
        try
        {
            var res = await sql.QueryAsync(
                restUrl, accessToken, $"SELECT TOP 1 1 AS ok FROM {table}", 30, ct);
            return res.Success && res.RawJson is not null
                && LogoSqlService.ExtractArray(res.RawJson).Count > 0;
        }
        catch { return false; }
    }
}
