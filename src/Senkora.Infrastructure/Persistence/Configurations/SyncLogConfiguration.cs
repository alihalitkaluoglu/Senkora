using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senkora.Domain.Entities.Sync;

namespace Senkora.Infrastructure.Persistence.Configurations;

public class SyncLogConfiguration : IEntityTypeConfiguration<SyncLog>
{
    public void Configure(EntityTypeBuilder<SyncLog> b)
    {
        b.ToTable("SyncLogs");
        b.HasKey(x => x.Id);
        b.Property(x => x.Level).HasMaxLength(20).HasDefaultValue("Information");
        b.Property(x => x.Message).IsRequired().HasMaxLength(2000);
        b.Property(x => x.RequestUrl).HasMaxLength(1000);
        b.Property(x => x.RequestBody).HasColumnType("nvarchar(max)");
        b.Property(x => x.ResponseBody).HasColumnType("nvarchar(max)");
        b.Property(x => x.EntityType).HasMaxLength(100);
        b.Property(x => x.EntityId).HasMaxLength(100);
        b.HasIndex(x => new { x.TenantId, x.SyncJobId });
        b.HasIndex(x => x.CreatedAt);
    }
}
