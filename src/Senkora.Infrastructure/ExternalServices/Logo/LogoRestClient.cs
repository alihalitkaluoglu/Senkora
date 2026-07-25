using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Senkora.Domain.ValueObjects;
using Senkora.Infrastructure.ExternalServices.Logo.Models;

namespace Senkora.Infrastructure.ExternalServices.Logo;

/// <summary>
/// Low-level Logo REST HTTP client.
/// All calls go through LogoTokenManager for token management.
/// Ref: Logo REST Teknik Dokumani - Kimlik Dogrulama, HTTP Metodlari
/// </summary>
public sealed class LogoRestClient(
    HttpClient httpClient,
    ILogger<LogoRestClient> logger)
{
    /// <summary>
    /// Obtains an access token using OAuth2 Password Grant.
    /// POST /api/v1/token with Basic auth (clientId:clientSecret)
    /// </summary>
    public async Task<LogoTokenResponse> GetTokenAsync(
        string baseUrl,
        string clientId,
        string clientSecret,
        string username,
        string password,
        int firmNo,
        CancellationToken ct = default)
    {
        var tokenUrl = $"{baseUrl.TrimEnd('/')}/api/v1/token";
        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

        var body = $"grant_type=password" +
                   $"&username={Uri.EscapeDataString(username)}" +
                   $"&password={Uri.EscapeDataString(password)}" +
                   $"&firmno={firmNo}";

        using var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl);
        request.Headers.Add("Authorization", $"Basic {credentials}");
        request.Headers.Add("Accept", "application/json");
        request.Content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded");

        logger.LogDebug("Logo token request to {Url} for firm {FirmNo}", tokenUrl, firmNo);

        var response = await httpClient.SendAsync(request, ct);
        var content = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Logo token request failed: {StatusCode} {Content}", response.StatusCode, content);
            throw new HttpRequestException($"Logo token failed: {response.StatusCode} - {content}");
        }

        var tokenResponse = JsonConvert.DeserializeObject<LogoTokenResponse>(content)
            ?? throw new InvalidOperationException("Logo token response is null.");

        logger.LogDebug("Logo token obtained, expires in {ExpiresIn}s", tokenResponse.ExpiresIn);
        return tokenResponse;
    }

    /// <summary>GET /api/v1/{resource} or /api/v1/{resource}/{id}</summary>
    public async Task<string> GetAsync(string url, string accessToken, CancellationToken ct = default)
        => await SendAsync(HttpMethod.Get, url, null, accessToken, ct);

    /// <summary>POST /api/v1/{resource} — Creates a new resource</summary>
    public async Task<string> PostAsync(string url, object body, string accessToken, CancellationToken ct = default)
        => await SendAsync(HttpMethod.Post, url, body, accessToken, ct);

    /// <summary>PUT /api/v1/{resource}/{ref} — Full update</summary>
    public async Task<string> PutAsync(string url, object body, string accessToken, CancellationToken ct = default)
        => await SendAsync(HttpMethod.Put, url, body, accessToken, ct);

    /// <summary>PATCH /api/v1/{resource}/{ref} — Partial update (header fields only)</summary>
    public async Task<string> PatchAsync(string url, object body, string accessToken, CancellationToken ct = default)
        => await SendAsync(HttpMethod.Patch, url, body, accessToken, ct);

    /// <summary>DELETE /api/v1/{resource}/{ref}</summary>
    public async Task<string> DeleteAsync(string url, string accessToken, CancellationToken ct = default)
        => await SendAsync(HttpMethod.Delete, url, null, accessToken, ct);

    /// <summary>POST /api/v1/queries/unsafe — Raw SQL (must be enabled in Logo config)</summary>
    public async Task<string> UnsafeQueryAsync(
        string baseUrl, string sql, string accessToken,
        int cmdTimeoutSeconds = 30, CancellationToken ct = default)
    {
        var url = $"{baseUrl.TrimEnd('/')}/api/v1/queries/unsafe?cmdTimeout={cmdTimeoutSeconds}";
        return await SendAsync(HttpMethod.Post, url, $"\"{sql}\"", accessToken, ct);
    }

    private async Task<string> SendAsync(
        HttpMethod method, string url, object? body,
        string accessToken, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Add("Authorization", $"Bearer {accessToken}");
        request.Headers.Add("Accept", "application/json");

        if (body is not null)
        {
            var json = body is string s ? s : JsonConvert.SerializeObject(body);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var response = await httpClient.SendAsync(request, ct);
        sw.Stop();

        var content = await response.Content.ReadAsStringAsync(ct);

        logger.LogDebug("Logo REST {Method} {Url} -> {StatusCode} ({Ms}ms)",
            method, url, (int)response.StatusCode, sw.ElapsedMilliseconds);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Logo REST error {StatusCode} at {Url}: {Content}",
                response.StatusCode, url, content);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException($"Logo token expired or invalid. URL: {url}");

            throw new HttpRequestException(
                $"Logo REST {method} {url} failed: {(int)response.StatusCode} - {content}");
        }

        return content;
    }
}
