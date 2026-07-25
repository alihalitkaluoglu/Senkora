using MediatR;
using Microsoft.EntityFrameworkCore;
using Senkora.Application.Common.Interfaces;
using Senkora.Application.Common.Models;

namespace Senkora.Application.Features.Integration.Logo.Queries;

public sealed record GetLogoConnectionsQuery(Guid TenantId)
    : IRequest<Result<List<LogoConnectionDto>>>;

public sealed record LogoConnectionDto(
    Guid      Id,
    string    Name,
    string    RestUrl,
    string    Username,
    int       FirmNo,
    int       PeriodNo,
    bool      IsActive,
    bool      IsVerified,
    DateTime? LastVerifiedAt,
    DateTime? LastSyncAt,
    int       TimeoutSeconds,
    bool      HasCachedToken);

public sealed class GetLogoConnectionsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetLogoConnectionsQuery, Result<List<LogoConnectionDto>>>
{
    public async Task<Result<List<LogoConnectionDto>>> Handle(
        GetLogoConnectionsQuery request, CancellationToken ct)
    {
        var list = await db.LogoConnections
            .AsNoTracking()
            .Where(c => c.TenantId == request.TenantId)
            .OrderBy(c => c.Name)
            .Select(c => new LogoConnectionDto(
                c.Id, c.Name, c.RestUrl, c.Username,
                c.FirmNo, c.PeriodNo, c.IsActive, c.IsVerified,
                c.LastVerifiedAt, c.LastSyncAt, c.TimeoutSeconds,
                c.CachedTokenEncrypted != null))
            .ToListAsync(ct);

        return Result<List<LogoConnectionDto>>.Success(list);
    }
}
