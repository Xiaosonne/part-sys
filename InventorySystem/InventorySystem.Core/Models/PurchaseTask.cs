using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace InventorySystem.Core.Models;

/// <summary>
/// 采购任务 - 当选型单提交时库存不够自动生成
/// </summary>
public class PurchaseTask
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    /// <summary>关联的选型单ID</summary>
    [BsonRepresentation(BsonType.ObjectId)]
    public string SelectionPlanId { get; set; } = string.Empty;

    /// <summary>关联的选型配件ID</summary>
    [BsonRepresentation(BsonType.ObjectId)]
    public string SelectionItemId { get; set; } = string.Empty;

    /// <summary>配件ID</summary>
    [BsonRepresentation(BsonType.ObjectId)]
    public string PartId { get; set; } = string.Empty;

    /// <summary>配件名称（冗余存储）</summary>
    public string PartName { get; set; } = string.Empty;

    /// <summary>原始锁库数量（用于后续释放）</summary>
    public int LockedQty { get; set; }

    /// <summary>需要采购的数量（缺口）</summary>
    public int RequiredQty { get; set; }

    /// <summary>采购状态</summary>
    public PurchaseTaskStatus Status { get; set; } = PurchaseTaskStatus.Pending;

    /// <summary>创建人</summary>
    [BsonRepresentation(BsonType.ObjectId)]
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>最后更新人</summary>
    [BsonRepresentation(BsonType.ObjectId)]
    public string? UpdatedBy { get; set; }

    /// <summary>最后更新时间</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>备注</summary>
    public string? Remark { get; set; }
}

public enum PurchaseTaskStatus
{
    Pending,     // 待采购
    InProgress, // 采购中
    Received,   // 已到货
    Cancelled   // 已取消
}
