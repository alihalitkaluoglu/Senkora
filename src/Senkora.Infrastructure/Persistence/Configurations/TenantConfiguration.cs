using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senkora.Domain.Entities.Tenants;

namespace Senkora.Infrastructure.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> b)
    {
        b.ToTable("Tenants");
        b.HasKey(x => x.Id);

        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.Subdomain).IsRequired().HasMaxLength(100);
        b.Property(x => x.ContactEmail).IsRequired().HasMaxLength(256);
        b.Property(x => x.ContactPhone).HasMaxLength(50);
        b.Property(x => x.LicenseTier).HasConversion<string>().HasMaxLength(50);

        b.HasIndex(x => x.Subdomain).IsUnique();
        b.HasIndex(x => x.IsDeleted);

        b.HasMany(x => x.Settings)
         .WithOne()
         .HasForeignKey(x => x.TenantId)
         .OnDelete(DeleteBehavior.Cascade);
    }
}
