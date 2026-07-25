using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senkora.Domain.Entities.Sync;

namespace Senkora.Infrastructure.Persistence.Configurations;

public class SyncErrorConfiguration : IEntityTypeConfiguration<SyncError>
{
    public void Configure(EntityTypeBuilder<SyncError> b)
    {
        b.ToTable("SyncErrors");
        b.HasKey(x => x.Id);
        b.Property(x => x.EntityType).IsRequired().HasMaxLength(100);
        b.Property(x => x.EntityId).IsRequired().HasMaxLength(200);
        b.Property(x => x.ErrorCode).IsRequired().HasMaxLength(100);
        b.Property(x => x.ErrorMessage).IsRequired().HasMaxLength(2000);
        b.Property(x => x.StackTrace).HasColumnType("nvarchar(max)");
        b.HasIndex(x => new { x.TenantId, x.IsResolved });
    }
}
