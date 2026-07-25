using Senkora.Domain.Entities.Common;
using Senkora.Domain.Enums;

namespace Senkora.Domain.Entities.Tenants;

public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public LicenseTier LicenseTier { get; set; } = LicenseTier.Trial;
    public DateTime? LicenseExpiresAt { get; set; }
    public int MaxWooStores { get; set; } = 1;
    public int MaxLogoConnections { get; set; } = 1;
    public int MaxMarketplaces { get; set; } = 0;

    public ICollection<TenantSettings> Settings { get; set; } = [];
}
