using InventorySystem.Core.DTOs;
using InventorySystem.Core.Interfaces;
using InventorySystem.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace InventorySystem.API.Controllers;

/// <summary>
/// 工作流管理 — 流程定义 CRUD、流程实例启停、审批任务处理与历史
/// </summary>
[ApiController]
[Route("api/workflows")]
[Authorize]
public class WorkflowsController : ControllerBase
{
    private readonly IWorkflowService _workflowService;
    private readonly IWorkflowTaskService _taskService;
    private readonly IWorkflowHistoryRepository _historyRepo;
    private readonly IUserRepository _userRepo;

    public WorkflowsController(
        IWorkflowService workflowService, IWorkflowTaskService taskService,
        IWorkflowHistoryRepository historyRepo, IUserRepository userRepo)
    {
        _workflowService = workflowService;
        _taskService = taskService;
        _historyRepo = historyRepo;
        _userRepo = userRepo;
    }

    // ========== Definitions ==========

    /// <summary>创建流程定义</summary>
    [HttpPost("definitions")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> CreateDefinition([FromBody] WorkflowDefinition definition)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        definition.CreatedBy = userId;
        var result = await _workflowService.CreateDefinitionAsync(definition);
        return CreatedAtAction(nameof(GetDefinition), new { id = result.Id }, result);
    }

    /// <summary>获取流程定义列表（可按分类筛选）</summary>
    [HttpGet("definitions")]
    public async Task<IActionResult> GetDefinitions([FromQuery] string? category)
        => Ok(await _workflowService.GetDefinitionsAsync(category));

    /// <summary>获取流程定义详情</summary>
    [HttpGet("definitions/{id}")]
    public async Task<IActionResult> GetDefinition(string id)
    {
        var definition = await _workflowService.GetDefinitionAsync(id);
        return definition == null ? NotFound() : Ok(definition);
    }

