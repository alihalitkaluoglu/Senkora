using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senkora.Domain.Entities.Identity;

namespace Senkora.Infrastructure.Persistence.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> b)
    {
        b.ToTable("Roles");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(100);
        b.Property(x => x.Description).HasMaxLength(500);
        b.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();

        b.HasMany(x => x.UserRoles)
         .WithOne(x => x.Role)
         .HasForeignKey(x => x.RoleId)
         .OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.Permissions)
         .WithOne(x => x.Role)
         .HasForeignKey(x => x.RoleId)
         .OnDelete(DeleteBehavior.Cascade);
    }
}
