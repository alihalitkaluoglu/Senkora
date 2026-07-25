namespace Senkora.Application.Common.Interfaces;

/// <summary>
/// Logo baglantisi icin token ve URL bilgisini cozumler.
/// Sifre cozme ve token alma islemlerini kapsullar.
/// </summary>
public interface ILogoConnectionResolver
{
    Task<LogoConnectionInfo> ResolveAsync(
        Guid tenantId, Guid connectionId, CancellationToken ct = default);
}

public sealed record LogoConnectionInfo(
    string RestUrl,
    string AccessToken,
    int    FirmNo,
    string Username);
