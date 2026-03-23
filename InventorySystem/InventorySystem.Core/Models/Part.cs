using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace InventorySystem.Core.Models;

public class Part
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string Name { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public List<Attachment> Attachments { get; set; } = new();

    [BsonRepresentation(BsonType.ObjectId)]
    public string? SpecTemplateId { get; set; }

    public List<SpecValue> Specs { get; set; } = new();

    public int TotalQty { get; set; }
    public int AvailableQty { get; set; }
    public int LockedQty { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class Attachment
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}

public class SpecValue
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
}
