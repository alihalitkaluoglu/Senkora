namespace Senkora.Application.Common.Interfaces;

/// <summary>
/// Logo veritabanindaki tablo adlarini kesfeder.
/// Logo surumleri arasinda tablo adlari degistigi icin
/// sabit isim varsaymak yerine sys.tables uzerinden aranir.
/// </summary>
public interface ILogoSchemaService
{
    /// <summary>Ada gore tablo arar (LIKE '%pattern%').</summary>
    Task<List<string>> FindTablesAsync(
        string restUrl, string accessToken, string pattern,
        CancellationToken ct = default);

    /// <summary>Ticari islem grubu tablosunu bulur.</summary>
    Task<string?> ResolveTradingGroupTableAsync(
        string restUrl, string accessToken, int firmNo, CancellationToken ct = default);

    /// <summary>Stok toplam tablosunu bulur.</summary>
    Task<string?> ResolveStockTableAsync(
        string restUrl, string accessToken,
        int firmNo, int periodNo, CancellationToken ct = default);
}
