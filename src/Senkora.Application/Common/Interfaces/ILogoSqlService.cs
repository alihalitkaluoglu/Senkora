namespace Senkora.Application.Common.Interfaces;

/// <summary>
/// Logo REST uzerinden SQL sorgusu calistirir.
/// Logo surumune gore endpoint ve parametre bicimi degistigi icin
/// calisan bicim ilk basarili denemede tespit edilip sabitlenir.
/// </summary>
public interface ILogoSqlService
{
    /// <summary>Sorguyu calistirir, JSON dizi doner. Basarisizsa bos liste.</summary>
    Task<LogoSqlResult> QueryAsync(
        string restUrl, string accessToken, string sql,
        int timeoutSeconds = 60, CancellationToken ct = default);

    /// <summary>Tum bicimleri deneyip hangisinin calistigini raporlar (tani).</summary>
    Task<List<LogoSqlProbe>> ProbeAllAsync(
        string restUrl, string accessToken, string sql,
        CancellationToken ct = default);
}

public sealed record LogoSqlResult(
    bool    Success,
    string? RawJson,
    string? UsedVariant,
    string? Error);

public sealed record LogoSqlProbe(
    string  Variant,
    string  Url,
    bool    Success,
    int     RowCount,
    string? Error,
    string? SamplePayload);
