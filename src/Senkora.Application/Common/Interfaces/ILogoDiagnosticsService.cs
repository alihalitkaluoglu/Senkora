using Senkora.Application.Features.Products.Queries;

namespace Senkora.Application.Common.Interfaces;

public interface ILogoDiagnosticsService
{
    Task<LogoFetchDiagnostics> ProbeAsync(
        string restUrl, string accessToken,
        int firmNo, int periodNo, int limit,
        CancellationToken ct = default);
}
