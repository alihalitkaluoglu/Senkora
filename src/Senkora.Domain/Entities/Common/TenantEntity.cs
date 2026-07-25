namespace Senkora.Domain.Entities.Common;

public abstract class TenantEntity : BaseEntity
{
    public Guid TenantId { get; set; }
}
