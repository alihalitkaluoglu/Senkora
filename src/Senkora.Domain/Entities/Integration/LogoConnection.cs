using Senkora.Domain.Entities.Common;

namespace Senkora.Domain.Entities.Integration;

public class LogoConnection : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string RestUrl { get; set; } = string.Empty;
    public string ClientIdEncrypted { get; set; } = string.Empty;
    public string ClientSecretEncrypted { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PasswordEncrypted { get; set; } = string.Empty;
    public int FirmNo { get; set; }
    public int PeriodNo { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public bool IsVerified { get; set; } = false;
    public DateTime? LastVerifiedAt { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
    // Cached token info (encrypted)
    public string? CachedTokenEncrypted { get; set; }
    public DateTime? TokenExpiresAt { get; set; }
}
