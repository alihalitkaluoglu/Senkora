namespace Senkora.Application.Common.Interfaces;

/// <summary>
/// Logo ERP REST servis soyutlamasi.
/// Application katmani bu interface uzerinden Logo ile konusur.
/// Implementasyon Infrastructure katmanindadir.
/// </summary>
public interface ILogoRestService
{
    /// <summary>Token alarak baglanti testi yapar. CurrentFirm dondurur.</summary>
    Task<LogoTestResult> TestConnectionAsync(
        string restUrl, string clientId, string clientSecret,
        string username, string password, int firmNo,
        CancellationToken ct = default);
}

public sealed record LogoTestResult(
    bool   IsSuccess,
    string? AccessToken,
    int?   CurrentFirm,
    string? ErrorMessage,
    long   ResponseTimeMs);
