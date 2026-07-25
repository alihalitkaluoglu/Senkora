namespace Senkora.Application.Common.Interfaces;

public interface ILogoProductService
{
    /// <summary>Malzeme kartlarini sayfa sayfa ceker.</summary>
    Task<List<LogoItemDto>> FetchItemsAsync(
        string restUrl, string accessToken,
        int firmNo, int offset = 0, int limit = 100,
        CancellationToken ct = default);

    Task<LogoItemDto?> FetchItemByRefAsync(
        string restUrl, string accessToken,
        long itemRef, CancellationToken ct = default);

    /// <summary>
    /// Malzeme satis fiyat kartlarini ceker (ITEM_SALES_PRICE / salesItemPrices).
    /// Sonuc: itemRef -> fiyat sozlugu.
    /// </summary>
    Task<Dictionary<long, LogoItemPriceDto>> FetchSalesPricesAsync(
        string restUrl, string accessToken,
        CancellationToken ct = default);

    /// <summary>Logo'daki toplam malzeme karti sayisini dondurur (sayfalama plani icin).</summary>
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

/// <summary>Logo malzeme satis fiyat karti</summary>
public sealed record LogoItemPriceDto(
    long    ItemRef,
    string  ItemCode,
    decimal Price,
    decimal VatRate,
    string? CurrencyCode,
    int     PriceListRef,
    DateTime? BeginDate,
    DateTime? EndDate);
