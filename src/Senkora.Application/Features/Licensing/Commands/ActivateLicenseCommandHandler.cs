using MediatR;
using Microsoft.Extensions.Logging;
using Senkora.Application.Common.Interfaces;
using Senkora.Application.Common.Models;

namespace Senkora.Application.Features.Licensing.Commands;

public sealed class ActivateLicenseCommandHandler(
    ILicensingService licensingService,
    ILogger<ActivateLicenseCommandHandler> logger)
    : IRequestHandler<ActivateLicenseCommand, Result<string>>
{
    public async Task<Result<string>> Handle(
        ActivateLicenseCommand request, CancellationToken ct)
    {
        var result = await licensingService.ActivateLicenseAsync(
            request.LicenseKey,
            request.Domain,
            request.HardwareFingerprint,
            request.IpAddress,
            ct);

        if (!result.IsSuccess)
            return Result<string>.Failure(result.ErrorMessage!, "ACTIVATION_FAILED");

        logger.LogInformation("License activated: {Key} for {Domain}", request.LicenseKey, request.Domain);
        return Result<string>.Success("Lisans basariyla aktive edildi.");
    }
}
