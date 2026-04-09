using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace InventorySystem.Core.Models;

public class StockTransaction
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonRepresentation(BsonType.ObjectId)]
    public string PartId { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty; // INBOUND | OUTBOUND | LOCK | UNLOCK | RETURN

    public int Quantity { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string OperatorId { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.ObjectId)]
    public string? ProjectId { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string? RecipientId { get; set; }

    public string? RecipientName { get; set; }
    public string Note { get; set; } = string.Empty;

    /// <summary>来源类型：手动/采购/选型</summary>
    public StockSourceType SourceType { get; set; } = StockSourceType.Manual;

    /// <summary>关联采购任务（采购入库时）</summary>
    [BsonRepresentation(BsonType.ObjectId)]
    public string? PurchaseTaskId { get; set; }

    /// <summary>关联选型单</summary>
    [BsonRepresentation(BsonType.ObjectId)]
    public string? SelectionPlanId { get; set; }

    /// <summary>关联选型配件项</summary>
    public string? SelectionItemId { get; set; }

    /// <summary>供应商（入库时）</summary>
    public string? Supplier { get; set; }

    /// <summary>采购单号（入库时）</summary>
    public string? PurchaseOrderNo { get; set; }

    /// <summary>用途说明（出库时）</summary>
    public string? Usage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
