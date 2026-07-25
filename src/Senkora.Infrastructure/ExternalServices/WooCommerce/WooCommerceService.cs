using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Senkora.Application.Common.Interfaces;

namespace Senkora.Infrastructure.ExternalServices.WooCommerce;

public sealed class WooCommerceService(
    IHttpClientFactory httpClientFactory,
    ILogger<WooCommerceService> logger) : IWooCommerceService
{
    public async Task<WooTestResult> TestConnectionAsync(
        string storeUrl, string consumerKey, string consumerSecret,
        CancellationToken ct = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var http    = httpClientFactory.CreateClient("WooCommerce");
            var baseUrl = storeUrl.TrimEnd('/');

            // Query param auth (HTTP icin daha guvenilir)
            // HTTPS ise Basic Auth da calisir, ama query param her ikisinde de calisir
            // 1. Once REST API'nin aktif olup olmadigini kontrol et
            var rootEndpoint = $"{baseUrl}/wp-json/wc/v3" +
                               $"?consumer_key={Uri.EscapeDataString(consumerKey)}" +
                               $"&consumer_secret={Uri.EscapeDataString(consumerSecret)}";

            using var req = new HttpRequestMessage(HttpMethod.Get, rootEndpoint);
            req.Headers.Add("Accept", "application/json");
            req.Headers.Add("User-Agent", "Senkora/1.0");

            var resp = await http.SendAsync(req, ct);

            // Bazi WC siteleri root endpointe 200 yerine 404 verir, products deneyelim
            if (!resp.IsSuccessStatusCode)
            {
                var prodEndpoint = $"{baseUrl}/wp-json/wc/v3/products" +
                                   $"?consumer_key={Uri.EscapeDataString(consumerKey)}" +
                                   $"&consumer_secret={Uri.EscapeDataString(consumerSecret)}" +
                                   $"&per_page=1";
                using var req2 = new HttpRequestMessage(HttpMethod.Get, prodEndpoint);
                req2.Headers.Add("Accept", "application/json");
                req2.Headers.Add("User-Agent", "Senkora/1.0");
                resp = await http.SendAsync(req2, ct);
            }

            sw.Stop();

            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync(ct);
                logger.LogWarning("WooCommerce test failed {Status}: {Body}",
                    (int)resp.StatusCode, body[..Math.Min(500, body.Length)]);

                var errMsg = resp.StatusCode == System.Net.HttpStatusCode.Unauthorized
                    ? "401 Yetkisiz: Consumer Key veya Consumer Secret hatali. WooCommerce'de 'Read/Write' yetkili API key kullanin."
                    : $"HTTP {(int)resp.StatusCode}: {resp.ReasonPhrase}";

                return new WooTestResult(false, null, null, errMsg, sw.ElapsedMilliseconds);
            }

            // Magaza bilgilerini ayri istekle al
            string? storeName = null;
            string? wooVersion = null;
            try
            {
                var infoUrl = $"{baseUrl}/wp-json";
                using var infoReq = new HttpRequestMessage(HttpMethod.Get, infoUrl);
                infoReq.Headers.Add("Accept", "application/json");
                var infoResp = await http.SendAsync(infoReq, ct);
                if (infoResp.IsSuccessStatusCode)
                {
                    var infoJson = await infoResp.Content.ReadAsStringAsync(ct);
                    var infoObj = JObject.Parse(infoJson);
                    storeName = infoObj["name"]?.ToString();
                }
            }
            catch { /* Store name optional */ }

            logger.LogInformation("WooCommerce test OK: {Url}", storeUrl);
            return new WooTestResult(true, storeName, wooVersion, null, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogWarning(ex, "WooCommerce test exception: {Url}", storeUrl);
            return new WooTestResult(false, null, null, ex.Message, sw.ElapsedMilliseconds);
        }
    }
}
