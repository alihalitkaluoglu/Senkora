using MediatR;
using Senkora.Application.Common.Models;
using Senkora.Domain.Enums;

namespace Senkora.Application.Features.Licensing.Commands;

public sealed record GenerateLicenseCommand(
    Guid        TenantId,
    LicenseTier Tier,
    int         DurationDays = 365,
    int         MaxWooStores = 1,
    int         MaxLogoConnections = 1,
    int         MaxMarketplaces = 0,
    string?     AllowedDomain = null)
    : IRequest<Result<GenerateLicenseResult>>;

public sealed record GenerateLicenseResult(
    string   LicenseKey,
    Guid     LicenseId,
    DateTime ExpiresAt,
    string   Tier);
