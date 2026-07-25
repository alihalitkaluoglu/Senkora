using MediatR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Senkora.Application.Common.Interfaces;
using Senkora.Application.Common.Models;
using Senkora.Domain.Enums;
using Senkora.Domain.ValueObjects;

namespace Senkora.Application.Features.Products.Commands;

/// <summary>
/// Portal'da yapılan zenginleştirme verilerini kaydeder.
/// Status → Enriched olur (gönderime hazır).
/// </summary>
public sealed record EnrichProductCommand(
    Guid               TenantId,
    Guid               ProductMappingId,
    ProductEnrichment  Enrichment) : IRequest<Result>;

public sealed class EnrichProductCommandHandler(IApplicationDbContext db)
    : IRequestHandler<EnrichProductCommand, Result>
{
    public async Task<Result> Handle(EnrichProductCommand request, CancellationToken ct)
    {
        var mapping = await db.ProductMappings
            .FirstOrDefaultAsync(p => p.Id == request.ProductMappingId
                && p.TenantId == request.TenantId, ct);

        if (mapping is null)
            return Result.Failure("Ürün eşlemesi bulunamadı.", "NOT_FOUND");

        mapping.EnrichmentJson = JsonConvert.SerializeObject(request.Enrichment);
        mapping.Status         = SyncMappingStatus.Enriched;

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
