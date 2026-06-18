using InventorySystem.Core.Interfaces;
using InventorySystem.Core.Models;
using Serilog;

namespace InventorySystem.Infrastructure.Services;

public class WorkflowService : IWorkflowService
{
    private readonly IWorkflowDefinitionRepository _definitionRepo;
    private readonly IWorkflowInstanceRepository _instanceRepo;
    private readonly IWorkflowTaskRepository _taskRepo;
    private readonly IWorkflowHistoryRepository _historyRepo;

    public WorkflowService(
        IWorkflowDefinitionRepository definitionRepo,
        IWorkflowInstanceRepository instanceRepo,
        IWorkflowTaskRepository taskRepo,
        IWorkflowHistoryRepository historyRepo)
    {
        _definitionRepo = definitionRepo;
        _instanceRepo = instanceRepo;
        _taskRepo = taskRepo;
        _historyRepo = historyRepo;
    }

    /// <summary>
    /// 将 Dictionary 中的 JsonElement 转换为可序列化的基础类型
    /// </summary>
    private Dictionary<string, object> ConvertFormData(Dictionary<string, object>? formData)
    {
        return FormDataConverter.Convert(formData);
    }

    public async Task<WorkflowDefinition> CreateDefinitionAsync(WorkflowDefinition definition)
    {
        definition.Id = null;
        definition.CreatedAt = DateTime.UtcNow;
        definition.UpdatedAt = DateTime.UtcNow;
        await _definitionRepo.CreateAsync(definition);
        return definition;
    }

    public async Task<WorkflowDefinition?> GetDefinitionAsync(string id) =>
        await _definitionRepo.GetByIdAsync(id);

    public async Task<List<WorkflowDefinition>> GetDefinitionsAsync(string? category = null) =>
        string.IsNullOrEmpty(category)
            ? await _definitionRepo.GetAllAsync()
            : await _definitionRepo.GetByCategoryAsync(category);

    public async Task UpdateDefinitionAsync(string id, WorkflowDefinition definition)
    {
        definition.Id = id;
        definition.UpdatedAt = DateTime.UtcNow;
        await _definitionRepo.UpdateAsync(id, definition);
    }

    public async Task DeleteDefinitionAsync(string id) =>
        await _definitionRepo.DeleteAsync(id);

    public async Task<WorkflowInstance> StartInstanceAsync(
        string definitionId, string entityType, string entityId, string startedBy, List<string>? selectedFileIds = null, string? name = null, Dictionary<string, object>? formData = null)
    {
        var definition = await _definitionRepo.GetByIdAsync(definitionId);
        if (definition == null)
            throw new InvalidOperationException($"Workflow definition {definitionId} not found");

        Log.Information("[WORKFLOW] Starting workflow: definitionId={DefinitionId}, startedBy={StartedBy}", definitionId, startedBy);
        Log.Information("[WORKFLOW] Definition nodes count: {NodeCount}", definition.Nodes.Count);

        var instance = new WorkflowInstance
        {
            DefinitionId = definitionId,
            DefinitionVersion = definition.Version,
            Name = string.IsNullOrEmpty(name) ? definition.Name : name,
            EntityType = entityType,
            EntityId = entityId,
            StartedBy = startedBy,
            StartedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            SelectedFileIds = selectedFileIds ?? new(),
            CurrentNodeId = definition.Nodes.FirstOrDefault()?.Id ?? string.Empty,
            FormData = ConvertFormData(formData)
        };

        await _instanceRepo.CreateAsync(instance);
        Log.Information("[WORKFLOW] Instance created: {InstanceId}", instance.Id);

        // Create initial history record
        await _historyRepo.CreateAsync(new WorkflowHistory
        {
            InstanceId = instance.Id!,
            NodeId = instance.CurrentNodeId,
            NodeName = definition.Nodes.FirstOrDefault()?.Name ?? string.Empty,
            Action = "Start",
            OperatorId = startedBy,
            FormData = instance.FormData,
            CreatedAt = DateTime.UtcNow
        });

        // Create first approval task if first approval node exists
        var firstApprovalNode = definition.Nodes.FirstOrDefault(n => n.NodeType == "SingleApproval");
        Log.Information("[WORKFLOW] First approval node: {NodeName}, approvers={ApproverCount}",
            firstApprovalNode?.Name ?? "null", firstApprovalNode?.Approvers?.Count ?? 0);

        if (firstApprovalNode?.Approvers.Count > 0)
        {
            var assigneeId = firstApprovalNode.Approvers[0];
            Log.Information("[WORKFLOW] Creating task for assignee: {AssigneeId}", assigneeId);

            await _taskRepo.CreateAsync(new WorkflowTask
            {
                InstanceId = instance.Id!,
                NodeId = firstApprovalNode.Id,
                NodeName = firstApprovalNode.Name,
                AssigneeId = assigneeId,
                CreatedAt = DateTime.UtcNow,
                DueDate = firstApprovalNode.TimeoutMinutes > 0 ? DateTime.UtcNow.AddMinutes(firstApprovalNode.TimeoutMinutes) : null
            });
            Log.Information("[WORKFLOW] Task created successfully");
        }
        else
        {
            Log.Warning("[WORKFLOW] No approval node found or no approvers configured");
        }

        return instance;
    }

