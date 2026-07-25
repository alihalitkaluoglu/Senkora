using Senkora.Domain.Entities.Common;

namespace Senkora.Domain.Entities.Sync;

public class SyncLog : TenantEntity
{
    public Guid SyncJobId { get; set; }
    public SyncJob SyncJob { get; set; } = null!;
    public string Level { get; set; } = "Information";
    public string Message { get; set; } = string.Empty;
    public string? RequestUrl { get; set; }
    public string? RequestBody { get; set; }
    public string? ResponseBody { get; set; }
    public int? HttpStatusCode { get; set; }
    public long? DurationMs { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
}
