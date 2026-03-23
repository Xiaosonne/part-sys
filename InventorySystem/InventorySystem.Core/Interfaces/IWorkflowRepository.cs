using InventorySystem.Core.Models;

namespace InventorySystem.Core.Interfaces;

public interface IWorkflowDefinitionRepository
{
    Task<WorkflowDefinition?> GetByIdAsync(string id);
    Task<List<WorkflowDefinition>> GetAllAsync();
    Task<List<WorkflowDefinition>> GetByCategoryAsync(string category);
    Task CreateAsync(WorkflowDefinition definition);
    Task UpdateAsync(string id, WorkflowDefinition definition);
    Task DeleteAsync(string id);
}

public interface IWorkflowInstanceRepository
{
    Task<WorkflowInstance?> GetByIdAsync(string id);
    Task<List<WorkflowInstance>> GetByStartedByAsync(string userId);
    Task<List<WorkflowInstance>> GetByEntityAsync(string entityType, string entityId);
    Task CreateAsync(WorkflowInstance instance);
    Task UpdateAsync(string id, WorkflowInstance instance);
}

public interface IWorkflowTaskRepository
{
    Task<WorkflowTask?> GetByIdAsync(string id);
    Task<List<WorkflowTask>> GetPendingByAssigneeAsync(string assigneeId);
    Task<List<WorkflowTask>> GetAllPendingAsync();
    Task<List<WorkflowTask>> GetCompletedByAssigneeAsync(string assigneeId);
    Task<List<WorkflowTask>> GetAllCompletedAsync();
    Task<List<WorkflowTask>> GetByInstanceAsync(string instanceId);
    Task CreateAsync(WorkflowTask task);
    Task UpdateAsync(string id, WorkflowTask task);
}

public interface IWorkflowHistoryRepository
{
    Task<List<WorkflowHistory>> GetByInstanceAsync(string instanceId);
    Task CreateAsync(WorkflowHistory history);
}
