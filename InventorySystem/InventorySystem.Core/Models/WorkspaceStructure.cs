using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace InventorySystem.Core.Models;

public class WorkspaceStructure
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    public string ProjectId { get; set; }
    public List<WorkspaceNode> Structure { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class WorkspaceNode
{
    public string FolderId { get; set; }
    public string FolderName { get; set; }
    public List<WorkspaceNode> Children { get; set; } = new();
}
