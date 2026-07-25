using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senkora.Domain.Entities.Catalog;

namespace Senkora.Infrastructure.Persistence.Configurations;

public class ProductMappingConfiguration : IEntityTypeConfiguration<ProductMapping>
{
    public void Configure(EntityTypeBuilder<ProductMapping> b)
    {
        b.ToTable("ProductMappings");
        b.HasKey(x => x.Id);

        // Logo alanları
        b.Property(x => x.LogoItemCode).IsRequired().HasMaxLength(100);
        b.Property(x => x.LogoItemName).HasMaxLength(500);
        b.Property(x => x.LogoGroupCode).HasMaxLength(100);
        b.Property(x => x.LogoSpecode).HasMaxLength(100);
        b.Property(x => x.LogoAuxDesc).HasMaxLength(1000);
        b.Property(x => x.LogoDescription).HasColumnType("nvarchar(max)");
        b.Property(x => x.LogoSellPrice).HasColumnType("decimal(18,4)");
        b.Property(x => x.LogoSellPrice2).HasColumnType("decimal(18,4)");
        b.Property(x => x.LogoVatRate).HasColumnType("decimal(18,4)");
        b.Property(x => x.LogoStock).HasColumnType("decimal(18,4)");
        b.Property(x => x.LogoWeight).HasColumnType("decimal(18,4)");
        b.Property(x => x.LogoUnitCode).HasMaxLength(50);

        // WooCommerce alanları
        b.Property(x => x.WooSku).HasMaxLength(200);
        b.Property(x => x.WooProductName).HasMaxLength(500);
        b.Property(x => x.WooProductUrl).HasMaxLength(500);

        // Durum
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
        b.Property(x => x.LastSyncError).HasMaxLength(2000);
        b.Property(x => x.LastSyncedPrice).HasColumnType("decimal(18,4)");
        b.Property(x => x.LastSyncedStock).HasColumnType("decimal(18,4)");

        // JSON
        b.Property(x => x.EnrichmentJson).HasColumnType("nvarchar(max)");

        // Index'ler
        b.HasIndex(x => new { x.TenantId, x.LogoItemRef, x.WooStoreId }).IsUnique();
        b.HasIndex(x => new { x.TenantId, x.Status });
        b.HasIndex(x => new { x.TenantId, x.WooProductId });
        b.HasIndex(x => x.IsDeleted);
    }
}
