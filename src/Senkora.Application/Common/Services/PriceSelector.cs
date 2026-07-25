using Senkora.Application.Common.Interfaces;

namespace Senkora.Application.Common.Services;

/// <summary>
/// Logo fiyat kartlari arasindan magazaya uygun fiyati secer.
///
/// Kriter puanlamasi (proje / ticari islem grubu / masraf merkezi):
///   kart alani DOLU ve magaza ile AYNI  → +2
///   kart alani BOS (genel fiyat)        →  0  (uyumlu)
///   kart alani DOLU ve FARKLI           → eleme
///
/// Uc kriteri de tutan kart en yuksek puani alir;
/// hicbir kriteri olmayan genel fiyat yedek kalir.
/// </summary>
public static class PriceSelector
{
    public static LogoItemPriceDto? Select(
        IEnumerable<LogoItemPriceDto> candidates,
        string? storeProjectCode,
        string? storeTradingGroupCode,
        string? storeCostCenterCode,
        DateTime? asOf = null)
    {
        var today = (asOf ?? DateTime.UtcNow).Date;

        LogoItemPriceDto? best = null;
        var bestScore = int.MinValue;

        foreach (var p in candidates)
        {
            if (p.BeginDate is not null && p.BeginDate > today) continue;
            if (p.EndDate   is not null && p.EndDate   < today) continue;

            var s1 = Score(p.ProjectCode,      storeProjectCode);      if (s1 < 0) continue;
            var s2 = Score(p.TradingGroupCode, storeTradingGroupCode); if (s2 < 0) continue;
            var s3 = Score(p.CostCenterCode,   storeCostCenterCode);   if (s3 < 0) continue;

            var score = s1 + s2 + s3;
            if (p.Price > 0) score += 1;

            if (score > bestScore ||
               (score == bestScore && best is not null && p.PriceListRef > best.PriceListRef))
            {
                best = p; bestScore = score;
            }
        }

        return best;
    }

    private static int Score(string? cardValue, string? storeValue)
    {
        if (string.IsNullOrWhiteSpace(cardValue))  return 0;
        if (string.IsNullOrWhiteSpace(storeValue)) return -1;
        return string.Equals(cardValue.Trim(), storeValue.Trim(),
            StringComparison.OrdinalIgnoreCase) ? 2 : -1;
    }
}
