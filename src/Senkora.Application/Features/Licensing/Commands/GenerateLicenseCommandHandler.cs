using MediatR;
using Microsoft.Extensions.Logging;
using Senkora.Application.Common.Interfaces;
using Senkora.Application.Common.Models;

namespace Senkora.Application.Features.Licensing.Commands;

public sealed class GenerateLicenseCommandHandler(
    ILicensingService licensingService,
    IApplicationDbContext db,
    ILogger<GenerateLicenseCommandHandler> logger)
    : IRequestHandler<GenerateLicenseCommand, Result<GenerateLicenseResult>>
{
    public async Task<Result<GenerateLicenseResult>> Handle(
        GenerateLicenseCommand request, CancellationToken ct)
    {
        var tenant = await db.Tenants.FindAsync([request.TenantId], ct);
        if (tenant is null)
            return Result<GenerateLicenseResult>.Failure(
                "Tenant bulunamadi.", "TENANT_NOT_FOUND");

        var expiresAt  = DateTime.UtcNow.AddDays(request.DurationDays);
        var licenseKey = await licensingService.GenerateLicenseKeyAsync(
            request.TenantId, request.Tier, expiresAt, ct);

        logger.LogInformation(
            "License generated for tenant {TenantId} tier {Tier} expires {ExpiresAt}",
            request.TenantId, request.Tier, expiresAt);

        // License entity DB'ye ILicensingService implementasyonu tarafindan kaydedilir
        return Result<GenerateLicenseResult>.Success(new GenerateLicenseResult(
            LicenseKey: licenseKey,
            LicenseId:  Guid.NewGuid(), // gercek ID service'ten gelir
            ExpiresAt:  expiresAt,
            Tier:       request.Tier.ToString()));
    }
}
