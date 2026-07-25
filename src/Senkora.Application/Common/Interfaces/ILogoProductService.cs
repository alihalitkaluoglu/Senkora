namespace Senkora.Application.Common.Interfaces;

public interface ILogoProductService
{
    Task<List<LogoItemDto>> FetchItemsAsync(
        string restUrl, string accessToken,
        int firmNo, int offset = 0, int limit = 100,
        CancellationToken ct = default);

    Task<LogoItemDto?> FetchItemByRefAsync(
        string restUrl, string accessToken,
        long itemRef, CancellationToken ct = default);

    /// <summary>Stok miktarlari (itemRef -> miktar). SQL sorgusu ile alinir.</summary>
    Task<Dictionary<long, decimal>> FetchStockAsync(
        string restUrl, string accessToken,
        int firmNo, int periodNo, CancellationToken ct = default);

    /// <summary>Tum satis fiyat kartlari. Secim cagiran tarafta PriceSelector ile yapilir.</summary>
    Task<List<LogoItemPriceDto>> FetchSalesPricesAsync(
        string restUrl, string accessToken, CancellationToken ct = default);

    Task<int> GetItemCountAsync(
        string restUrl, string accessToken, CancellationToken ct = default);
}

public sealed record LogoItemDto(
    long    LogicalRef,
    string  Code,
    string  Name,
    string? AuxDesc,
    string? Description,
    string? GroupCode,
    string? Specode,
    string? UnitCode,
    long?   MarkRef,
    int     CardType,
    decimal SellPrice,
    decimal SellPrice2,
    decimal VatRate,
    decimal Stock,
    decimal Weight);

/// <summary>
/// Logo malzeme satis fiyat karti.
/// Proje kodu / ticari islem grubu / masraf merkezi fiyat secim kriterleridir.
/// </summary>
public sealed record LogoItemPriceDto(
    long      ItemRef,
    string    ItemCode,
    decimal   Price,
    decimal   VatRate,
    string?   CurrencyCode,
    int       PriceListRef,
    DateTime? BeginDate,
    DateTime? EndDate,
    string?   ProjectCode      = null,
    string?   TradingGroupCode = null,
    string?   CostCenterCode   = null);
