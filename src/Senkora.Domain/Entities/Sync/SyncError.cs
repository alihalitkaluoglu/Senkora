using Senkora.Domain.Entities.Common;

namespace Senkora.Domain.Entities.Sync;

public class SyncError : TenantEntity
{
    public Guid SyncJobId { get; set; }
    public SyncJob SyncJob { get; set; } = null!;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public bool IsResolved { get; set; } = false;
    public DateTime? ResolvedAt { get; set; }
    public int RetryCount { get; set; } = 0;
}
