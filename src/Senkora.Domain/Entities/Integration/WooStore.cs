using Senkora.Domain.Entities.Common;

namespace Senkora.Domain.Entities.Integration;

public class WooStore : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string StoreUrl { get; set; } = string.Empty;
    public string ConsumerKeyEncrypted { get; set; } = string.Empty;
    public string ConsumerSecretEncrypted { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsVerified { get; set; } = false;
    public DateTime? LastVerifiedAt { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public string? WebhookSecret { get; set; }
    public string ApiVersion { get; set; } = "wc/v3";

    // ── WordPress Media API (gorsel yukleme icin) ───────────────────────────
    // WooCommerce ck/cs WordPress core API'de calismaz.
    // Gorsel yuklemek icin WordPress kullanici adi + Application Password gerekir.
    // WP Admin → Kullanicilar → Profil → Application Passwords
    public string? WpUsername { get; set; }
    public string? WpAppPasswordEncrypted { get; set; }
}
