using InventorySystem.Core.Interfaces;
using InventorySystem.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace InventorySystem.API.Controllers;

[ApiController]
[Route("api/workflows")]
[Authorize]
public class WorkflowsController : ControllerBase
{
    private readonly IWorkflowService _workflowService;
    private readonly IWorkflowTaskService _taskService;
    private readonly IWorkflowHistoryRepository _historyRepo;
    private readonly IUserRepository _userRepo;

    public WorkflowsController(IWorkflowService workflowService, IWorkflowTaskService taskService, IWorkflowHistoryRepository historyRepo, IUserRepository userRepo)
    {
        _workflowService = workflowService;
        _taskService = taskService;
        _historyRepo = historyRepo;
        _userRepo = userRepo;
    }

    // Definitions
    [HttpPost("definitions")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> CreateDefinition([FromBody] WorkflowDefinition definition)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        definition.CreatedBy = userId;
        var result = await _workflowService.CreateDefinitionAsync(definition);
        return CreatedAtAction(nameof(GetDefinition), new { id = result.Id }, result);
    }

    [HttpGet("definitions")]
    public async Task<IActionResult> GetDefinitions([FromQuery] string? category)
    {
        var definitions = await _workflowService.GetDefinitionsAsync(category);
        return Ok(definitions);
    }

    [HttpGet("definitions/{id}")]
    public async Task<IActionResult> GetDefinition(string id)
    {
        var definition = await _workflowService.GetDefinitionAsync(id);
        return definition == null ? NotFound() : Ok(definition);
    }