    public async Task<WorkflowInstance?> GetInstanceAsync(string id) =>
        await _instanceRepo.GetByIdAsync(id);

    public async Task<List<WorkflowInstance>> GetInstancesByUserAsync(string userId) =>
        await _instanceRepo.GetByStartedByAsync(userId);

    public async Task CancelInstanceAsync(string id)
    {
        var instance = await _instanceRepo.GetByIdAsync(id);
        if (instance == null)
            throw new InvalidOperationException($"Workflow instance {id} not found");

        instance.Status = "Cancelled";
        instance.UpdatedAt = DateTime.UtcNow;
        await _instanceRepo.UpdateAsync(id, instance);
    }
}

public class WorkflowTaskService : IWorkflowTaskService
{
    private readonly IWorkflowTaskRepository _taskRepo;
    private readonly IWorkflowInstanceRepository _instanceRepo;
    private readonly IWorkflowDefinitionRepository _definitionRepo;
    private readonly IWorkflowHistoryRepository _historyRepo;
    private readonly ISelectionService _selectionService;
    private readonly ISelectionPlanRepository _selectionPlanRepo;

    public WorkflowTaskService(
        IWorkflowTaskRepository taskRepo,
        IWorkflowInstanceRepository instanceRepo,
        IWorkflowDefinitionRepository definitionRepo,
        IWorkflowHistoryRepository historyRepo,
        ISelectionService selectionService,
        ISelectionPlanRepository selectionPlanRepo)
    {
        _taskRepo = taskRepo;
        _instanceRepo = instanceRepo;
        _definitionRepo = definitionRepo;
        _historyRepo = historyRepo;
        _selectionService = selectionService;
        _selectionPlanRepo = selectionPlanRepo;
    }

    public async Task<List<WorkflowTask>> GetPendingTasksAsync(string userId) =>
        await _taskRepo.GetPendingByAssigneeAsync(userId);

    public async Task<List<WorkflowTask>> GetAllPendingTasksAsync() =>
        await _taskRepo.GetAllPendingAsync();

    public async Task<List<WorkflowTask>> GetCompletedTasksAsync(string userId) =>
        await _taskRepo.GetCompletedByAssigneeAsync(userId);

    public async Task<List<WorkflowTask>> GetAllCompletedTasksAsync() =>
        await _taskRepo.GetAllCompletedAsync();

