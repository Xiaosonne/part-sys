using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace InventorySystem.Core.Models;

public class PartCategory
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    /// <summary>
    /// 分类名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 分类路径，如 "电子元器件/微控制器/ESP32"
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// 父分类ID，没有则为null
    /// </summary>
    [BsonRepresentation(BsonType.ObjectId)]
    public string? ParentId { get; set; }

    /// <summary>
    /// 排序顺序
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// 关联的规格模板ID
    /// </summary>
    [BsonRepresentation(BsonType.ObjectId)]
    public string? SpecTemplateId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
