using MediatR;
using Senkora.Application.Common.Models;

namespace Senkora.Application.Features.Licensing.Commands;

public sealed record ActivateLicenseCommand(
    string LicenseKey,
    string Domain,
    string HardwareFingerprint,
    string IpAddress)
    : IRequest<Result<string>>;
