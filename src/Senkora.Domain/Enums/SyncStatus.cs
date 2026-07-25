namespace Senkora.Domain.Enums;

public enum SyncStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4,
    PartialSuccess = 5
}
