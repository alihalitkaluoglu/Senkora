namespace Senkora.Application.Common.Interfaces;

/// <summary>
/// WooCommerce baglantisi icin sifre cozulmus kimlik bilgilerini cozumler.
/// </summary>
public interface IWooConnectionResolver
{
    Task<WooConnectionInfo> ResolveAsync(
        Guid tenantId, Guid storeId, CancellationToken ct = default);
}

public sealed record WooConnectionInfo(
    string  StoreUrl,
    string  ConsumerKey,
    string  ConsumerSecret,
    string? WpUsername      = null,
    string? WpAppPassword   = null)
{
    /// <summary>WordPress medya yuklemesi icin kimlik bilgileri tanimli mi?</summary>
    public bool CanUploadMedia =>
        !string.IsNullOrWhiteSpace(WpUsername) &&
        !string.IsNullOrWhiteSpace(WpAppPassword);
}
