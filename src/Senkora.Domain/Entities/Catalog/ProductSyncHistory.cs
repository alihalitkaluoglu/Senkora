using Senkora.Domain.Entities.Common;

namespace Senkora.Domain.Entities.Catalog;

/// <summary>
/// Urun bazinda aktarim ve degisiklik gecmisi.
/// Her Logo cekme, zenginlestirme ve WooCommerce gonderimi kaydedilir.
/// </summary>
public class ProductSyncHistory : TenantEntity
{
    public Guid   ProductMappingId { get; set; }

    /// <summary>LogoFetch, Enrich, WooCreate, WooUpdate, Error</summary>
    public string Action     { get; set; } = "";
    public bool   IsSuccess  { get; set; }
    public string? Message   { get; set; }

    /// <summary>Degisen alanlar: {"LogoSellPrice":{"old":100,"new":120}}</summary>
    public string? ChangesJson { get; set; }

    public long?  WooProductId { get; set; }
    public int    DurationMs   { get; set; }
    public string? PerformedBy { get; set; }
}
