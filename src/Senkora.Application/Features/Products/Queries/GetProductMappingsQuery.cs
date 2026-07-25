using MediatR;
using Microsoft.EntityFrameworkCore;
using Senkora.Application.Common.Interfaces;
using Senkora.Application.Common.Models;
using Senkora.Domain.Enums;

namespace Senkora.Application.Features.Products.Queries;

public sealed record GetProductMappingsQuery(
    Guid    TenantId,
    Guid?   WooStoreId      = null,
    string? Status          = null,
    string? Search          = null,
    int     Page            = 1,
    int     PageSize        = 50) : IRequest<Result<PagedResult<ProductMappingDto>>>;

public sealed record ProductMappingDto(
    Guid     Id,
    long     LogoItemRef,
    string   LogoItemCode,
    string   LogoItemName,
    string?  LogoGroupCode,
    decimal  LogoSellPrice,
    decimal  LogoStock,
    string   Status,
    long?    WooProductId,
    string?  WooSku,
    DateTime? LastSyncedAt,
    string?  LastSyncError,
    bool     HasEnrichment,
    bool     HasImages,
    int      ImageCount,
    DateTime LogoLastFetched);

public sealed class GetProductMappingsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetProductMappingsQuery, Result<PagedResult<ProductMappingDto>>>
{
    public async Task<Result<PagedResult<ProductMappingDto>>> Handle(
        GetProductMappingsQuery request, CancellationToken ct)
    {
        var query = db.ProductMappings
            .AsNoTracking()
            .Where(p => p.TenantId == request.TenantId);

        if (request.WooStoreId.HasValue)
            query = query.Where(p => p.WooStoreId == request.WooStoreId.Value);

        if (!string.IsNullOrWhiteSpace(request.Status)
            && Enum.TryParse<SyncMappingStatus>(request.Status, out var status))
            query = query.Where(p => p.Status == status);

        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(p =>
                p.LogoItemCode.Contains(request.Search) ||
                p.LogoItemName.Contains(request.Search));

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var dtos = items.Select(p => new ProductMappingDto(
            Id:              p.Id,
            LogoItemRef:     p.LogoItemRef,
            LogoItemCode:    p.LogoItemCode,
            LogoItemName:    p.LogoItemName,
            LogoGroupCode:   p.LogoGroupCode,
            LogoSellPrice:   p.LogoSellPrice,
            LogoStock:       p.LogoStock,
            Status:          p.Status.ToString(),
            WooProductId:    p.WooProductId,
            WooSku:          p.WooSku,
            LastSyncedAt:    p.LastSyncedAt,
            LastSyncError:   p.LastSyncError,
            HasEnrichment:   p.EnrichmentJson != null,
            HasImages:       p.EnrichmentJson != null &&
                             p.EnrichmentJson.Contains("\"StoredPath\""),
            ImageCount:      p.EnrichmentJson != null
                             ? CountImages(p.EnrichmentJson) : 0,
            LogoLastFetched: p.LogoLastFetched
        )).ToList();

        return Result<PagedResult<ProductMappingDto>>.Success(
            PagedResult<ProductMappingDto>.Create(items: dtos,
                totalCount: total, page: request.Page, pageSize: request.PageSize));
    }

    private static int CountImages(string json)
    {
        try { return Newtonsoft.Json.JsonConvert
            .DeserializeObject<Senkora.Domain.ValueObjects.ProductEnrichment>(json)
            ?.Images.Count ?? 0; }
        catch { return 0; }
    }
}
