using Senkora.Domain.Entities.Common;
using Senkora.Domain.Enums;

namespace Senkora.Domain.Entities.Orders;

public class OrderMapping : TenantEntity
{
    public Guid WooStoreId { get; set; }
    public Guid LogoConnectionId { get; set; }
    // WooCommerce side
    public long WooOrderId { get; set; }
    public string WooOrderNumber { get; set; } = string.Empty;
    public string WooOrderStatus { get; set; } = string.Empty;
    public decimal WooOrderTotal { get; set; }
    // Logo ERP side
    public int? LogoInternalRef { get; set; }
    public string? LogoDocNumber { get; set; }
    public string LogoResourceType { get; set; } = "salesOrders";
    // Sync state
    public MappingStatus Status { get; set; } = MappingStatus.Pending;
    public DateTime? TransferredAt { get; set; }
    public string? TransferError { get; set; }
    public int RetryCount { get; set; } = 0;
}
