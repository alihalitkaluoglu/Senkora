using MediatR;
using Microsoft.EntityFrameworkCore;
using Senkora.Application.Common.Interfaces;
using Senkora.Application.Common.Models;
using Senkora.Domain.Entities.Tenants;
using Senkora.Domain.Enums;

namespace Senkora.Application.Features.Tenants.Commands;

public sealed class CreateTenantCommandHandler(IApplicationDbContext db)
    : IRequestHandler<CreateTenantCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateTenantCommand request, CancellationToken ct)
    {
        var exists = await db.Tenants
            .AnyAsync(t => t.Subdomain == request.Subdomain.ToLowerInvariant(), ct);

        if (exists)
            return Result<Guid>.Failure(
                $"'{request.Subdomain}' subdomaini zaten kullaniliyor.", "SUBDOMAIN_TAKEN");

        var (maxWoo, maxLogo, maxMarket) = request.InitialTier switch
        {
            LicenseTier.Trial        => (1, 1, 0),
            LicenseTier.Starter      => (1, 1, 0),
            LicenseTier.Professional => (3, 2, 2),
            LicenseTier.Enterprise   => (99, 99, 99),
            _                        => (1, 1, 0)
        };

        var tenant = new Tenant
        {
            Name               = request.Name,
            Subdomain          = request.Subdomain.ToLowerInvariant(),
            ContactEmail       = request.ContactEmail,
            ContactPhone       = request.ContactPhone,
            IsActive           = true,
            LicenseTier        = request.InitialTier,
            MaxWooStores       = maxWoo,
            MaxLogoConnections = maxLogo,
            MaxMarketplaces    = maxMarket,
            CreatedBy          = "system"
        };

        db.Tenants.Add(tenant);
        await db.SaveChangesAsync(ct);
        return Result<Guid>.Success(tenant.Id);
    }
}
