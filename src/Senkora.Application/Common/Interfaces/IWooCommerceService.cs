namespace Senkora.Application.Common.Interfaces;

/// <summary>
/// WooCommerce REST servis soyutlamasi.
/// </summary>
public interface IWooCommerceService
{
    Task<WooTestResult> TestConnectionAsync(
        string storeUrl, string consumerKey, string consumerSecret,
        CancellationToken ct = default);
}

public sealed record WooTestResult(
    bool   IsSuccess,
    string? StoreName,
    string? WooVersion,
    string? ErrorMessage,
    long   ResponseTimeMs);
