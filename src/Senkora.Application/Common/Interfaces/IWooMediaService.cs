namespace Senkora.Application.Common.Interfaces;

/// <summary>
/// WordPress Media Library'ye gorsel yukler.
/// WooCommerce urun gorselleri icin WordPress'te barindirilan URL gerekir.
/// Kimlik dogrulama: WordPress kullanici adi + Application Password (Basic Auth).
/// </summary>
public interface IWooMediaService
{
    /// <summary>
    /// Gorseli WordPress medya kutuphanesine yukler, public URL doner.
    /// </summary>
    Task<WooMediaResult> UploadAsync(
        string storeUrl, string wpUsername, string wpAppPassword,
        Stream content, string fileName, string contentType,
        CancellationToken ct = default);
}

public sealed record WooMediaResult(
    bool    IsSuccess,
    long?   MediaId,
    string? SourceUrl,
    string? ErrorMessage);
