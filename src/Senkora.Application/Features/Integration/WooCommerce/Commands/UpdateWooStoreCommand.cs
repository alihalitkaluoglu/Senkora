using MediatR;
using Microsoft.EntityFrameworkCore;
using Senkora.Application.Common.Interfaces;
using Senkora.Application.Common.Models;
using Senkora.Domain.Interfaces.Services;

namespace Senkora.Application.Features.Integration.WooCommerce.Commands;

public sealed record UpdateWooStoreCommand(
    Guid    Id,
    Guid    TenantId,
    string  Name,
    string  StoreUrl,
    string? ConsumerKey,
    string? ConsumerSecret,
    bool    IsActive,
    string? WpUsername            = null,
    string? WpAppPassword         = null,
    string? PriceProjectCode      = null,
    string? PriceTradingGroupCode = null,
    string? PriceCostCenterCode   = null) : IRequest<Result>;

public sealed class UpdateWooStoreCommandHandler(
    IApplicationDbContext db,
    IEncryptionService encryption)
    : IRequestHandler<UpdateWooStoreCommand, Result>
{
    public async Task<Result> Handle(UpdateWooStoreCommand request, CancellationToken ct)
    {
        var store = await db.WooStores.FirstOrDefaultAsync(
            s => s.Id == request.Id && s.TenantId == request.TenantId, ct);

        if (store is null)
            return Result.Failure("Magaza bulunamadi.", "NOT_FOUND");

        store.Name     = request.Name;
        store.StoreUrl = request.StoreUrl.TrimEnd('/');
        store.IsActive = request.IsActive;

        if (!string.IsNullOrWhiteSpace(request.ConsumerKey))
            store.ConsumerKeyEncrypted = encryption.Encrypt(request.ConsumerKey.Trim());
        if (!string.IsNullOrWhiteSpace(request.ConsumerSecret))
            store.ConsumerSecretEncrypted = encryption.Encrypt(request.ConsumerSecret.Trim());

        // WordPress medya kimlik bilgileri
        if (request.WpUsername is not null)
            store.WpUsername = string.IsNullOrWhiteSpace(request.WpUsername)
                ? null : request.WpUsername.Trim();

        if (!string.IsNullOrWhiteSpace(request.WpAppPassword))
            store.WpAppPasswordEncrypted = encryption.Encrypt(request.WpAppPassword.Trim());

        // Fiyat secim kriterleri — bos gonderilirse temizlenir
        if (request.PriceProjectCode is not null)
            store.PriceProjectCode = Normalize(request.PriceProjectCode);
        if (request.PriceTradingGroupCode is not null)
            store.PriceTradingGroupCode = Normalize(request.PriceTradingGroupCode);
        if (request.PriceCostCenterCode is not null)
            store.PriceCostCenterCode = Normalize(request.PriceCostCenterCode);

        store.IsVerified     = false;
        store.LastVerifiedAt = null;

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    private static string? Normalize(string value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
