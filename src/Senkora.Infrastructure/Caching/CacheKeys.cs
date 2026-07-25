namespace Senkora.Infrastructure.Caching;

public static class CacheKeys
{
    public static string LogoToken(Guid connectionId) => $"logo:token:{connectionId}";
    public static string TenantConfig(Guid tenantId) => $"tenant:config:{tenantId}";
    public static string LicenseStatus(Guid tenantId) => $"license:status:{tenantId}";
    public static string ProductMapping(Guid tenantId, long wooId) => $"product:mapping:{tenantId}:{wooId}";
    public static string WooStoreStatus(Guid storeId) => $"woo:status:{storeId}";
}
