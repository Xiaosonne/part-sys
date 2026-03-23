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
    public string Status { get; set; } = "draft"; // draft | submitted | approved

    [BsonRepresentation(BsonType.ObjectId)]
    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<SelectionItem> Items { get; set; } = new();
}

public class SelectionItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Category { get; set; } = string.Empty;
    public List<FilterCriteria> FilterCriteria { get; set; } = new();
    public int RequiredQty { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string? SelectedPartId { get; set; }

    public string? Note { get; set; }
}

public class FilterCriteria
{
    public string Key { get; set; } = string.Empty;
    public string Operator { get; set; } = "eq"; // eq | gte | lte | contains
    public string Value { get; set; } = string.Empty;
}
