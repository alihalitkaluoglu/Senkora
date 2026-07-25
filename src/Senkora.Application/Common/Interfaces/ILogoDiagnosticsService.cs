using Senkora.Application.Features.Products.Queries;

namespace Senkora.Application.Common.Interfaces;

public interface ILogoDiagnosticsService
{
    Task<LogoFetchDiagnostics> ProbeItemsAsync(
        string restUrl, string accessToken, int limit, CancellationToken ct = default);
}
