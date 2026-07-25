using Senkora.Domain.Entities.Common;
using Senkora.Domain.Enums;

namespace Senkora.Domain.Entities.Integration;

public class FieldMapping : TenantEntity
{
    public Guid WooStoreId { get; set; }
    public WooStore WooStore { get; set; } = null!;
    public Guid LogoConnectionId { get; set; }
    public LogoConnection LogoConnection { get; set; } = null!;
    public MappingEntityType EntityType { get; set; }
    public string SourceField { get; set; } = string.Empty;
    public string TargetField { get; set; } = string.Empty;
    public string? DefaultValue { get; set; }
    public string? TransformExpression { get; set; }
    public bool IsRequired { get; set; } = false;
    public int SortOrder { get; set; } = 0;
}
