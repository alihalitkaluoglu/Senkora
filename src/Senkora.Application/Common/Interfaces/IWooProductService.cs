namespace Senkora.Application.Common.Interfaces;

public interface IWooProductService
{
    /// <summary>WooCommerce'de yeni ürün oluşturur. Oluşturulan ürün ID'sini döner.</summary>
    Task<long> CreateProductAsync(
        string storeUrl, string consumerKey, string consumerSecret,
        WooProductPayload payload, CancellationToken ct = default);

    /// <summary>Mevcut ürünü günceller.</summary>
    Task UpdateProductAsync(
        string storeUrl, string consumerKey, string consumerSecret,
        long productId, WooProductPayload payload, CancellationToken ct = default);

    /// <summary>Sadece stok ve fiyat günceller (hızlı sync).</summary>
    Task PatchStockAndPriceAsync(
        string storeUrl, string consumerKey, string consumerSecret,
        long productId, decimal stock, decimal price, decimal? salePrice,
        CancellationToken ct = default);

    /// <summary>WooCommerce'den kategorileri listeler.</summary>
    Task<List<WooCategoryDto>> GetCategoriesAsync(
        string storeUrl, string consumerKey, string consumerSecret,
        CancellationToken ct = default);

    /// <summary>WooCommerce'den kargo sınıflarını listeler.</summary>
    Task<List<WooShippingClassDto>> GetShippingClassesAsync(
        string storeUrl, string consumerKey, string consumerSecret,
        CancellationToken ct = default);
}

public sealed class WooProductPayload
{
    public string        Name              { get; set; } = "";
    public string        Sku               { get; set; } = "";
    public string        Type              { get; set; } = "simple";
    public string        Status            { get; set; } = "publish";
    public string?       Description       { get; set; }
    public string?       ShortDescription  { get; set; }
    public string        RegularPrice      { get; set; } = "0";
    public string?       SalePrice         { get; set; }
    public string?       DateOnSaleFrom    { get; set; }
    public string?       DateOnSaleTo      { get; set; }
    public bool          ManageStock       { get; set; } = true;
    public int           StockQuantity     { get; set; }
    public string        StockStatus       { get; set; } = "instock";
    public string        Backorders        { get; set; } = "no";
    public string?       Weight            { get; set; }
    public WooDimensions? Dimensions       { get; set; }
    public List<WooCatRef>   Categories    { get; set; } = [];
    public List<WooTagRef>   Tags          { get; set; } = [];
    public List<WooImage>    Images        { get; set; } = [];
    public List<WooAttribute> Attributes   { get; set; } = [];
    public string?       ShippingClass     { get; set; }
    public string        CatalogVisibility { get; set; } = "visible";
    public bool          Featured          { get; set; } = false;
    public string?       Slug              { get; set; }
    public string?       TaxClass          { get; set; }
    public List<WooMeta>     MetaData      { get; set; } = [];
}

public record WooDimensions(string? Length, string? Width, string? Height);
public record WooCatRef(int Id);
public record WooTagRef(string Name);
public record WooImage(string Src, string? Alt = null, int Position = 0);
public record WooAttribute(string Name, List<string> Options, bool Visible = true, bool Variation = false);
public record WooMeta(string Key, object Value);
public record WooCategoryDto(int Id, string Name, string Slug, int? ParentId, int Count);
public record WooShippingClassDto(int Id, string Name, string Slug);
