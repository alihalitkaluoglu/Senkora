using Senkora.Domain.Entities.Common;

namespace Senkora.Domain.Entities.Tenants;

public class TenantSettings : TenantEntity
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool IsEncrypted { get; set; } = false;
}
