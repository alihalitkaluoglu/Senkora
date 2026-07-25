using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senkora.Domain.Entities.Orders;

namespace Senkora.Infrastructure.Persistence.Configurations;

public class OrderMappingConfiguration : IEntityTypeConfiguration<OrderMapping>
{
    public void Configure(EntityTypeBuilder<OrderMapping> b)
    {
        b.ToTable("OrderMappings");
        b.HasKey(x => x.Id);
        b.Property(x => x.WooOrderNumber).HasMaxLength(100);
        b.Property(x => x.WooOrderStatus).HasMaxLength(50);
        b.Property(x => x.WooOrderTotal).HasColumnType("decimal(18,4)");
        b.Property(x => x.LogoDocNumber).HasMaxLength(100);
        b.Property(x => x.LogoResourceType).HasMaxLength(50).HasDefaultValue("salesOrders");
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
        b.Property(x => x.TransferError).HasMaxLength(2000);

        b.HasIndex(x => new { x.TenantId, x.WooOrderId }).IsUnique();
        b.HasIndex(x => new { x.TenantId, x.Status });
        b.HasIndex(x => new { x.TenantId, x.LogoInternalRef });
    }
}
