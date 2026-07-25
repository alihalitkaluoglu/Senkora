using Microsoft.Extensions.Logging;
using Senkora.Application.Common.Interfaces;

namespace Senkora.Infrastructure.ExternalServices.Logo;

/// <summary>ILogoRestService implementasyonu</summary>
public sealed class LogoRestService(
    LogoRestClient client,
    ILogger<LogoRestService> logger) : ILogoRestService
{
    public async Task<LogoTestResult> TestConnectionAsync(
        string restUrl, string clientId, string clientSecret,
        string username, string password, int firmNo,
        CancellationToken ct = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var token = await client.GetTokenAsync(
                restUrl, clientId, clientSecret, username, password, firmNo, ct);

            var firmResp = await client.GetAsync(
                $"{restUrl.TrimEnd('/')}/api/v1/methods/CurrentFirm",
                token.AccessToken, ct);

            sw.Stop();
            _ = int.TryParse(firmResp.Trim('"', ' '), out var firm);

            logger.LogInformation("Logo test OK: {Url}", restUrl);
            return new LogoTestResult(true, token.AccessToken, firm > 0 ? firm : null, null, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogWarning(ex, "Logo test FAIL: {Url}", restUrl);
            return new LogoTestResult(false, null, null, ex.Message, sw.ElapsedMilliseconds);
        }
    }
}
