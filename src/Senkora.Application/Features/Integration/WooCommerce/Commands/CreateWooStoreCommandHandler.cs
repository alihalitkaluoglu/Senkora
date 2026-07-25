using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Senkora.Application.Common.Interfaces;
using Senkora.Application.Common.Models;
using Senkora.Domain.Entities.Integration;
using Senkora.Domain.Interfaces.Services;

namespace Senkora.Application.Features.Integration.WooCommerce.Commands;

public sealed class CreateWooStoreCommandHandler(
    IApplicationDbContext db,
    IEncryptionService encryption,
    ILogger<CreateWooStoreCommandHandler> logger)
    : IRequestHandler<CreateWooStoreCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateWooStoreCommand request, CancellationToken ct)
    {
        var exists = await db.WooStores.AnyAsync(
            s => s.TenantId == request.TenantId &&
                 s.StoreUrl == request.StoreUrl.TrimEnd('/') &&
                 !s.IsDeleted, ct);

        if (exists)
            return Result<Guid>.Failure(
                "Bu URL ile zaten bir magaza tanimli.", "DUPLICATE_STORE");

        var store = new WooStore
        {
            TenantId                = request.TenantId,
            Name                    = request.Name,
            StoreUrl                = request.StoreUrl.TrimEnd('/'),
            ConsumerKeyEncrypted    = encryption.Encrypt(request.ConsumerKey),
            ConsumerSecretEncrypted = encryption.Encrypt(request.ConsumerSecret),
            IsActive                = true,
            IsVerified              = false,
            WpUsername              = request.WpUsername,
            PriceProjectCode        = Norm(request.PriceProjectCode),
            PriceTradingGroupCode   = Norm(request.PriceTradingGroupCode),
            PriceCostCenterCode     = Norm(request.PriceCostCenterCode),
            WpAppPasswordEncrypted  = string.IsNullOrWhiteSpace(request.WpAppPassword)
                ? null : encryption.Encrypt(request.WpAppPassword),
            CreatedBy               = request.TenantId.ToString()
        };

        db.WooStores.Add(store);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "WooStore created: {Id} for tenant {TenantId}", store.Id, request.TenantId);

        return Result<Guid>.Success(store.Id);
    }

    private static string? Norm(string? v)
        => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
}
