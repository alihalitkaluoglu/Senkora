using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Senkora.Application.Common.Interfaces;
using Senkora.Domain.Entities.Licensing;
using Senkora.Domain.Enums;
using Senkora.Domain.ValueObjects;
using Senkora.Infrastructure.Caching;
using Senkora.Infrastructure.Persistence;
using System.Security.Cryptography;
using System.Text;

namespace Senkora.Infrastructure.Security;

/// <summary>
/// Lisans olusturma, dogrulama ve ozellikleri yonetir.
/// Lisans anahtari: Base64(HMAC-SHA256(payload)) formatindadir.
/// Uretim ortaminda RSA imzali olacak sekilde genisletilebilir.
/// </summary>
public sealed class LicensingService(
    ApplicationDbContext db,
    RedisCacheService cache,
    ILogger<LicensingService> logger) : ILicensingService
{
    // Lisans imzalama anahtari — uretimde environment variable'dan alinir
    private const string SigningKey = "Senkora_License_Signing_Key_2024_ChangeMeInProduction";
    private const string CachePrefix = "license:";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    // ── Lisans Anahtari Olusturma ──────────────────────────────────────────────
    public async Task<string> GenerateLicenseKeyAsync(
        Guid tenantId, LicenseTier tier,
        DateTime expiresAt, CancellationToken ct = default)
    {
        var features = LicenseFeatures.For(tier);
        var issuedAt = DateTime.UtcNow;

        // 1. Payload olustur
        var payload = new
        {
            tenantId = tenantId.ToString(),
            tier     = tier.ToString(),
            issuedAt = issuedAt.ToString("O"),
            expiresAt= expiresAt.ToString("O"),
            nonce    = Guid.NewGuid().ToString("N")[..8]
        };
        var payloadJson  = JsonConvert.SerializeObject(payload);
        var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);
        var payloadB64   = Convert.ToBase64String(payloadBytes);

        // 2. HMAC-SHA256 imzala
        var keyBytes     = Encoding.UTF8.GetBytes(SigningKey);
        using var hmac   = new HMACSHA256(keyBytes);
        var signBytes    = hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadB64));
        var signature    = Convert.ToBase64String(signBytes)[..16];

        // 3. Okunabilir anahtar: SNKR-XXXX-XXXX-XXXX-XXXX
        var keyRaw  = $"{payloadB64}.{signature}";
        var keyHash = Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes(keyRaw)))[..20]
            .Replace("+","A").Replace("/","B").Replace("=","C")
            .ToUpperInvariant();

        var licenseKey = $"SNKR-{keyHash[..4]}-{keyHash[4..8]}-{keyHash[8..12]}-{keyHash[12..16]}";

        // 4. DB'ye kaydet
        var license = new License
        {
            TenantId           = tenantId,
            LicenseKey         = licenseKey,
            Tier               = tier,
            IssuedAt           = issuedAt,
            ExpiresAt          = expiresAt,
            IsActive           = true,
            MaxWooStores       = features.MaxWooStores == int.MaxValue ? 999 : features.MaxWooStores,
            MaxLogoConnections = features.MaxLogoConnections == int.MaxValue ? 999 : features.MaxLogoConnections,
            MaxMarketplaces    = features.MaxMarketplaces == int.MaxValue ? 999 : features.MaxMarketplaces,
            MaxProductsPerSync = features.MaxProductsPerSync == int.MaxValue ? 999999 : features.MaxProductsPerSync,
            MaxOrdersPerMonth  = features.MaxOrdersPerMonth == int.MaxValue ? 999999 : features.MaxOrdersPerMonth,
            CreatedBy          = "system"
        };

        db.Licenses.Add(license);
        await db.SaveChangesAsync(ct);

        // 5. Tenant'i guncelle
        var tenant = await db.Tenants.FindAsync([tenantId], ct);
        if (tenant is not null)
        {
            tenant.LicenseTier      = tier;
            tenant.LicenseExpiresAt = expiresAt;
            await db.SaveChangesAsync(ct);
        }

        // 6. Cache temizle
        await cache.RemoveAsync($"{CachePrefix}{tenantId}", ct);

        logger.LogInformation(
            "License generated: {Key} for tenant {TenantId} tier {Tier}",
            licenseKey, tenantId, tier);

        return licenseKey;
    }

    // ── Lisans Aktivasyonu ─────────────────────────────────────────────────────
    public async Task<LicenseActivationResult> ActivateLicenseAsync(
        string licenseKey, string domain,
        string hardwareFingerprint, string ipAddress,
        CancellationToken ct = default)
    {
        var license = await db.Licenses
            .Include(l => l.Activations)
            .FirstOrDefaultAsync(l => l.LicenseKey == licenseKey && l.IsActive, ct);

        if (license is null)
            return new LicenseActivationResult(false, null, "Gecersiz lisans anahtari.", null);

        if (license.IsExpired)
            return new LicenseActivationResult(false, null, "Lisans suresi dolmus.", null);

        // Domain kontrolu
        if (!string.IsNullOrEmpty(license.AllowedDomain) &&
            !domain.EndsWith(license.AllowedDomain, StringComparison.OrdinalIgnoreCase))
            return new LicenseActivationResult(false, null,
                $"Bu lisans {license.AllowedDomain} domaini icin gecerlidir.", null);

        // Onceki aktivasyonu guncelle veya yeni ekle
        var existing = license.Activations
            .FirstOrDefault(a => a.Domain == domain || a.HardwareFingerprint == hardwareFingerprint);

        if (existing is not null)
        {
            existing.LastCheckedAt      = DateTime.UtcNow;
            existing.HardwareFingerprint= hardwareFingerprint;
            existing.IsActive           = true;
        }
        else
        {
            db.LicenseActivations.Add(new LicenseActivation
            {
                LicenseId            = license.Id,
                Domain               = domain,
                HardwareFingerprint  = hardwareFingerprint,
                IpAddress            = ipAddress,
                ActivatedAt          = DateTime.UtcNow,
                LastCheckedAt        = DateTime.UtcNow,
                IsActive             = true,
                CreatedBy            = "system"
            });
        }

        await db.SaveChangesAsync(ct);
        await cache.RemoveAsync($"{CachePrefix}{license.TenantId}", ct);

        return new LicenseActivationResult(true, licenseKey, null, license.Id);
    }

    // ── Lisans Kontrolu ────────────────────────────────────────────────────────
    public async Task<LicenseCheckResult> CheckLicenseAsync(
        Guid tenantId, CancellationToken ct = default)
    {
        // Cache'den bak
        var cached = await cache.GetAsync<LicenseCheckResult>($"{CachePrefix}{tenantId}", ct);
        if (cached is not null) return cached;

        // Tenant bul
        var tenant = await db.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId && !t.IsDeleted, ct);

        if (tenant is null)
            return new LicenseCheckResult(false, LicenseTier.Trial, null,
                "Tenant bulunamadi.", false, false, 0);

        // System tenant — her zaman gecerli
        if (tenantId == new Guid("00000000-0000-0000-0000-000000000001"))
        {
            var systemResult = new LicenseCheckResult(
                true, LicenseTier.Enterprise, DateTime.UtcNow.AddYears(99),
                null, false, false, 99 * 365);
            await cache.SetAsync($"{CachePrefix}{tenantId}", systemResult, CacheTtl, ct);
            return systemResult;
        }

        // Aktif lisans bul
        var license = await db.Licenses
            .AsNoTracking()
            .Where(l => l.TenantId == tenantId && l.IsActive && !l.IsDeleted)
            .OrderByDescending(l => l.ExpiresAt)
            .FirstOrDefaultAsync(ct);

        LicenseCheckResult result;

        if (license is null)
        {
            // Lisans yok — trial mode
            var trialExpiry   = tenant.CreatedAt.AddDays(30);
            var trialExpired  = DateTime.UtcNow > trialExpiry;
            var trialDays     = Math.Max(0, (int)(trialExpiry - DateTime.UtcNow).TotalDays);

            result = new LicenseCheckResult(
                IsValid:       !trialExpired,
                Tier:          LicenseTier.Trial,
                ExpiresAt:     trialExpiry,
                ErrorMessage:  trialExpired ? "Trial suresi doldu." : null,
                IsExpired:     trialExpired,
                IsTrialMode:   true,
                DaysRemaining: trialDays);
        }
        else
        {
            var daysLeft  = Math.Max(0, (int)(license.ExpiresAt - DateTime.UtcNow).TotalDays);
            var isExpired = license.IsExpired;

            // 72 saatlik grace period
            var gracePeriod     = license.ExpiresAt.AddHours(72);
            var inGracePeriod   = !isExpired || DateTime.UtcNow <= gracePeriod;

            result = new LicenseCheckResult(
                IsValid:       !isExpired || inGracePeriod,
                Tier:          license.Tier,
                ExpiresAt:     license.ExpiresAt,
                ErrorMessage:  isExpired ? "Lisans suresi doldu." : null,
                IsExpired:     isExpired,
                IsTrialMode:   false,
                DaysRemaining: daysLeft);
        }

        await cache.SetAsync($"{CachePrefix}{tenantId}", result, CacheTtl, ct);
        return result;
    }

    // ── Ozellik Seti ──────────────────────────────────────────────────────────
    public async Task<LicenseFeatures> GetFeaturesAsync(
        Guid tenantId, CancellationToken ct = default)
    {
        var check = await CheckLicenseAsync(tenantId, ct);
        return LicenseFeatures.For(check.Tier);
    }
}
