using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senkora.Domain.Entities.Integration;

namespace Senkora.Infrastructure.Persistence.Configurations;

public class WooStoreConfiguration : IEntityTypeConfiguration<WooStore>
{
    public void Configure(EntityTypeBuilder<WooStore> b)
    {
        b.ToTable("WooStores");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.StoreUrl).IsRequired().HasMaxLength(500);
        b.Property(x => x.ConsumerKeyEncrypted).IsRequired().HasMaxLength(2000);
        b.Property(x => x.ConsumerSecretEncrypted).IsRequired().HasMaxLength(2000);
        b.Property(x => x.WebhookSecret).HasMaxLength(500);
        b.Property(x => x.ApiVersion).HasMaxLength(20).HasDefaultValue("wc/v3");
        b.Property(x => x.WpUsername).HasMaxLength(200);
        b.Property(x => x.WpAppPasswordEncrypted).HasMaxLength(2000);
        b.HasIndex(x => new { x.TenantId, x.StoreUrl });
        b.HasIndex(x => x.IsDeleted);
    }
}
