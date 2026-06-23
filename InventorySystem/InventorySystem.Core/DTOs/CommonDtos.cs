using InventorySystem.Core.Models;

namespace InventorySystem.Core.DTOs;

public class PartSearchRequest
{
    public string? CategoryPath { get; set; }
    public string? Category { get; set; }
    public string? Keyword { get; set; }
    public List<SpecFilter>? SpecFilters { get; set; }
    public int? MinAvailableQty { get; set; }
    public int? MaxAvailableQty { get; set; }
}

public class SpecFilter
{
    public string Key { get; set; } = string.Empty;
    public string Op { get; set; } = "eq";
    public string Value { get; set; } = string.Empty;
}

public class PurchaseTaskListDto
{
    public string Id { get; set; } = string.Empty;
    public string SelectionPlanId { get; set; } = string.Empty;
    public string SelectionItemId { get; set; } = string.Empty;
    public string PartId { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
    public int LockedQty { get; set; }
    public int RequiredQty { get; set; }
    public PurchaseTaskStatus Status { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? Remark { get; set; }
    public string? SelectionPlanName { get; set; }
    public string? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public string? CreatedByName { get; set; }
    public string? UpdatedByName { get; set; }
}

public class UpdateStatusRequest
{
    public PurchaseTaskStatus Status { get; set; }
    public string? Remark { get; set; }
}

public class ReceiveRequest
{
    public string? Remark { get; set; }
}

public class StartInstanceRequest
{
    public string DefinitionId { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<string>? SelectedFileIds { get; set; }
    public Dictionary<string, object>? FormData { get; set; }
}

public class ApproveTaskRequest
{
    public string Comment { get; set; } = string.Empty;
    public Dictionary<string, object>? FormData { get; set; }
}

public class RejectTaskRequest
{
    public string Comment { get; set; } = string.Empty;
}

public class WorkflowInstanceDetailDto
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Status { get; set; }
    public string? StartedBy { get; set; }
    public string? StartedByName { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? CurrentNodeId { get; set; }
    public string? CurrentNodeName { get; set; }
    public List<string>? SelectedFileIds { get; set; }
    public WorkflowDefinition? Definition { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
}

public class WorkflowTaskDto
{
    public string? Id { get; set; }
    public string? InstanceId { get; set; }
    public string? NodeId { get; set; }
    public string? NodeName { get; set; }
    public string? AssigneeId { get; set; }
    public string? AssigneeName { get; set; }
    public string? Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Comment { get; set; }
    public string? InstanceName { get; set; }
    public string? StartedBy { get; set; }
    public string? StartedByName { get; set; }
    public WorkflowInstanceDetailDto? Instance { get; set; }
}

public record LoginRequest(string Username, string Password);
public record RegisterRequest(string Username, string Password, string DisplayName, string? Email, string? Role);
public record UpdateUserRequest(string? DisplayName, string? Email, string? Role, bool? IsActive);
public record ResetPasswordRequest(string NewPassword);

public record CreateUserRequest(string Username, string DisplayName, string? Email, string Role, bool IsActive, string Password = "123456");
