using Senkora.Domain.Entities.Common;

namespace Senkora.Domain.Entities.Identity;

public class Role : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsSystemRole { get; set; } = false;

    public ICollection<UserRole> UserRoles { get; set; } = [];
    public ICollection<RolePermission> Permissions { get; set; } = [];
}
