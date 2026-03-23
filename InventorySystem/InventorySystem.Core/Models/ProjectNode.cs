using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace InventorySystem.Core.Models;

public class ProjectNode
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonRepresentation(BsonType.ObjectId)]
    public string? ParentId { get; set; }

    public string Type { get; set; } = "folder"; // folder | project
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string OwnerId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Only for type=project
    public string? ProjectCode { get; set; }
    public string Status { get; set; } = "draft"; // draft | active | completed | archived
}
