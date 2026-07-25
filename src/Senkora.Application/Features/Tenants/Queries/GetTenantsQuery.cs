using MediatR;
using Microsoft.EntityFrameworkCore;
using Senkora.Application.Common.Interfaces;
using Senkora.Application.Common.Models;

namespace Senkora.Application.Features.Tenants.Queries;

public sealed record GetTenantsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null) : IRequest<Result<PagedResult<TenantDto>>>;

public sealed record TenantDto(
    Guid Id,
    string Name,
    string Subdomain,
    string ContactEmail,
    bool IsActive,
    string LicenseTier,
    DateTime CreatedAt);

public sealed class GetTenantsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetTenantsQuery, Result<PagedResult<TenantDto>>>
{
    public async Task<Result<PagedResult<TenantDto>>> Handle(GetTenantsQuery request, CancellationToken ct)
    {
        var query = db.Tenants.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(t =>
                t.Name.Contains(request.Search) ||
                t.Subdomain.Contains(request.Search));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new TenantDto(
                t.Id, t.Name, t.Subdomain, t.ContactEmail,
                t.IsActive, t.LicenseTier.ToString(), t.CreatedAt))
            .ToListAsync(ct);

        return Result<PagedResult<TenantDto>>.Success(
            PagedResult<TenantDto>.Create(items, total, request.Page, request.PageSize));
    }
}
