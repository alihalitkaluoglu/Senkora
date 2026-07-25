using MediatR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Senkora.Application.Common.Interfaces;
using Senkora.Application.Common.Models;
using Senkora.Domain.ValueObjects;

namespace Senkora.Application.Features.Products.Queries;

public sealed record GetProductEnrichmentQuery(
    Guid TenantId,
    Guid ProductMappingId) : IRequest<Result<ProductEnrichmentDto>>;

public sealed record ProductEnrichmentDto(
    Guid              MappingId,
    string            LogoItemCode,
    string            LogoItemName,
    decimal           LogoSellPrice,
    decimal           LogoSellPrice2,
    decimal           LogoVatRate,
    decimal           LogoStock,
    decimal           LogoWeight,
    string?           LogoGroupCode,
    string?           LogoDescription,
    string?           LogoAuxDesc,
    ProductEnrichment Enrichment);

public sealed class GetProductEnrichmentQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetProductEnrichmentQuery, Result<ProductEnrichmentDto>>
{
    public async Task<Result<ProductEnrichmentDto>> Handle(
        GetProductEnrichmentQuery request, CancellationToken ct)
    {
        var mapping = await db.ProductMappings
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.ProductMappingId
                && p.TenantId == request.TenantId, ct);

        if (mapping is null)
            return Result<ProductEnrichmentDto>.Failure("Ürün bulunamadı.", "NOT_FOUND");

        var enrichment = mapping.EnrichmentJson is not null
            ? JsonConvert.DeserializeObject<ProductEnrichment>(mapping.EnrichmentJson)
              ?? new ProductEnrichment()
            : new ProductEnrichment();

        return Result<ProductEnrichmentDto>.Success(new ProductEnrichmentDto(
            MappingId:       mapping.Id,
            LogoItemCode:    mapping.LogoItemCode,
            LogoItemName:    mapping.LogoItemName,
            LogoSellPrice:   mapping.LogoSellPrice,
            LogoSellPrice2:  mapping.LogoSellPrice2,
            LogoVatRate:     mapping.LogoVatRate,
            LogoStock:       mapping.LogoStock,
            LogoWeight:      mapping.LogoWeight,
            LogoGroupCode:   mapping.LogoGroupCode,
            LogoDescription: mapping.LogoDescription,
            LogoAuxDesc:     mapping.LogoAuxDesc,
            Enrichment:      enrichment));
    }
}
