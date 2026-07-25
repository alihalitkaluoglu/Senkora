using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Senkora.Application.Common.Interfaces;

namespace Senkora.Infrastructure.ExternalServices.WooCommerce;

public sealed class WooMediaService(
    IHttpClientFactory httpClientFactory,
    ILogger<WooMediaService> logger) : IWooMediaService
{
    public async Task<WooMediaResult> UploadAsync(
        string storeUrl, string wpUsername, string wpAppPassword,
        Stream content, string fileName, string contentType,
        CancellationToken ct = default)
    {
        try
        {
            var http = httpClientFactory.CreateClient("WooCommerce");
            var url  = $"{storeUrl.TrimEnd('/')}/wp-json/wp/v2/media";

            // Application Password bosluklari icerebilir, WordPress bunlari kabul eder
            var creds = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{wpUsername}:{wpAppPassword}"));

            using var ms = new MemoryStream();
            await content.CopyToAsync(ms, ct);
            ms.Position = 0;

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Basic", creds);
            req.Headers.Add("Accept", "application/json");
            req.Headers.Add("User-Agent", "Senkora/1.0");

            var fileContent = new ByteArrayContent(ms.ToArray());
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            fileContent.Headers.ContentDisposition =
                new ContentDispositionHeaderValue("attachment") { FileName = fileName };
            req.Content = fileContent;

            var resp = await http.SendAsync(req, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                var msg = TryGetWpError(body);
                logger.LogWarning("WP media upload failed {Status}: {Msg}",
                    (int)resp.StatusCode, msg);

                var friendly = resp.StatusCode == System.Net.HttpStatusCode.Unauthorized
                    ? "WordPress kimlik dogrulamasi basarisiz. Kullanici adi ve " +
                      "Application Password degerlerini kontrol edin."
                    : $"WordPress medya yukleme hatasi ({(int)resp.StatusCode}): {msg}";

                return new WooMediaResult(false, null, null, friendly);
            }

            var obj       = JObject.Parse(body);
            var mediaId   = obj.Value<long?>("id");
            var sourceUrl = obj.Value<string>("source_url");

            if (string.IsNullOrWhiteSpace(sourceUrl))
                return new WooMediaResult(false, mediaId, null,
                    "WordPress yanitinda gorsel URL'i bulunamadi.");

            logger.LogInformation("WP media uploaded: #{Id} {Url}", mediaId, sourceUrl);
            return new WooMediaResult(true, mediaId, sourceUrl, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "WP media upload exception");
            return new WooMediaResult(false, null, null, ex.Message);
        }
    }

    private static string TryGetWpError(string body)
    {
        try
        {
            var o = JObject.Parse(body);
            return o["message"]?.ToString() ?? o["code"]?.ToString() ?? body[..Math.Min(200, body.Length)];
        }
        catch { return body[..Math.Min(200, body.Length)]; }
    }
}
