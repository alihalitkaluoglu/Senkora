using Senkora.Domain.Entities.Common;

namespace Senkora.Domain.Entities.Licensing;

public class LicenseActivation : BaseEntity
{
    public Guid LicenseId { get; set; }
    public License License { get; set; } = null!;
    public string Domain { get; set; } = string.Empty;
    public string HardwareFingerprint { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public DateTime ActivatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastCheckedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
