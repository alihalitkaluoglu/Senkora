namespace Senkora.Domain.Enums;

public enum SyncMappingStatus
{
    Draft      = 0,  // Logo'dan alındı, henüz eşlenmedi
    Enriched   = 1,  // Portal'da zenginleştirildi, gönderime hazır
    Pending    = 2,  // Gönderim kuyruğunda
    Synced     = 3,  // WooCommerce'e başarıyla gönderildi
    Error      = 4,  // Son gönderimde hata oluştu
    Excluded   = 5,  // Gönderimden hariç tutuldu
}
