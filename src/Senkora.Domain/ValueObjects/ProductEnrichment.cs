namespace Senkora.Domain.ValueObjects;

/// <summary>
/// Portal'da yapılan ürün zenginleştirme verisi.
/// ProductMapping tablosunda JSON olarak saklanır.
/// Logo'dan otomatik gelemeyen WooCommerce'e özel alanlar.
/// </summary>
public sealed class ProductEnrichment
{
    public List<ProductImage>     Images            { get; set; } = [];
    public List<int>              WooCategoryIds    { get; set; } = [];
    public List<string>           Tags              { get; set; } = [];
    public List<ProductAttribute> Attributes        { get; set; } = [];
    public ProductDimensions?     Dimensions        { get; set; }
    public string?                ShippingClass     { get; set; }
    public string?                CatalogVisibility { get; set; } = "visible";
    public bool                   Featured          { get; set; } = false;
    public string?                OverrideName      { get; set; }
    public string?                OverrideShortDesc { get; set; }
    public string?                OverrideDescription { get; set; }
    public string?                OverrideSlug      { get; set; }
    public List<ProductMeta>      CustomMeta        { get; set; } = [];
    public bool                   ManageStock       { get; set; } = true;
    public string                 BackorderPolicy   { get; set; } = "no"; // no, notify, yes
    public decimal?               RegularPriceOverride { get; set; }
    public decimal?               SalePriceOverride { get; set; }
    public DateTime?              SaleFrom          { get; set; }
    public DateTime?              SaleTo            { get; set; }
}

public sealed class ProductImage
{
    /// <summary>Sunucudaki yerel yol (tenant/mapping/guid.jpg)</summary>
    public string  StoredPath { get; set; } = "";
    /// <summary>WordPress medya kutuphanesine yuklendikten sonraki public URL</summary>
    public string? RemoteUrl  { get; set; }
    public string? Alt        { get; set; }
    public bool    IsFeatured { get; set; }
    public int     SortOrder  { get; set; }
}

public sealed class ProductAttribute
{
    public string       Name      { get; set; } = "";
    public List<string> Options   { get; set; } = [];
    public bool         Visible   { get; set; } = true;
    public bool         Variation { get; set; } = false;
}

public sealed class ProductDimensions
{
    public string? Length { get; set; }
    public string? Width  { get; set; }
    public string? Height { get; set; }
}

public sealed class ProductMeta
{
    public string Key   { get; set; } = "";
    public string Value { get; set; } = "";
}
