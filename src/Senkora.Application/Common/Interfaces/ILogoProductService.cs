namespace Senkora.Application.Common.Interfaces;

public interface ILogoProductService
{
    /// <summary>
    /// Bir sayfa malzeme ceker. Donen nesne bir sonraki offset'i icerir —
    /// filtreleme yapildigi icin cagiran taraf items.Count ile offset ilerletmemeli.
    /// </summary>
    Task<LogoItemPage> FetchItemsAsync(
        string restUrl, string accessToken,
        int firmNo, int offset, int maxScan,
        CancellationToken ct = default);

    Task<LogoItemDto?> FetchItemByRefAsync(
        string restUrl, string accessToken,
        long itemRef, CancellationToken ct = default);

    Task<Dictionary<long, decimal>> FetchStockAsync(
        string restUrl, string accessToken,
        int firmNo, int periodNo, CancellationToken ct = default);

    Task<List<LogoItemPriceDto>> FetchSalesPricesAsync(
        string restUrl, string accessToken, CancellationToken ct = default);

    Task<int> GetItemCountAsync(
        string restUrl, string accessToken, CancellationToken ct = default);
}

/// <summary>
/// Logo'dan alinan bir sayfa.
///   Items      → filtreden gecen malzemeler (TM/MM, aktif)
///   RawScanned → Logo'nun dondurdugu ham kayit sayisi
///   NextOffset → bir sonraki istekte kullanilacak offset
///   HasMore    → Logo'da daha kayit var mi
/// </summary>
public sealed record LogoItemPage(
    List<LogoItemDto> Items,
    int               RawScanned,
    int               NextOffset,
    bool              HasMore);

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
