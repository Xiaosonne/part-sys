using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace InventorySystem.Core.Models;

public class WorkflowInstance
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string DefinitionId { get; set; } = string.Empty;
    public int DefinitionVersion { get; set; } = 1;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "Running";

    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;

    public List<string> SelectedFileIds { get; set; } = new();
    public string CurrentNodeId { get; set; } = string.Empty;

    public Dictionary<string, object> FormData { get; set; } = new();

    public string StartedBy { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class WorkflowTask
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string InstanceId { get; set; } = string.Empty;
    public string NodeId { get; set; } = string.Empty;
    public string NodeName { get; set; } = string.Empty;

    public string AssigneeId { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";

    public Dictionary<string, object> FormData { get; set; } = new();
    public string Comment { get; set; } = string.Empty;
    public List<string> UploadedFileIds { get; set; } = new();

    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? DueDate { get; set; }
}

public class WorkflowHistory
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string InstanceId { get; set; } = string.Empty;
    public string NodeId { get; set; } = string.Empty;
    public string NodeName { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;
    public string OperatorId { get; set; } = string.Empty;
    public string OperatorName { get; set; } = string.Empty;

    public string Comment { get; set; } = string.Empty;
    public Dictionary<string, object> FormData { get; set; } = new();

    public DateTime CreatedAt { get; set; }
}