    public async Task ApproveTaskAsync(string taskId, string comment, Dictionary<string, object>? formData = null)
    {
        var task = await _taskRepo.GetByIdAsync(taskId);
        if (task == null)
            throw new InvalidOperationException($"Task {taskId} not found");

        if (task.Status != "Pending")
            throw new InvalidOperationException($"Task {taskId} is already {task.Status}, cannot approve");

        // 转换 formData 中的 JsonElement 为基本类型
        var convertedFormData = ConvertFormData(formData);

        task.Status = "Approved";
        task.Comment = comment;
        task.FormData = convertedFormData;
        task.CompletedAt = DateTime.UtcNow;
        await _taskRepo.UpdateAsync(taskId, task);

        // Record history
        await _historyRepo.CreateAsync(new WorkflowHistory
        {
            InstanceId = task.InstanceId,
            NodeId = task.NodeId,
            NodeName = task.NodeName,
            Action = "Approve",
            OperatorId = task.AssigneeId,
            Comment = comment,
            FormData = convertedFormData,
            CreatedAt = DateTime.UtcNow
        });

        // Move to next node
        var instance = await _instanceRepo.GetByIdAsync(task.InstanceId);
        if (instance != null)
        {
            var definition = await _definitionRepo.GetByIdAsync(instance.DefinitionId);
            if (definition != null)
            {
                var currentNode = definition.Nodes.FirstOrDefault(n => n.Id == task.NodeId);
                if (currentNode?.NextNodes.Count > 0)
                {
                    var nextNodeId = currentNode.NextNodes[0];
                    var nextNode = definition.Nodes.FirstOrDefault(n => n.Id == nextNodeId);

                    if (nextNode != null)
                    {
                        instance.CurrentNodeId = nextNodeId;
                        instance.UpdatedAt = DateTime.UtcNow;

                        if (nextNode.NodeType == "End")
                        {
                            instance.Status = "Completed";
                            instance.CompletedAt = DateTime.UtcNow;
                        }

                        await _instanceRepo.UpdateAsync(instance.Id!, instance);

                        // Create next task if approval node
                        if (nextNode.NodeType == "SingleApproval" && nextNode.Approvers.Count > 0)
                        {
                            await _taskRepo.CreateAsync(new WorkflowTask
                            {
                                InstanceId = instance.Id!,
                                NodeId = nextNode.Id,
                                NodeName = nextNode.Name,
                                AssigneeId = nextNode.Approvers[0],
                                CreatedAt = DateTime.UtcNow,
                                DueDate = nextNode.TimeoutMinutes > 0 ? DateTime.UtcNow.AddMinutes(nextNode.TimeoutMinutes) : null
                            });
                        }
                    }
                }
            }
        }
    }

    public async Task RejectTaskAsync(string taskId, string comment)
    {
        var task = await _taskRepo.GetByIdAsync(taskId);
        if (task == null)
            throw new InvalidOperationException($"Task {taskId} not found");

        if (task.Status != "Pending")
            throw new InvalidOperationException($"Task {taskId} is already {task.Status}, cannot reject");

        task.Status = "Rejected";
        task.Comment = comment;
        task.CompletedAt = DateTime.UtcNow;
        await _taskRepo.UpdateAsync(taskId, task);

        // Record history
        await _historyRepo.CreateAsync(new WorkflowHistory
        {
            InstanceId = task.InstanceId,
            NodeId = task.NodeId,
            NodeName = task.NodeName,
            Action = "Reject",
            OperatorId = task.AssigneeId,
            Comment = comment,
            CreatedAt = DateTime.UtcNow
        });

        // Mark instance as rejected
        var instance = await _instanceRepo.GetByIdAsync(task.InstanceId);
        if (instance != null)
        {
            instance.Status = "Rejected";
            instance.UpdatedAt = DateTime.UtcNow;
            await _instanceRepo.UpdateAsync(instance.Id!, instance);

            // Auto-unlock selection plan stock if workflow is linked to a submitted plan
            if (instance.EntityType == "SelectionPlan" && !string.IsNullOrEmpty(instance.EntityId))
            {
                try
                {
                    var plan = await _selectionPlanRepo.GetByIdAsync(instance.EntityId);
                    if (plan != null && plan.Status == SelectionPlanStatus.Submitted)
                    {
                        await _selectionService.CancelAsync(instance.EntityId, instance.StartedBy);
                    }
                }
                catch (Exception ex)
                {
                    // Log but don't fail the rejection
                    Serilog.Log.Warning(ex,
                        "Failed to auto-unlock selection plan {PlanId} after workflow rejection",
                        instance.EntityId);
                }
            }
        }
    }

    private Dictionary<string, object> ConvertFormData(Dictionary<string, object>? formData)
    {
        return FormDataConverter.Convert(formData);
    }
}
