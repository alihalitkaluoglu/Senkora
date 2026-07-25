using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senkora.Domain.Entities.Integration;

namespace Senkora.Infrastructure.Persistence.Configurations;

public class FieldMappingConfiguration : IEntityTypeConfiguration<FieldMapping>
{
    public void Configure(EntityTypeBuilder<FieldMapping> b)
    {
        b.ToTable("FieldMappings");
        b.HasKey(x => x.Id);
        b.Property(x => x.EntityType).HasConversion<string>().HasMaxLength(50);
        b.Property(x => x.SourceField).IsRequired().HasMaxLength(200);
        b.Property(x => x.TargetField).IsRequired().HasMaxLength(200);
        b.Property(x => x.DefaultValue).HasMaxLength(1000);
        b.Property(x => x.TransformExpression).HasMaxLength(2000);
        b.HasIndex(x => new { x.TenantId, x.EntityType });
    }
}
