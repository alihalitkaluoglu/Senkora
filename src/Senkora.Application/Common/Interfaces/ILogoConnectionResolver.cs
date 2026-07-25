namespace Senkora.Application.Common.Interfaces;

public interface ILogoConnectionResolver
{
    Task<LogoConnectionInfo> ResolveAsync(
        Guid tenantId, Guid connectionId, CancellationToken ct = default);
}

public sealed record LogoConnectionInfo(
    string RestUrl,
    string AccessToken,
    int    FirmNo,
    string Username,
    int    PeriodNo = 1);
