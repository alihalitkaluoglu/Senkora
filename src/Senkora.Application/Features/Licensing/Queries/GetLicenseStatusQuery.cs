using MediatR;
using Senkora.Application.Common.Interfaces;
using Senkora.Application.Common.Models;

namespace Senkora.Application.Features.Licensing.Queries;

public sealed record GetLicenseStatusQuery(Guid TenantId)
    : IRequest<Result<LicenseStatusDto>>;

public sealed record LicenseStatusDto(
    bool     IsValid,
    string   Tier,
    DateTime? ExpiresAt,
    int      DaysRemaining,
    bool     IsExpired,
    bool     IsTrialMode,
    // Features
    int      MaxWooStores,
    int      MaxLogoConnections,
    int      MaxMarketplaces,
    bool     RealtimeSync,
    bool     WebhookSupport,
    bool     AdvancedReporting,
    int      SyncIntervalMinutes);

public sealed class GetLicenseStatusQueryHandler(
    ILicensingService licensingService)
    : IRequestHandler<GetLicenseStatusQuery, Result<LicenseStatusDto>>
{
    public async Task<Result<LicenseStatusDto>> Handle(
        GetLicenseStatusQuery request, CancellationToken ct)
    {
        var check    = await licensingService.CheckLicenseAsync(request.TenantId, ct);
        var features = await licensingService.GetFeaturesAsync(request.TenantId, ct);

        return Result<LicenseStatusDto>.Success(new LicenseStatusDto(
            IsValid:              check.IsValid,
            Tier:                 check.Tier.ToString(),
            ExpiresAt:            check.ExpiresAt,
            DaysRemaining:        check.DaysRemaining,
            IsExpired:            check.IsExpired,
            IsTrialMode:          check.IsTrialMode,
            MaxWooStores:         features.MaxWooStores,
            MaxLogoConnections:   features.MaxLogoConnections,
            MaxMarketplaces:      features.MaxMarketplaces,
            RealtimeSync:         features.RealtimeSync,
            WebhookSupport:       features.WebhookSupport,
            AdvancedReporting:    features.AdvancedReporting,
            SyncIntervalMinutes:  features.SyncIntervalMinutes));
    }
}
