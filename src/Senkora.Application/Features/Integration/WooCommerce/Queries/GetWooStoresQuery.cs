using MediatR;
using Microsoft.EntityFrameworkCore;
using Senkora.Application.Common.Interfaces;
using Senkora.Application.Common.Models;

namespace Senkora.Application.Features.Integration.WooCommerce.Queries;

public sealed record GetWooStoresQuery(Guid TenantId)
    : IRequest<Result<List<WooStoreDto>>>;

public sealed record WooStoreDto(
    Guid      Id,
    string    Name,
    string    StoreUrl,
    string    ApiVersion,
    bool      IsActive,
    bool      IsVerified,
    DateTime? LastVerifiedAt,
    DateTime? LastSyncAt,
    string?   WpUsername,
    bool      HasWpCredentials,
    string?   PriceProjectCode,
    string?   PriceTradingGroupCode,
    string?   PriceCostCenterCode);

public sealed class GetWooStoresQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetWooStoresQuery, Result<List<WooStoreDto>>>
{
    public async Task<Result<List<WooStoreDto>>> Handle(
        GetWooStoresQuery request, CancellationToken ct)
    {
        var list = await db.WooStores
            .AsNoTracking()
            .Where(s => s.TenantId == request.TenantId)
            .OrderBy(s => s.Name)
            .Select(s => new WooStoreDto(
                s.Id, s.Name, s.StoreUrl, s.ApiVersion,
                s.IsActive, s.IsVerified, s.LastVerifiedAt, s.LastSyncAt,
                s.WpUsername, s.WpAppPasswordEncrypted != null,
                s.PriceProjectCode, s.PriceTradingGroupCode, s.PriceCostCenterCode))
            .ToListAsync(ct);

        return Result<List<WooStoreDto>>.Success(list);
    }
}
