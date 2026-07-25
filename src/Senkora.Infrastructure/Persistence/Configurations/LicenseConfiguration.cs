using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senkora.Domain.Entities.Licensing;

namespace Senkora.Infrastructure.Persistence.Configurations;

public class LicenseConfiguration : IEntityTypeConfiguration<License>
{
    public void Configure(EntityTypeBuilder<License> b)
    {
        b.ToTable("Licenses");
        b.HasKey(x => x.Id);
        b.Property(x => x.LicenseKey).IsRequired().HasMaxLength(500);
        b.Property(x => x.Tier).HasConversion<string>().HasMaxLength(50);
        b.Property(x => x.AllowedDomain).HasMaxLength(256);
        b.Property(x => x.HardwareFingerprint).HasMaxLength(500);
        b.HasIndex(x => x.LicenseKey).IsUnique();
        b.HasIndex(x => x.TenantId);

        b.HasMany(x => x.Activations)
         .WithOne(x => x.License)
         .HasForeignKey(x => x.LicenseId)
         .OnDelete(DeleteBehavior.Cascade);
    }
}
