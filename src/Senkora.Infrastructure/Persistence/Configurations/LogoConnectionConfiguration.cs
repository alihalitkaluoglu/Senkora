using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senkora.Domain.Entities.Integration;

namespace Senkora.Infrastructure.Persistence.Configurations;

public class LogoConnectionConfiguration : IEntityTypeConfiguration<LogoConnection>
{
    public void Configure(EntityTypeBuilder<LogoConnection> b)
    {
        b.ToTable("LogoConnections");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.RestUrl).IsRequired().HasMaxLength(500);
        b.Property(x => x.ClientIdEncrypted).IsRequired().HasMaxLength(2000);
        b.Property(x => x.ClientSecretEncrypted).IsRequired().HasMaxLength(2000);
        b.Property(x => x.Username).IsRequired().HasMaxLength(200);
        b.Property(x => x.PasswordEncrypted).IsRequired().HasMaxLength(2000);
        b.Property(x => x.CachedTokenEncrypted).HasMaxLength(4000);
        b.HasIndex(x => new { x.TenantId, x.RestUrl });
        b.HasIndex(x => x.IsDeleted);
    }
}
