using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace InventorySystem.Core.Models;

public class SelectionPlan
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonRepresentation(BsonType.ObjectId)]
    public string ProjectId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>
    /// 选型单状态: Draft(草稿) | Submitted(已提交/锁定) | Completed(已完成) | Cancelled(已取消)
    /// </summary>
    public SelectionPlanStatus Status { get; set; } = SelectionPlanStatus.Draft;

    [BsonRepresentation(BsonType.ObjectId)]
    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; set; }
    public List<SelectionItem> Items { get; set; } = new();
}

public enum SelectionPlanStatus
{
    Draft,       // 草稿
    Submitted,   // 已提交（已锁定）
    Completed,   // 已完成（全部出库）
    Cancelled    // 已取消（解锁）
}

public class SelectionItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [BsonRepresentation(BsonType.ObjectId)]
    public string SelectedPartId { get; set; } = string.Empty;

    public string PartName { get; set; } = string.Empty;  // 冗余存储配件名称
    public string Category { get; set; } = string.Empty;

    /// <summary>需求总数</summary>
    public int RequiredQty { get; set; }

    /// <summary>已锁定（预留未出库）</summary>
    public int LockedQty { get; set; }

    /// <summary>已出库（已领用）</summary>
    public int OutboundQty { get; set; }

    /// <summary>待采购（库存不够）</summary>
    public int PendingQty { get; set; }

    /// <summary>采购任务ID（如果有待采购）</summary>
    [BsonRepresentation(BsonType.ObjectId)]
    public string? PurchaseTaskId { get; set; }

    public string? Note { get; set; }

    // 自动计算属性（不存储）
    [BsonIgnore]
    public int RemainingQty => RequiredQty - OutboundQty;  // 还需多少

    [BsonIgnore]
    public bool IsCompleted => RemainingQty == 0;

    [BsonIgnore]
    public SelectionItemStatus Status
    {
        get
        {
            if (IsCompleted) return SelectionItemStatus.Completed;
            if (PendingQty > 0) return SelectionItemStatus.PartiallyPending;
            if (OutboundQty > 0) return SelectionItemStatus.PartiallyOutbound;
            return SelectionItemStatus.AllLocked;
        }
    }
}

public enum SelectionItemStatus
{
    AllLocked,           // 全部锁定，未出库
    PartiallyOutbound,   // 部分出库
    PartiallyPending,    // 部分待采购
    Completed           // 全部完成
}

public class FilterCriteria
{
    public string Key { get; set; } = string.Empty;
    public string Operator { get; set; } = "eq"; // eq | gte | lte | contains
    public string Value { get; set; } = string.Empty;
}
