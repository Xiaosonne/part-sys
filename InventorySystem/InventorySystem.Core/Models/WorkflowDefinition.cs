using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace InventorySystem.Core.Models;

public class WorkflowDefinition
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public string Category { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;

    // 开始节点配置：定义启动流程时需要填写的信息
    public StartConfig StartConfig { get; set; } = new();

    public List<WorkflowNode> Nodes { get; set; } = new();

    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// 开始节点配置
public class StartConfig
{
    // 是否需要选择关联实体（如项目）
    public bool RequireEntity { get; set; } = false;

    // 实体类型（如 "Project"），为空则不限制
    public string EntityType { get; set; } = string.Empty;

    // 是否需要选择文件
    public bool RequireFiles { get; set; } = false;

    // 最少需要的文件数量
    public int MinFileCount { get; set; } = 0;

    // 最多允许的文件数量（0 表示不限制）
    public int MaxFileCount { get; set; } = 0;

    // 允许的文件类型（如 [".pdf", ".docx"]），为空表示不限制
    public List<string> AllowedFileTypes { get; set; } = new();

    // 额外的表单字段
    public List<FormField> FormFields { get; set; } = new();
}

public class WorkflowNode
{
    public string Id { get; set; } = string.Empty;
    public string NodeType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ApprovalMode { get; set; } = string.Empty;

    public List<string> Approvers { get; set; } = new();
    public string ApproverSource { get; set; } = "Static";

    public List<FormField> FormFields { get; set; } = new();
    public List<string> NextNodes { get; set; } = new();

    public int TimeoutMinutes { get; set; } = 0;
    public string TimeoutAction { get; set; } = "Notify";
}

public class FormField
{
    public string Id { get; set; } = string.Empty;
    public string FieldType { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public bool Required { get; set; } = false;
    public string DefaultValue { get; set; } = string.Empty;
    public string Placeholder { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public string DisplayCondition { get; set; } = string.Empty;

    // 实体类型，用于 project/projectFile 字段类型
    // Project / Folder / Part 等
    public string EntityType { get; set; } = string.Empty;

    // 关联的实体字段 key，用于 projectFile 类型
    // 指定从哪个 project 字段获取项目 ID
    public string EntitySourceKey { get; set; } = string.Empty;

    // 文件类型限制，用于 projectFile 类型
    public List<string> AllowedFileTypes { get; set; } = new();
}
