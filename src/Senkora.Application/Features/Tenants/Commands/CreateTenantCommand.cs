using MediatR;
using Senkora.Application.Common.Models;
using Senkora.Domain.Enums;

namespace Senkora.Application.Features.Tenants.Commands;

public sealed record CreateTenantCommand(
    string Name,
    string Subdomain,
    string ContactEmail,
    string ContactPhone,
    LicenseTier InitialTier = LicenseTier.Trial) : IRequest<Result<Guid>>;
