using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Senkora.Application.Common.Interfaces;

namespace Senkora.Infrastructure.ExternalServices.Logo;

/// <summary>
/// Logo REST SQL servisi.
///
/// Logo surumleri arasinda endpoint ve parametre adi degisiyor.
/// Bilinen bicimler sirayla denenir, calisan bicim hafizaya alinir.
/// </summary>
public sealed class LogoSqlService(
    LogoRestClient client,
    ILogger<LogoSqlService> logger) : ILogoSqlService
{
    /// <summary>Ilk basarili bicim — sonraki cagrilarda dogrudan kullanilir</summary>
    private static int? _workingVariant;

    private sealed record Variant(
        string Name,
        Func<string, string, string> BuildUrl,           // (baseUrl, sql) => url
        Func<string, object?>        BuildBody,          // sql => body (null = GET)
        bool   IsPost);

    private static readonly Variant[] Variants =
    [
        // ── GET varyantlari ──────────────────────────────────────────────────
        new("GET ?query=",
            (b, s) => $"{b}/api/v1/queries?query={Uri.EscapeDataString(s)}",
            _ => null, false),

        new("GET ?tsql=",
            (b, s) => $"{b}/api/v1/queries?tsql={Uri.EscapeDataString(s)}",
            _ => null, false),

        new("GET ?sql=",
            (b, s) => $"{b}/api/v1/queries?sql={Uri.EscapeDataString(s)}",
            _ => null, false),

        new("GET unsafe?query=",
            (b, s) => $"{b}/api/v1/queries/unsafe?query={Uri.EscapeDataString(s)}",
            _ => null, false),

        // ── POST varyantlari ─────────────────────────────────────────────────
        new("POST unsafe (ham string)",
            (b, _) => $"{b}/api/v1/queries/unsafe?cmdTimeout=60",
            s => JsonConvert.SerializeObject(s), true),

        new("POST unsafe {query}",
            (b, _) => $"{b}/api/v1/queries/unsafe?cmdTimeout=60",
            s => new { query = s }, true),

        new("POST unsafe {tsql}",
            (b, _) => $"{b}/api/v1/queries/unsafe?cmdTimeout=60",
            s => new { tsql = s }, true),

        new("POST queries {query}",
            (b, _) => $"{b}/api/v1/queries",
            s => new { query = s }, true),

        new("POST queries (ham string)",
            (b, _) => $"{b}/api/v1/queries",
            s => JsonConvert.SerializeObject(s), true),
    ];

    public async Task<LogoSqlResult> QueryAsync(
        string restUrl, string accessToken, string sql,
        int timeoutSeconds = 60, CancellationToken ct = default)
    {
        var baseUrl = restUrl.TrimEnd('/');

        // Calisan bicim biliniyorsa once onu dene
        var order = _workingVariant.HasValue
            ? Enumerable.Range(0, Variants.Length)
                .OrderBy(i => i == _workingVariant.Value ? 0 : 1).ToArray()
            : Enumerable.Range(0, Variants.Length).ToArray();

        string? lastError = null;

        foreach (var idx in order)
        {
            var v = Variants[idx];
            ct.ThrowIfCancellationRequested();

            try
            {
                var url  = v.BuildUrl(baseUrl, sql);
                var body = v.BuildBody(sql);

                var json = v.IsPost
                    ? await client.PostRawAsync(url, body, accessToken, ct)
                    : await client.GetAsync(url, accessToken, ct);

                // Logo hata JSON'u dondurmus olabilir
                if (LooksLikeError(json, out var apiErr))
                {
                    lastError = $"[{v.Name}] {apiErr}";
                    continue;
                }

                if (_workingVariant != idx)
                {
                    _workingVariant = idx;
                    logger.LogInformation("Logo SQL bicimi belirlendi: {Variant}", v.Name);
                }

                return new LogoSqlResult(true, json, v.Name, null);
            }
            catch (Exception ex)
            {
                lastError = $"[{v.Name}] {Short(ex.Message)}";
            }
        }

        logger.LogWarning("Logo SQL calistirilamadi. Son hata: {Error}", lastError);
        return new LogoSqlResult(false, null, null, lastError);
    }

    public async Task<List<LogoSqlProbe>> ProbeAllAsync(
        string restUrl, string accessToken, string sql,
        CancellationToken ct = default)
    {
        var baseUrl = restUrl.TrimEnd('/');
        var results = new List<LogoSqlProbe>();

        foreach (var v in Variants)
        {
            ct.ThrowIfCancellationRequested();
            var url = v.BuildUrl(baseUrl, sql);

            try
            {
                var body = v.BuildBody(sql);
                var json = v.IsPost
                    ? await client.PostRawAsync(url, body, accessToken, ct)
                    : await client.GetAsync(url, accessToken, ct);

                if (LooksLikeError(json, out var apiErr))
                {
                    results.Add(new LogoSqlProbe(v.Name, url, false, 0, apiErr, null));
                    continue;
                }

                var arr = ExtractArray(json);
                results.Add(new LogoSqlProbe(
                    v.Name, url, true, arr.Count, null,
                    Short(arr.FirstOrDefault()?.ToString() ?? json, 400)));
            }
            catch (Exception ex)
            {
                results.Add(new LogoSqlProbe(v.Name, url, false, 0, Short(ex.Message, 300), null));
            }
        }

        return results;
    }

    private static bool LooksLikeError(string json, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(json)) { error = "Bos yanit"; return true; }

        var trimmed = json.TrimStart();
        if (trimmed.StartsWith('[')) return false;

        try
        {
            var obj = JObject.Parse(json);
            if (obj["Message"] != null)
            {
                var msg   = obj["Message"]!.ToString();
                var state = obj["ModelState"]?.ToString(Formatting.None);
                error = state is null ? msg : $"{msg} {state}";
                return true;
            }
        }
        catch { /* dizi disi bir sey; hata degil */ }

        return false;
    }

    public static JArray ExtractArray(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return [];
        var trimmed = raw.TrimStart();
        if (trimmed.StartsWith('[')) return JArray.Parse(raw);

        var obj = JObject.Parse(raw);
        return obj["items"] as JArray ?? obj["Items"] as JArray
            ?? obj["value"] as JArray ?? obj["data"] as JArray
            ?? obj["result"] as JArray ?? obj["Result"] as JArray ?? [];
    }

    private static string Short(string s, int max = 200)
        => s.Length > max ? s[..max] + "..." : s;
}
