using Senkora.Domain.Entities.Common;
using Senkora.Domain.Enums;

namespace Senkora.Domain.Entities.Licensing;

public class License : BaseEntity
{
    public Guid TenantId { get; set; }
    public string LicenseKey { get; set; } = string.Empty;
    public LicenseTier Tier { get; set; }
    public DateTime IssuedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public int MaxWooStores { get; set; }
    public int MaxLogoConnections { get; set; }
    public int MaxMarketplaces { get; set; }
    public int MaxProductsPerSync { get; set; }
    public int MaxOrdersPerMonth { get; set; }
    public string? AllowedDomain { get; set; }
    public string? HardwareFingerprint { get; set; }
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    public ICollection<LicenseActivation> Activations { get; set; } = [];
}
