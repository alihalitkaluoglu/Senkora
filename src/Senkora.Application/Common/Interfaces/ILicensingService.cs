using Senkora.Domain.Enums;
using Senkora.Domain.ValueObjects;

namespace Senkora.Application.Common.Interfaces;

public interface ILicensingService
{
    /// <summary>Tenant lisansinin gecerli olup olmadigini kontrol eder.</summary>
    Task<LicenseCheckResult> CheckLicenseAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>Lisans anahtari olusturur (SuperAdmin).</summary>
    Task<string> GenerateLicenseKeyAsync(
        Guid tenantId, LicenseTier tier,
        DateTime expiresAt, CancellationToken ct = default);

    /// <summary>Lisansi aktive eder.</summary>
    Task<LicenseActivationResult> ActivateLicenseAsync(
        string licenseKey, string domain,
        string hardwareFingerprint, string ipAddress,
        CancellationToken ct = default);

    /// <summary>Tenant icin aktif ozellik setini dondurur.</summary>
    Task<LicenseFeatures> GetFeaturesAsync(Guid tenantId, CancellationToken ct = default);
}

public sealed record LicenseCheckResult(
    bool          IsValid,
    LicenseTier   Tier,
    DateTime?     ExpiresAt,
    string?       ErrorMessage,
    bool          IsExpired,
    bool          IsTrialMode,
    int           DaysRemaining);

public sealed record LicenseActivationResult(
    bool   IsSuccess,
    string? LicenseKey,
    string? ErrorMessage,
    Guid?  LicenseId);
