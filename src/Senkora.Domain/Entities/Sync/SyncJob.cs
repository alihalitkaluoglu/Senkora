using Senkora.Domain.Entities.Common;
using Senkora.Domain.Enums;

namespace Senkora.Domain.Entities.Sync;

public class SyncJob : TenantEntity
{
    public Guid WooStoreId { get; set; }
    public Guid LogoConnectionId { get; set; }
    public SyncJobType JobType { get; set; }
    public SyncDirection Direction { get; set; }
    public SyncStatus Status { get; set; } = SyncStatus.Pending;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int ProcessedCount { get; set; } = 0;
    public int SuccessCount { get; set; } = 0;
    public int ErrorCount { get; set; } = 0;
    public string? TriggerSource { get; set; }
    public string? ErrorMessage { get; set; }
    public string? HangfireJobId { get; set; }

    public ICollection<SyncLog> Logs { get; set; } = [];
    public ICollection<SyncError> Errors { get; set; } = [];
}
