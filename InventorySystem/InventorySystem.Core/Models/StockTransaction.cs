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
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
