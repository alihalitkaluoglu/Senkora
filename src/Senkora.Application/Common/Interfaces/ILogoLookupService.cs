namespace Senkora.Application.Common.Interfaces;

/// <summary>
/// Logo'dan fiyat kriteri secim listeleri.
///   Projeler       → REST /api/v1/projects
///   Masraf merkezi → REST /api/v1/overheadAccounts
///   Ticari islem gr→ REST endpoint YOK, SQL (queries) ile tablodan
/// </summary>
public interface ILogoLookupService
{
    Task<LogoLookupResult> GetAllAsync(
        string restUrl, string accessToken, int firmNo, CancellationToken ct = default);
}

public sealed record LogoLookupItem(string Code, string Name);

public sealed record LogoLookupSet(
    List<LogoLookupItem> Items,
    string?              Source = null,   // hangi endpoint/sorgu kullanildi
    string?              Error  = null);

public sealed record LogoLookupResult(
    LogoLookupSet Projects,
    LogoLookupSet TradingGroups,
    LogoLookupSet CostCenters);