    /// <summary>更新流程定义</summary>
    [HttpPut("definitions/{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> UpdateDefinition(string id, [FromBody] WorkflowDefinition definition)
    {
        var existing = await _workflowService.GetDefinitionAsync(id);
        if (existing == null) return NotFound();
        await _workflowService.UpdateDefinitionAsync(id, definition);
        return Ok(definition);
    }

    /// <summary>删除流程定义</summary>
    [HttpDelete("definitions/{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> DeleteDefinition(string id)
    {
        var existing = await _workflowService.GetDefinitionAsync(id);
        if (existing == null) return NotFound();
        await _workflowService.DeleteDefinitionAsync(id);
        return NoContent();
    }

    // ========== Instances ==========

    /// <summary>启动流程实例</summary>
    [HttpPost("instances")]
    public async Task<IActionResult> StartInstance([FromBody] StartInstanceRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        var instance = await _workflowService.StartInstanceAsync(
            request.DefinitionId, request.EntityType, request.EntityId,
            userId, request.SelectedFileIds, request.Name, request.FormData);
        return CreatedAtAction(nameof(GetInstance), new { id = instance.Id }, instance);
    }

    /// <summary>获取流程实例详情（含关联用户和当前节点信息）</summary>
    [HttpGet("instances/{id}")]
    public async Task<IActionResult> GetInstance(string id)
    {
        var instance = await _workflowService.GetInstanceAsync(id);
        if (instance == null) return NotFound();

        var user = await _userRepo.GetByIdAsync(instance.StartedBy);
        var definition = await _workflowService.GetDefinitionAsync(instance.DefinitionId);
        var currentNode = definition?.Nodes.FirstOrDefault(n => n.Id == instance.CurrentNodeId);

        return Ok(new WorkflowInstanceDetailDto
        {
            Id = instance.Id, Name = instance.Name, Status = instance.Status,
            StartedBy = instance.StartedBy, StartedByName = user?.Username ?? "Unknown",
            StartedAt = instance.StartedAt, CompletedAt = instance.CompletedAt,
            CurrentNodeId = instance.CurrentNodeId, CurrentNodeName = currentNode?.Name ?? "",
            SelectedFileIds = instance.SelectedFileIds, Definition = definition,
            EntityType = instance.EntityType, EntityId = instance.EntityId
        });
    }

    /// <summary>获取我发起的流程实例列表</summary>
    [HttpGet("instances")]
    public async Task<IActionResult> GetMyInstances()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        return Ok(await _workflowService.GetInstancesByUserAsync(userId));
    }

    /// <summary>取消流程实例</summary>
    [HttpPost("instances/{id}/cancel")]
    public async Task<IActionResult> CancelInstance(string id)
    {
        var instance = await _workflowService.GetInstanceAsync(id);
        if (instance == null) return NotFound();
        await _workflowService.CancelInstanceAsync(id);
        return NoContent();
    }

    // ========== Tasks ==========

    /// <summary>获取待办审批任务列表（admin 看到全部，其他人看到自己的）</summary>
    [HttpGet("tasks/pending")]
    public async Task<IActionResult> GetPendingTasks()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        var isAdmin = User.IsInRole("admin");
        Log.Information("[API] GetPendingTasks: userId={UserId}, isAdmin={IsAdmin}", userId, isAdmin);

        var tasks = isAdmin
            ? await _taskService.GetAllPendingTasksAsync()
            : await _taskService.GetPendingTasksAsync(userId);

        Log.Information("[API] Found {TaskCount} pending tasks", tasks.Count);
        var result = await EnrichTaskDtos(tasks);
        Log.Information("[API] Returning {ResultCount} tasks", result.Count);
        return Ok(result);
    }

    /// <summary>获取已处理审批任务历史（admin 看到全部，其他人看到自己的）</summary>
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
        var result = await EnrichTaskDtos(tasks);
        Log.Information("[API] Returning {ResultCount} history tasks", result.Count);
        return Ok(result);
    }

    /// <summary>审批通过当前任务（自动流转到下一节点）</summary>
    [HttpPost("tasks/{id}/approve")]
    public async Task<IActionResult> ApproveTask(string id, [FromBody] ApproveTaskRequest request)
    {
        await _taskService.ApproveTaskAsync(id, request.Comment, request.FormData);
        return NoContent();
    }

    /// <summary>驳回当前任务（流程实例标记为已驳回）</summary>
    [HttpPost("tasks/{id}/reject")]
    public async Task<IActionResult> RejectTask(string id, [FromBody] RejectTaskRequest request)
    {
        await _taskService.RejectTaskAsync(id, request.Comment);
        return NoContent();
    }

    /// <summary>获取流程实例的操作历史</summary>
    [HttpGet("instances/{id}/history")]
    public async Task<IActionResult> GetInstanceHistory(string id)
        => Ok(await _historyRepo.GetByInstanceAsync(id));

    /// <summary>
    /// 将审批任务列表拼装为带关联信息的 DTO（任务+实例+定义+用户）
    /// </summary>
    private async Task<List<WorkflowTaskDto>> EnrichTaskDtos(List<WorkflowTask> tasks)
    {
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

            result.Add(new WorkflowTaskDto
            {
                Id = task.Id, InstanceId = task.InstanceId,
                NodeId = task.NodeId, NodeName = task.NodeName,
                AssigneeId = task.AssigneeId, AssigneeName = assigneeUser?.Username ?? task.AssigneeId,
                Status = task.Status, CreatedAt = task.CreatedAt,
                DueDate = task.DueDate, CompletedAt = task.CompletedAt, Comment = task.Comment,
                InstanceName = instance?.Name ?? "",
                StartedBy = instance?.StartedBy ?? "",
                StartedByName = startedByUser?.Username ?? "Unknown",
                Instance = new WorkflowInstanceDetailDto
                {
                    Id = instance?.Id, Name = instance?.Name, Status = instance?.Status,
                    StartedBy = instance?.StartedBy,
                    StartedByName = startedByUser?.Username ?? "Unknown",
                    StartedAt = instance?.StartedAt ?? DateTime.MinValue,
                    CompletedAt = instance?.CompletedAt,
                    CurrentNodeId = instance?.CurrentNodeId, CurrentNodeName = currentNode?.Name ?? "",
                    SelectedFileIds = instance?.SelectedFileIds, Definition = definition
                }
            });
        }
        return result;
    }
}
