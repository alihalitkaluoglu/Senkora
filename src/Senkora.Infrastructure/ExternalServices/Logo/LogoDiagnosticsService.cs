using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Senkora.Application.Common.Interfaces;
using Senkora.Application.Features.Products.Queries;

namespace Senkora.Infrastructure.ExternalServices.Logo;

public sealed class LogoDiagnosticsService(
    LogoRestClient client,
    ILogger<LogoDiagnosticsService> logger) : ILogoDiagnosticsService
{
    public async Task<LogoFetchDiagnostics> ProbeItemsAsync(
        string restUrl, string accessToken, int limit, CancellationToken ct = default)
    {
        var url = $"{restUrl.TrimEnd('/')}/api/v1/items?offset=0&limit={limit}";
        string raw;

        try
        {
            raw = await client.GetAsync(url, accessToken, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Diagnostics: items request failed");
            return new LogoFetchDiagnostics(
                TokenObtained: true, TokenPreview: null,
                RequestUrl: url, RequestSucceeded: false,
                RawResponsePreview: null, ParsedItemCount: 0, FirstItemJson: null,
                ErrorMessage: ex.Message, ErrorStage: "HTTP_REQUEST");
        }

        var preview = raw.Length > 1500 ? raw[..1500] + " ...[kesildi]" : raw;

        try
        {
            var trimmed = raw.TrimStart();
            JArray arr;

            if (trimmed.StartsWith('['))
            {
                arr = JArray.Parse(raw);
            }
            else
            {
                var obj = JObject.Parse(raw);
                if (obj["Message"] != null)
                    return new LogoFetchDiagnostics(
                        true, null, url, false, preview, 0, null,
                        obj["Message"]!.ToString(), "LOGO_API_ERROR");

                arr = obj["items"] as JArray
                   ?? obj["Items"] as JArray
                   ?? obj["value"] as JArray
                   ?? [];
            }

            var firstJson = arr.FirstOrDefault()?.ToString();
            return new LogoFetchDiagnostics(
                TokenObtained: true, TokenPreview: null,
                RequestUrl: url, RequestSucceeded: true,
                RawResponsePreview: preview,
                ParsedItemCount: arr.Count,
                FirstItemJson: firstJson is not null && firstJson.Length > 2000
                    ? firstJson[..2000] + " ...[kesildi]" : firstJson,
                ErrorMessage: arr.Count == 0
                    ? "Yanit alindi ancak items dizisi bos veya beklenen formatta degil."
                    : null,
                ErrorStage: arr.Count == 0 ? "PARSE_EMPTY" : null);
        }
        catch (Exception ex)
        {
            return new LogoFetchDiagnostics(
                true, null, url, true, preview, 0, null,
                ex.Message, "JSON_PARSE");
        }
    }
}
