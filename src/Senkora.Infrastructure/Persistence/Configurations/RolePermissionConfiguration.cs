using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senkora.Domain.Entities.Identity;

namespace Senkora.Infrastructure.Persistence.Configurations;

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> b)
    {
        b.ToTable("RolePermissions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Permission).IsRequired().HasMaxLength(200);
        b.HasIndex(x => new { x.RoleId, x.Permission }).IsUnique();
    }
}
