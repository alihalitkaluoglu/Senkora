using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Senkora.Application.Common.Interfaces;
using Senkora.Application.Common.Models;
using Senkora.Domain.ValueObjects;

namespace Senkora.Application.Features.Products.Commands;

public sealed record UploadProductImageCommand(
    Guid      TenantId,
    Guid      ProductMappingId,
    IFormFile File,
    string    Extension) : IRequest<Result<string>>;

public sealed class UploadProductImageCommandHandler(
    IApplicationDbContext db,
    IFileStorageService fileStorage)
    : IRequestHandler<UploadProductImageCommand, Result<string>>
{
    public async Task<Result<string>> Handle(
        UploadProductImageCommand request, CancellationToken ct)
    {
        var mapping = await db.ProductMappings
            .FirstOrDefaultAsync(p => p.Id == request.ProductMappingId
                && p.TenantId == request.TenantId, ct);

        if (mapping is null)
            return Result<string>.Failure("Ürün bulunamadı.", "NOT_FOUND");

        // Dosyayı kaydet
        var fileName = $"{request.TenantId}/{request.ProductMappingId}/{Guid.NewGuid()}{request.Extension}";
        var storedPath = await fileStorage.SaveAsync(fileName, request.File.OpenReadStream(), ct);

        // Enrichment'a ekle
        var enrichment = mapping.EnrichmentJson is not null
            ? JsonConvert.DeserializeObject<ProductEnrichment>(mapping.EnrichmentJson)
              ?? new ProductEnrichment()
            : new ProductEnrichment();

        var isFirst = !enrichment.Images.Any();
        enrichment.Images.Add(new ProductImage
        {
            StoredPath = storedPath,
            Alt        = mapping.LogoItemName,
            IsFeatured = isFirst,
            SortOrder  = enrichment.Images.Count
        });

        mapping.EnrichmentJson = JsonConvert.SerializeObject(enrichment);
        await db.SaveChangesAsync(ct);

        return Result<string>.Success(storedPath);
    }
}
