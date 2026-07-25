using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senkora.Domain.Entities.Identity;

namespace Senkora.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("RefreshTokens");
        b.HasKey(x => x.Id);
        b.Property(x => x.Token).IsRequired().HasMaxLength(500);
        b.Property(x => x.CreatedByIp).HasMaxLength(50);
        b.Property(x => x.ReplacedByToken).HasMaxLength(500);
        b.HasIndex(x => x.Token);
        b.HasIndex(x => new { x.UserId, x.IsRevoked });
    }
}
