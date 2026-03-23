using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace InventorySystem.Core.Models;

public class SpecTemplate
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string Category { get; set; } = string.Empty;
    public List<ParamDef> ParamDefs { get; set; } = new();
}

public class ParamDef
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string DataType { get; set; } = "string"; // string | number | boolean
    public List<string>? Options { get; set; }
    public bool Required { get; set; }
}
