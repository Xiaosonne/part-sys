using InventorySystem.Core.Models;

namespace InventorySystem.Core.Interfaces;

public interface IWorkflowService
{
    Task<WorkflowDefinition> CreateDefinitionAsync(WorkflowDefinition definition);
    Task<WorkflowDefinition?> GetDefinitionAsync(string id);
    Task<List<WorkflowDefinition>> GetDefinitionsAsync(string? category = null);
    Task UpdateDefinitionAsync(string id, WorkflowDefinition definition);
    Task DeleteDefinitionAsync(string id);

    Task<WorkflowInstance> StartInstanceAsync(string definitionId, string entityType, string entityId, string startedBy, List<string>? selectedFileIds = null, string? name = null, Dictionary<string, object>? formData = null);
    Task<WorkflowInstance?> GetInstanceAsync(string id);
    Task<List<WorkflowInstance>> GetInstancesByUserAsync(string userId);
    Task CancelInstanceAsync(string id);
}

public interface IWorkflowTaskService
{
    Task<List<WorkflowTask>> GetPendingTasksAsync(string userId);
    Task<List<WorkflowTask>> GetAllPendingTasksAsync();
    Task<List<WorkflowTask>> GetCompletedTasksAsync(string userId);
    Task<List<WorkflowTask>> GetAllCompletedTasksAsync();
    Task ApproveTaskAsync(string taskId, string comment, Dictionary<string, object>? formData = null);
    Task RejectTaskAsync(string taskId, string comment);
}
