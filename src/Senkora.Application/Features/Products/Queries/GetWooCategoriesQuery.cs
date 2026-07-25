using MediatR;
using Senkora.Application.Common.Interfaces;
using Senkora.Application.Common.Models;

namespace Senkora.Application.Features.Products.Queries;

public sealed record GetWooCategoriesQuery(
    Guid TenantId,
    Guid WooStoreId) : IRequest<Result<List<WooCategoryDto>>>;

public sealed class GetWooCategoriesQueryHandler(
    IWooConnectionResolver wooResolver,
    IWooProductService wooService)
    : IRequestHandler<GetWooCategoriesQuery, Result<List<WooCategoryDto>>>
{
    public async Task<Result<List<WooCategoryDto>>> Handle(
        GetWooCategoriesQuery request, CancellationToken ct)
    {
        WooConnectionInfo info;
        try
        {
            info = await wooResolver.ResolveAsync(
                request.TenantId, request.WooStoreId, ct);
        }
        catch (Exception ex)
        {
            return Result<List<WooCategoryDto>>.Failure(
                $"WooCommerce baglantisi kurulamadi: {ex.Message}", "CONNECTION_FAILED");
        }

        var cats = await wooService.GetCategoriesAsync(
            info.StoreUrl, info.ConsumerKey, info.ConsumerSecret, ct);

        return Result<List<WooCategoryDto>>.Success(cats);
    }
}
