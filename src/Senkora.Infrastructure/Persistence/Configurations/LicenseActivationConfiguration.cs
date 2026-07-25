using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senkora.Domain.Entities.Licensing;

namespace Senkora.Infrastructure.Persistence.Configurations;

public class LicenseActivationConfiguration : IEntityTypeConfiguration<LicenseActivation>
{
    public void Configure(EntityTypeBuilder<LicenseActivation> b)
    {
        b.ToTable("LicenseActivations");
        b.HasKey(x => x.Id);
        b.Property(x => x.Domain).IsRequired().HasMaxLength(256);
        b.Property(x => x.HardwareFingerprint).IsRequired().HasMaxLength(500);
        b.Property(x => x.IpAddress).HasMaxLength(50);
    }
}
