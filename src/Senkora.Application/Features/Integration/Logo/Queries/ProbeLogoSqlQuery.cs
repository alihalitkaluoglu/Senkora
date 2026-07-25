using MediatR;
using Senkora.Application.Common.Interfaces;
using Senkora.Application.Common.Models;

namespace Senkora.Application.Features.Integration.Logo.Queries;

/// <summary>
/// Logo REST'in hangi SQL cagri bicimini kabul ettigini tespit eder.
/// Tum bilinen endpoint/parametre kombinasyonlarini dener ve sonuclari raporlar.
/// </summary>
public sealed record ProbeLogoSqlQuery(
    Guid    TenantId,
    Guid    LogoConnectionId,
    string? CustomSql = null) : IRequest<Result<List<LogoSqlProbe>>>;

public sealed class ProbeLogoSqlQueryHandler(
    ILogoConnectionResolver resolver,
    ILogoSqlService sqlService)
    : IRequestHandler<ProbeLogoSqlQuery, Result<List<LogoSqlProbe>>>
{
    public async Task<Result<List<LogoSqlProbe>>> Handle(
        ProbeLogoSqlQuery request, CancellationToken ct)
    {
        LogoConnectionInfo conn;
        try
        {
            conn = await resolver.ResolveAsync(request.TenantId, request.LogoConnectionId, ct);
        }
        catch (Exception ex)
        {
            return Result<List<LogoSqlProbe>>.Failure(
                $"Logo baglantisi kurulamadi: {ex.Message}", "CONNECTION_FAILED");
        }

        // Varsayilan olarak kesin var olan bir tabloya basit sorgu
        var sql = string.IsNullOrWhiteSpace(request.CustomSql)
            ? $"SELECT TOP 3 LOGICALREF, CODE FROM LG_{conn.FirmNo:D3}_ITEMS"
            : request.CustomSql;

        var probes = await sqlService.ProbeAllAsync(conn.RestUrl, conn.AccessToken, sql, ct);
        return Result<List<LogoSqlProbe>>.Success(probes);
    }
}
