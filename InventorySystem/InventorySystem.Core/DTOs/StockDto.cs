using InventorySystem.Core.Models;

namespace InventorySystem.Core.DTOs;

public class StockOverviewDto
{
    public string PartId { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
    public string PartModel { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int TotalQty { get; set; }
    public int LockedQty { get; set; }
    public int AvailableQty { get; set; }
    public int PendingPurchaseQty { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class StockTransactionDto
{
    public string Id { get; set; } = string.Empty;
    public string PartId { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
    public string PartModel { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public StockSourceType SourceType { get; set; }
    public string SourceTypeName => SourceType.ToString();
    public int Quantity { get; set; }
    public string OperatorId { get; set; } = string.Empty;
    public string OperatorName { get; set; } = string.Empty;
    public string? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public string? SelectionPlanId { get; set; }
    public string? SelectionPlanName { get; set; }
    public string? SelectionItemId { get; set; }
    public string? RecipientId { get; set; }
    public string? RecipientName { get; set; }
    public string? Note { get; set; }
    public string? Supplier { get; set; }
    public string? PurchaseOrderNo { get; set; }
    public string? Usage { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class StockLockSummaryDto
{
    public string PartId { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
    public string PartModel { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int TotalLocked { get; set; }
    public List<LockDetail> Locks { get; set; } = new();
}

public class LockDetail
{
    public string TransactionId { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string SelectionPlanId { get; set; } = string.Empty;
    public string SelectionPlanName { get; set; } = string.Empty;
    public string SelectionItemId { get; set; } = string.Empty;
    public int LockedQty { get; set; }
    public string OperatorId { get; set; } = string.Empty;
    public string OperatorName { get; set; } = string.Empty;
    public DateTime LockedAt { get; set; }
}

public class InboundRequestDto
{
    public string PartId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public StockSourceType SourceType { get; set; } = StockSourceType.Manual;
    public string? PurchaseTaskId { get; set; }
    public string? Supplier { get; set; }
    public string? PurchaseOrderNo { get; set; }
    public string? Note { get; set; }
}

public class OutboundRequestDto
{
    public string PartId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string ProjectId { get; set; } = string.Empty;
    public string? SelectionPlanId { get; set; }
    public string? SelectionItemId { get; set; }
    public string? RecipientId { get; set; }
    public string? RecipientName { get; set; }
    public string? Usage { get; set; }
    public string? Note { get; set; }
}

public class LockRequestDto
{
    public string PartId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? ProjectId { get; set; }
    public string? SelectionPlanId { get; set; }
    public string? SelectionItemId { get; set; }
    public StockSourceType SourceType { get; set; } = StockSourceType.Manual;
    public string? Note { get; set; }
}

public class UnlockRequestDto
{
    public string PartId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? ProjectId { get; set; }
    public string? SelectionPlanId { get; set; }
    public string? SelectionItemId { get; set; }
    public StockSourceType SourceType { get; set; } = StockSourceType.Manual;
    public string? Note { get; set; }
}

public class BatchUnlockRequestDto
{
    public string? PartId { get; set; }
    public string? ProjectId { get; set; }
    public string? SelectionPlanId { get; set; }
    public string? SelectionItemId { get; set; }
}

public class TransactionQueryDto
{
    public string? PartId { get; set; }
    public string? ProjectId { get; set; }
    public string? Type { get; set; }
    public StockSourceType? SourceType { get; set; }
    public string? OperatorId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class TransactionListResponseDto
{
    public List<StockTransaction> Items { get; set; } = new();
    public long TotalCount { get; set; }
}
