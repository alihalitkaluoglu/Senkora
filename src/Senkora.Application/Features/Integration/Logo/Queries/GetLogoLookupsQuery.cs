using MediatR;
using Senkora.Application.Common.Interfaces;
using Senkora.Application.Common.Models;

namespace Senkora.Application.Features.Integration.Logo.Queries;

public sealed record GetLogoLookupsQuery(
    Guid TenantId,
    Guid LogoConnectionId) : IRequest<Result<LogoLookupResult>>;

public sealed class GetLogoLookupsQueryHandler(
    ILogoConnectionResolver resolver,
    ILogoLookupService lookup)
    : IRequestHandler<GetLogoLookupsQuery, Result<LogoLookupResult>>
{
    public async Task<Result<LogoLookupResult>> Handle(
        GetLogoLookupsQuery request, CancellationToken ct)
    {
        LogoConnectionInfo conn;
        try
        {
            conn = await resolver.ResolveAsync(request.TenantId, request.LogoConnectionId, ct);
        }
        catch (Exception ex)
        {
            return Result<LogoLookupResult>.Failure(
                $"Logo baglantisi kurulamadi: {ex.Message}", "CONNECTION_FAILED");
        }

        var result = await lookup.GetAllAsync(conn.RestUrl, conn.AccessToken, conn.FirmNo, ct);
        return Result<LogoLookupResult>.Success(result);
    }
}
