using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senkora.Domain.Entities.Catalog;

namespace Senkora.Infrastructure.Persistence.Configurations;

public class ProductSyncHistoryConfiguration : IEntityTypeConfiguration<ProductSyncHistory>
{
    public void Configure(EntityTypeBuilder<ProductSyncHistory> b)
    {
        b.ToTable("ProductSyncHistories");
        b.HasKey(x => x.Id);

        b.Property(x => x.Action).IsRequired().HasMaxLength(50);
        b.Property(x => x.Message).HasMaxLength(2000);
        b.Property(x => x.ChangesJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.PerformedBy).HasMaxLength(200);

        b.HasIndex(x => new { x.TenantId, x.ProductMappingId, x.CreatedAt });
        b.HasIndex(x => x.IsDeleted);
    }
}