    [HttpPut("definitions/{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> UpdateDefinition(string id, [FromBody] WorkflowDefinition definition)
    {
        var existing = await _workflowService.GetDefinitionAsync(id);
        if (existing == null) return NotFound();
        await _workflowService.UpdateDefinitionAsync(id, definition);
        return Ok(definition);
    }

    [HttpDelete("definitions/{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> DeleteDefinition(string id)
    {
        var existing = await _workflowService.GetDefinitionAsync(id);
        if (existing == null) return NotFound();
        await _workflowService.DeleteDefinitionAsync(id);
        return NoContent();
    }

    // Instances
    [HttpPost("instances")]
    public async Task<IActionResult> StartInstance([FromBody] StartInstanceRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        var instance = await _workflowService.StartInstanceAsync(
            request.DefinitionId, request.EntityType, request.EntityId, userId, request.SelectedFileIds, request.Name, request.FormData);
        return CreatedAtAction(nameof(GetInstance), new { id = instance.Id }, instance);
    }

    [HttpGet("instances/{id}")]
    public async Task<IActionResult> GetInstance(string id)
    {
        var instance = await _workflowService.GetInstanceAsync(id);
        if (instance == null) return NotFound();

        var user = await _userRepo.GetByIdAsync(instance.StartedBy);
        var definition = await _workflowService.GetDefinitionAsync(instance.DefinitionId);
        var currentNode = definition?.Nodes.FirstOrDefault(n => n.Id == instance.CurrentNodeId);

        var dto = new WorkflowInstanceDetailDto
        {
            Id = instance.Id,
            Name = instance.Name,
            Status = instance.Status,
            StartedBy = instance.StartedBy,
            StartedByName = user?.Username ?? "Unknown",
            StartedAt = instance.StartedAt,
            CompletedAt = instance.CompletedAt,
            CurrentNodeId = instance.CurrentNodeId,
            CurrentNodeName = currentNode?.Name ?? "",
            SelectedFileIds = instance.SelectedFileIds,
            Definition = definition,
            EntityType = instance.EntityType,
            EntityId = instance.EntityId
        };

        return Ok(dto);
    }

    [HttpGet("instances")]
    public async Task<IActionResult> GetMyInstances()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        var instances = await _workflowService.GetInstancesByUserAsync(userId);
        return Ok(instances);
    }

    [HttpPost("instances/{id}/cancel")]
    public async Task<IActionResult> CancelInstance(string id)
    {
        var instance = await _workflowService.GetInstanceAsync(id);
        if (instance == null) return NotFound();
        await _workflowService.CancelInstanceAsync(id);
        return NoContent();
    }

    // Tasks
    [HttpGet("tasks/pending")]
    public async Task<IActionResult> GetPendingTasks()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        var isAdmin = User.IsInRole("admin");

        Log.Information("[API] GetPendingTasks: userId={UserId}, isAdmin={IsAdmin}", userId, isAdmin);

        // Admin sees all pending tasks, others see only their own
        var tasks = isAdmin
            ? await _taskService.GetAllPendingTasksAsync()
            : await _taskService.GetPendingTasksAsync(userId);

        Log.Information("[API] Found {TaskCount} pending tasks", tasks.Count);

        var result = new List<WorkflowTaskDto>();
        foreach (var task in tasks)
        {
            Log.Debug("[API] Processing task: id={TaskId}, assigneeId={AssigneeId}, instanceId={InstanceId}",
                task.Id, task.AssigneeId, task.InstanceId);

            var instance = await _workflowService.GetInstanceAsync(task.InstanceId);
            var startedByUser = instance != null ? await _userRepo.GetByIdAsync(instance.StartedBy) : null;
            var assigneeUser = await _userRepo.GetByIdAsync(task.AssigneeId);
            var definition = instance != null ? await _workflowService.GetDefinitionAsync(instance.DefinitionId) : null;
            var currentNode = definition?.Nodes.FirstOrDefault(n => n.Id == instance?.CurrentNodeId);

            var instanceDetailDto = new WorkflowInstanceDetailDto
            {
                Id = instance?.Id,
                Name = instance?.Name,
                Status = instance?.Status,
                StartedBy = instance?.StartedBy,
                StartedByName = startedByUser?.Username ?? "Unknown",
                StartedAt = instance?.StartedAt ?? DateTime.MinValue,
                CompletedAt = instance?.CompletedAt,
                CurrentNodeId = instance?.CurrentNodeId,
                CurrentNodeName = currentNode?.Name ?? "",
                SelectedFileIds = instance?.SelectedFileIds,
                Definition = definition
            };

            result.Add(new WorkflowTaskDto
            {
                Id = task.Id,
                InstanceId = task.InstanceId,
                NodeId = task.NodeId,
                NodeName = task.NodeName,
                AssigneeId = task.AssigneeId,
                AssigneeName = assigneeUser?.Username ?? task.AssigneeId,
                Status = task.Status,
                CreatedAt = task.CreatedAt,
                DueDate = task.DueDate,
                CompletedAt = task.CompletedAt,
                Comment = task.Comment,
                InstanceName = instance?.Name ?? "",
                StartedBy = instance?.StartedBy ?? "",
                StartedByName = startedByUser?.Username ?? "Unknown",
                Instance = instanceDetailDto
            });
        }

        Log.Information("[API] Returning {ResultCount} tasks", result.Count);
        return Ok(result);
    }

    [HttpGet("tasks/history")]
    public async Task<IActionResult> GetHistoryTasks()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        var isAdmin = User.IsInRole("admin");

        Log.Information("[API] GetHistoryTasks: userId={UserId}, isAdmin={IsAdmin}", userId, isAdmin);

        var tasks = isAdmin
            ? await _taskService.GetAllCompletedTasksAsync()
            : await _taskService.GetCompletedTasksAsync(userId);

        Log.Information("[API] Found {TaskCount} completed tasks", tasks.Count);

        var result = new List<WorkflowTaskDto>();
        foreach (var task in tasks)
        {
            var instance = await _workflowService.GetInstanceAsync(task.InstanceId);
            var startedByUser = instance != null ? await _userRepo.GetByIdAsync(instance.StartedBy) : null;
            var assigneeUser = await _userRepo.GetByIdAsync(task.AssigneeId);
            var definition = instance != null ? await _workflowService.GetDefinitionAsync(instance.DefinitionId) : null;
            var currentNode = definition?.Nodes.FirstOrDefault(n => n.Id == instance?.CurrentNodeId);

            var instanceDetailDto = new WorkflowInstanceDetailDto
            {
                Id = instance?.Id,
                Name = instance?.Name,
                Status = instance?.Status,
                StartedBy = instance?.StartedBy,
                StartedByName = startedByUser?.Username ?? "Unknown",
                StartedAt = instance?.StartedAt ?? DateTime.MinValue,
                CompletedAt = instance?.CompletedAt,
                CurrentNodeId = instance?.CurrentNodeId,
                CurrentNodeName = currentNode?.Name ?? "",
                SelectedFileIds = instance?.SelectedFileIds,
                Definition = definition
            };

            result.Add(new WorkflowTaskDto
            {
                Id = task.Id,
                InstanceId = task.InstanceId,
                NodeId = task.NodeId,
                NodeName = task.NodeName,
                AssigneeId = task.AssigneeId,
                AssigneeName = assigneeUser?.Username ?? task.AssigneeId,
                Status = task.Status,
                CreatedAt = task.CreatedAt,
                DueDate = task.DueDate,
                CompletedAt = task.CompletedAt,
                InstanceName = instance?.Name ?? "",
                StartedBy = instance?.StartedBy ?? "",
                StartedByName = startedByUser?.Username ?? "Unknown",
                Instance = instanceDetailDto
            });
        }

        Log.Information("[API] Returning {ResultCount} history tasks", result.Count);
        return Ok(result);
    }

    [HttpPost("tasks/{id}/approve")]
    public async Task<IActionResult> ApproveTask(string id, [FromBody] ApproveTaskRequest request)
    {
        await _taskService.ApproveTaskAsync(id, request.Comment, request.FormData);
        return NoContent();
    }

    [HttpPost("tasks/{id}/reject")]
    public async Task<IActionResult> RejectTask(string id, [FromBody] RejectTaskRequest request)
    {
        await _taskService.RejectTaskAsync(id, request.Comment);
        return NoContent();
    }

    [HttpGet("instances/{id}/history")]
    public async Task<IActionResult> GetInstanceHistory(string id)
    {
        var history = await _historyRepo.GetByInstanceAsync(id);
        return Ok(history);
    }
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
    public string Id { get; set; }
    public string Name { get; set; }
    public string Status { get; set; }
    public string StartedBy { get; set; }
    public string StartedByName { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string CurrentNodeId { get; set; }
    public string CurrentNodeName { get; set; }
    public List<string> SelectedFileIds { get; set; }
    public WorkflowDefinition Definition { get; set; }
    // 关联的业务实体
    public string EntityType { get; set; }
    public string EntityId { get; set; }
}

public class WorkflowTaskDto
{
    public string Id { get; set; }
    public string InstanceId { get; set; }
    public string NodeId { get; set; }
    public string NodeName { get; set; }
    public string AssigneeId { get; set; }
    public string AssigneeName { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string Comment { get; set; }
    public string InstanceName { get; set; }
    public string StartedBy { get; set; }
    public string StartedByName { get; set; }
    public WorkflowInstanceDetailDto Instance { get; set; }
}
