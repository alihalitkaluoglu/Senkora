using Senkora.Domain.Entities.Common;
using Senkora.Domain.Enums;

namespace Senkora.Domain.Entities.Catalog;

public class ProductMapping : TenantEntity
{
    // Logo tarafı
    public long    LogoItemRef     { get; set; }
    public string  LogoItemCode    { get; set; } = "";
    public string  LogoItemName    { get; set; } = "";
    public string? LogoGroupCode   { get; set; }
    public string? LogoSpecode     { get; set; }
    public string? LogoAuxDesc     { get; set; }
    public string? LogoDescription { get; set; }
    public decimal LogoSellPrice   { get; set; }
    public decimal LogoSellPrice2  { get; set; }
    public decimal LogoVatRate     { get; set; }
    public decimal LogoStock       { get; set; }
    public decimal LogoWeight      { get; set; }
    public string? LogoUnitCode    { get; set; }
    public long?   LogoMarkRef     { get; set; }
    public int     LogoCardType    { get; set; } = 1; // 1=Malzeme, 2=Hizmet
    public DateTime LogoLastFetched { get; set; } = DateTime.UtcNow;

    // WooCommerce tarafı
    public long?   WooProductId    { get; set; }
    public string? WooSku          { get; set; }
    public string? WooProductName  { get; set; }
    public string? WooProductUrl   { get; set; }

    // Eşleme durumu
    public SyncMappingStatus Status         { get; set; } = SyncMappingStatus.Draft;
    public string?            LastSyncError { get; set; }
    public DateTime?          LastSyncedAt  { get; set; }
    public decimal?           LastSyncedPrice { get; set; }
    public decimal?           LastSyncedStock { get; set; }

    // Portal zenginleştirme verisi (JSON)
    public string? EnrichmentJson { get; set; }

    // Bağlantılar
    public Guid LogoConnectionId { get; set; }
    public Guid WooStoreId       { get; set; }
}
