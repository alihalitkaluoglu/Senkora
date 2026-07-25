using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senkora.Domain.Entities.Sync;

namespace Senkora.Infrastructure.Persistence.Configurations;

public class SyncJobConfiguration : IEntityTypeConfiguration<SyncJob>
{
    public void Configure(EntityTypeBuilder<SyncJob> b)
    {
        b.ToTable("SyncJobs");
        b.HasKey(x => x.Id);
        b.Property(x => x.JobType).HasConversion<string>().HasMaxLength(50);
        b.Property(x => x.Direction).HasConversion<string>().HasMaxLength(50);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
        b.Property(x => x.TriggerSource).HasMaxLength(100);
        b.Property(x => x.ErrorMessage).HasMaxLength(2000);
        b.Property(x => x.HangfireJobId).HasMaxLength(100);

        b.HasIndex(x => new { x.TenantId, x.Status });
        b.HasIndex(x => new { x.TenantId, x.CreatedAt });

        b.HasMany(x => x.Logs)
         .WithOne(x => x.SyncJob)
         .HasForeignKey(x => x.SyncJobId)
         .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(x => x.Errors)
         .WithOne(x => x.SyncJob)
         .HasForeignKey(x => x.SyncJobId)
         .OnDelete(DeleteBehavior.Cascade);
    }
}
