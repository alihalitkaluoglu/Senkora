using Senkora.Domain.Entities.Common;

namespace Senkora.Domain.Entities.Identity;

public class RolePermission : TenantEntity
{
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public string Permission { get; set; } = string.Empty;
}
