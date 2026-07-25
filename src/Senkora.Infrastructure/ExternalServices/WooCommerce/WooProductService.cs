using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Senkora.Application.Common.Interfaces;

namespace Senkora.Infrastructure.ExternalServices.WooCommerce;

public sealed class WooProductService(
    IHttpClientFactory httpClientFactory,
    ILogger<WooProductService> logger) : IWooProductService
{
    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new SnakeCaseNamingStrategy()
        },
        NullValueHandling = NullValueHandling.Ignore
    };

    public async Task<long> CreateProductAsync(
        string storeUrl, string ck, string cs,
        WooProductPayload payload, CancellationToken ct = default)
    {
        var url  = $"{storeUrl.TrimEnd('/')}/wp-json/wc/v3/products";
        var json = JsonConvert.SerializeObject(payload, JsonSettings);
        var resp = await SendAsync(HttpMethod.Post, url, ck, cs, json, ct);
        var obj  = JObject.Parse(resp);
        return obj.Value<long>("id");
    }

    public async Task UpdateProductAsync(
        string storeUrl, string ck, string cs,
        long productId, WooProductPayload payload, CancellationToken ct = default)
    {
        var url  = $"{storeUrl.TrimEnd('/')}/wp-json/wc/v3/products/{productId}";
        var json = JsonConvert.SerializeObject(payload, JsonSettings);
        await SendAsync(HttpMethod.Put, url, ck, cs, json, ct);
    }

    public async Task PatchStockAndPriceAsync(
        string storeUrl, string ck, string cs,
        long productId, decimal stock, decimal price, decimal? salePrice,
        CancellationToken ct = default)
    {
        var url = $"{storeUrl.TrimEnd('/')}/wp-json/wc/v3/products/{productId}";
        var patch = new
        {
            regular_price  = price.ToString("F2"),
            sale_price     = salePrice?.ToString("F2"),
            stock_quantity = (int)stock,
            stock_status   = stock > 0 ? "instock" : "outofstock"
        };
        await SendAsync(HttpMethod.Put, url, ck, cs,
            JsonConvert.SerializeObject(patch), ct);
    }

    public async Task<List<WooCategoryDto>> GetCategoriesAsync(
        string storeUrl, string ck, string cs, CancellationToken ct = default)
    {
        var result = new List<WooCategoryDto>();
        var page = 1;
        while (true)
        {
            var url  = $"{storeUrl.TrimEnd('/')}/wp-json/wc/v3/products/categories" +
                       $"?per_page=100&page={page}";
            var json = await SendAsync(HttpMethod.Get, url, ck, cs, null, ct);
            var arr  = JArray.Parse(json);
            if (!arr.Any()) break;

            result.AddRange(arr.Select(c => new WooCategoryDto(
                Id:       c.Value<int>("id"),
                Name:     c.Value<string>("name") ?? "",
                Slug:     c.Value<string>("slug") ?? "",
                ParentId: c.Value<int?>("parent"),
                Count:    c.Value<int>("count"))));
            page++;
            if (arr.Count < 100) break;
        }
        return result;
    }

    public async Task<List<WooShippingClassDto>> GetShippingClassesAsync(
        string storeUrl, string ck, string cs, CancellationToken ct = default)
    {
        var url  = $"{storeUrl.TrimEnd('/')}/wp-json/wc/v3/products/shipping_classes?per_page=100";
        var json = await SendAsync(HttpMethod.Get, url, ck, cs, null, ct);
        var arr  = JArray.Parse(json);
        return arr.Select(s => new WooShippingClassDto(
            Id:   s.Value<int>("id"),
            Name: s.Value<string>("name") ?? "",
            Slug: s.Value<string>("slug") ?? "")).ToList();
    }

    // ── HTTP ─────────────────────────────────────────────────────────────────
    private async Task<string> SendAsync(
        HttpMethod method, string url, string ck, string cs,
        string? body, CancellationToken ct)
    {
        var http    = httpClientFactory.CreateClient("WooCommerce");
        var isHttps = url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

        HttpRequestMessage BuildReq(string reqUrl)
        {
            var req = new HttpRequestMessage(method, reqUrl);
            req.Headers.Add("Accept", "application/json");
            req.Headers.Add("User-Agent", "Senkora/1.0");
            if (body is not null)
                req.Content = new StringContent(body, Encoding.UTF8, "application/json");
            return req;
        }

        HttpResponseMessage resp;
        if (isHttps)
        {
            var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ck}:{cs}"));
            var req   = BuildReq(url);
            req.Headers.Add("Authorization", $"Basic {basic}");
            resp = await http.SendAsync(req, ct);
        }
        else
        {
            // OAuth 1.0 HMAC-SHA256
            var signedUrl = SignOAuth1(method.Method, url, ck, cs, body);
            resp = await http.SendAsync(BuildReq(signedUrl), ct);
        }

        var content = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
        {
            logger.LogWarning("WooCommerce {Method} {Url} → {Status}: {Body}",
                method.Method, url, (int)resp.StatusCode,
                content[..Math.Min(500, content.Length)]);
            throw new HttpRequestException(
                $"WooCommerce API hatası {(int)resp.StatusCode}: " +
                TryGetWooError(content));
        }
        return content;
    }

    private static string TryGetWooError(string body)
    {
        try { return JObject.Parse(body)["message"]?.ToString() ?? body; }
        catch { return body[..Math.Min(200, body.Length)]; }
    }

    private static string SignOAuth1(
        string method, string url, string ck, string cs,
        string? body)
    {
        var uri        = new Uri(url);
        var baseUrl    = $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}";
        var queryParts = uri.Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Split('='))
            .Where(p => p.Length == 2)
            .ToDictionary(p => p[0], p => p[1]);

        var nonce     = Guid.NewGuid().ToString("N");
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

        var allParams = new SortedDictionary<string, string>(queryParts)
        {
            ["oauth_consumer_key"]     = ck,
            ["oauth_nonce"]            = nonce,
            ["oauth_signature_method"] = "HMAC-SHA256",
            ["oauth_timestamp"]        = timestamp,
            ["oauth_version"]          = "1.0"
        };

        var paramStr = string.Join("&", allParams
            .Select(kv =>
                $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}")
            .OrderBy(s => s));

        var baseStr = string.Join("&",
            method.ToUpper(),
            Uri.EscapeDataString(baseUrl),
            Uri.EscapeDataString(paramStr));

        var signingKey = $"{Uri.EscapeDataString(cs)}&";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(signingKey));
        var sig        = Convert.ToBase64String(
            hmac.ComputeHash(Encoding.UTF8.GetBytes(baseStr)));

        allParams["oauth_signature"] = sig;

        var query = string.Join("&", allParams.Select(kv =>
            $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

        return $"{baseUrl}?{query}";
    }
}
