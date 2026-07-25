using MediatR;
using Microsoft.EntityFrameworkCore;
using Senkora.Application.Common.Interfaces;
using Senkora.Application.Common.Models;

namespace Senkora.Application.Features.Products.Queries;

public sealed record GetProductHistoryQuery(
    Guid TenantId,
    Guid ProductMappingId) : IRequest<Result<List<ProductHistoryDto>>>;

public sealed record ProductHistoryDto(
    Guid     Id,
    string   Action,
    bool     IsSuccess,
    string?  Message,
    string?  ChangesJson,
    long?    WooProductId,
    int      DurationMs,
    string?  PerformedBy,
    DateTime CreatedAt);

public sealed class GetProductHistoryQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetProductHistoryQuery, Result<List<ProductHistoryDto>>>
{
    public async Task<Result<List<ProductHistoryDto>>> Handle(
        GetProductHistoryQuery request, CancellationToken ct)
    {
        var list = await db.ProductSyncHistories
            .AsNoTracking()
            .Where(h => h.TenantId == request.TenantId
                     && h.ProductMappingId == request.ProductMappingId)
            .OrderByDescending(h => h.CreatedAt)
            .Take(100)
            .Select(h => new ProductHistoryDto(
                h.Id, h.Action, h.IsSuccess, h.Message, h.ChangesJson,
                h.WooProductId, h.DurationMs, h.PerformedBy, h.CreatedAt))
            .ToListAsync(ct);

        return Result<List<ProductHistoryDto>>.Success(list);
    }
}
