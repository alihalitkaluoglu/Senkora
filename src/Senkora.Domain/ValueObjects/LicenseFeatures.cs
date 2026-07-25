namespace Senkora.Domain.ValueObjects;

using Senkora.Domain.Enums;

/// <summary>
/// Lisans tier'ina gore hangi ozelliklerin aktif oldugunu tanimlar.
/// </summary>
public sealed class LicenseFeatures
{
    public int    MaxWooStores       { get; init; }
    public int    MaxLogoConnections { get; init; }
    public int    MaxMarketplaces    { get; init; }
    public int    MaxProductsPerSync { get; init; }
    public int    MaxOrdersPerMonth  { get; init; }
    public bool   RealtimeSync       { get; init; }
    public bool   WebhookSupport     { get; init; }
    public bool   MultiWarehouse     { get; init; }
    public bool   AdvancedReporting  { get; init; }
    public bool   ApiAccess          { get; init; }
    public int    SyncIntervalMinutes { get; init; }

    public static LicenseFeatures For(LicenseTier tier) => tier switch
    {
        LicenseTier.Trial => new LicenseFeatures
        {
            MaxWooStores        = 1,
            MaxLogoConnections  = 1,
            MaxMarketplaces     = 0,
            MaxProductsPerSync  = 100,
            MaxOrdersPerMonth   = 50,
            RealtimeSync        = false,
            WebhookSupport      = false,
            MultiWarehouse      = false,
            AdvancedReporting   = false,
            ApiAccess           = false,
            SyncIntervalMinutes = 360
        },
        LicenseTier.Starter => new LicenseFeatures
        {
            MaxWooStores        = 1,
            MaxLogoConnections  = 1,
            MaxMarketplaces     = 0,
            MaxProductsPerSync  = 500,
            MaxOrdersPerMonth   = 200,
            RealtimeSync        = false,
            WebhookSupport      = true,
            MultiWarehouse      = false,
            AdvancedReporting   = false,
            ApiAccess           = false,
            SyncIntervalMinutes = 60
        },
        LicenseTier.Professional => new LicenseFeatures
        {
            MaxWooStores        = 3,
            MaxLogoConnections  = 2,
            MaxMarketplaces     = 2,
            MaxProductsPerSync  = 5000,
            MaxOrdersPerMonth   = 2000,
            RealtimeSync        = true,
            WebhookSupport      = true,
            MultiWarehouse      = true,
            AdvancedReporting   = true,
            ApiAccess           = false,
            SyncIntervalMinutes = 15
        },
        LicenseTier.Enterprise or LicenseTier.Oem => new LicenseFeatures
        {
            MaxWooStores        = int.MaxValue,
            MaxLogoConnections  = int.MaxValue,
            MaxMarketplaces     = int.MaxValue,
            MaxProductsPerSync  = int.MaxValue,
            MaxOrdersPerMonth   = int.MaxValue,
            RealtimeSync        = true,
            WebhookSupport      = true,
            MultiWarehouse      = true,
            AdvancedReporting   = true,
            ApiAccess           = true,
            SyncIntervalMinutes = 1
        },
        _ => For(LicenseTier.Trial)
    };
}
