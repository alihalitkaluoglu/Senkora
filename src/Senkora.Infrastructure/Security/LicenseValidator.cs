using Senkora.Application.Common.Interfaces;
using Senkora.Domain.Enums;
using Senkora.Domain.Interfaces.Services;

namespace Senkora.Infrastructure.Security;

/// <summary>Domain interface implementasyonu — basit delegate</summary>
public sealed class LicenseValidator(ILicensingService licensingService) : ILicenseValidator
{
    public async Task<bool> ValidateAsync(Guid tenantId, CancellationToken ct = default)
    {
        var result = await licensingService.CheckLicenseAsync(tenantId, ct);
        return result.IsValid;
    }

    public async Task<bool> HasFeatureAsync(
        Guid tenantId, LicenseTier minimumTier, CancellationToken ct = default)
    {
        var result = await licensingService.CheckLicenseAsync(tenantId, ct);
        return result.IsValid && result.Tier >= minimumTier;
    }

    public async Task<bool> CanAddConnectionAsync(
        Guid tenantId, ConnectorType type, CancellationToken ct = default)
    {
        var features = await licensingService.GetFeaturesAsync(tenantId, ct);
        return type switch
        {
            ConnectorType.WooCommerce => features.MaxWooStores > 0,
            ConnectorType.LogoErp     => features.MaxLogoConnections > 0,
            _                         => features.MaxMarketplaces > 0
        };
    }
}
