using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace InventorySystem.Core.Models;

public enum FileType
{
    PART,
    PROJECT,
    TEMPLATE,
    APPROVAL,
    SYSTEM
}

public class FileMetadata
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public string Bucket { get; set; } = string.Empty;
    public string ObjectKey { get; set; } = string.Empty;
    public FileType FileType { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string RelatedId { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.ObjectId)]
    public string UploadedBy { get; set; } = string.Empty;

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public string Description { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
