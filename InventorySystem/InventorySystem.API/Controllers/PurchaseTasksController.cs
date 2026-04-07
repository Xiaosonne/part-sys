using InventorySystem.Core.Interfaces;
using InventorySystem.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventorySystem.API.Controllers;

[ApiController]
[Route("api/purchase-tasks")]
[Authorize]
public class PurchaseTasksController : ControllerBase
{
    private readonly IPurchaseTaskRepository _repo;

    public PurchaseTasksController(IPurchaseTaskRepository repo)
    {
        _repo = repo;
    }

    /// <summary>
    /// 获取所有采购任务
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PurchaseTaskStatus? status = null)
    {
        if (status.HasValue)
            return Ok(await _repo.GetByStatusAsync(status.Value));
        return Ok(await _repo.GetAllAsync());
    }

    /// <summary>
    /// 获取单个采购任务
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var task = await _repo.GetByIdAsync(id);
        return task == null ? NotFound() : Ok(task);
    }

    /// <summary>
    /// 获取某个选型单的所有采购任务
    /// </summary>
    [HttpGet("selection/{selectionPlanId}")]
    public async Task<IActionResult> GetBySelectionPlan(string selectionPlanId)
    {
        return Ok(await _repo.GetBySelectionPlanIdAsync(selectionPlanId));
    }

    /// <summary>
    /// 获取待采购任务列表
    /// </summary>
    [HttpGet("pending")]
    public async Task<IActionResult> GetPending()
    {
        return Ok(await _repo.GetPendingTasksAsync());
    }

    /// <summary>
    /// 更新采购任务状态
    /// </summary>
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateStatusRequest request)
    {
        var task = await _repo.GetByIdAsync(id);
        if (task == null) return NotFound();

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        task.Status = request.Status;
        task.UpdatedAt = DateTime.UtcNow;
        task.UpdatedBy = userId;
        if (!string.IsNullOrEmpty(request.Remark))
            task.Remark = request.Remark;

        await _repo.UpdateAsync(id, task);
        return Ok(task);
    }

    /// <summary>
    /// 采购到货 - 状态从 InProgress 设置为 Received
    /// </summary>
    [HttpPost("{id}/receive")]
    public async Task<IActionResult> Receive(string id, [FromBody] ReceiveRequest? request = null)
    {
        var task = await _repo.GetByIdAsync(id);
        if (task == null) return NotFound();

        if (task.Status != PurchaseTaskStatus.InProgress)
            return BadRequest(new { message = $"当前状态为{task.Status}，无法标记为已收货" });

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        task.Status = PurchaseTaskStatus.Received;
        task.UpdatedAt = DateTime.UtcNow;
        task.UpdatedBy = userId;
        if (request != null && !string.IsNullOrEmpty(request.Remark))
            task.Remark = request.Remark;

        await _repo.UpdateAsync(id, task);
        return Ok(task);
    }

    /// <summary>
    /// 开始采购 - 状态从 Pending 设置为 InProgress
    /// </summary>
    [HttpPost("{id}/start")]
    public async Task<IActionResult> Start(string id)
    {
        var task = await _repo.GetByIdAsync(id);
        if (task == null) return NotFound();

        if (task.Status != PurchaseTaskStatus.Pending)
            return BadRequest(new { message = $"当前状态为{task.Status}，无法开始采购" });

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        task.Status = PurchaseTaskStatus.InProgress;
        task.UpdatedAt = DateTime.UtcNow;
        task.UpdatedBy = userId;

        await _repo.UpdateAsync(id, task);
        return Ok(task);
    }

    /// <summary>
    /// 取消采购任务
    /// </summary>
    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(string id)
    {
        var task = await _repo.GetByIdAsync(id);
        if (task == null) return NotFound();

        if (task.Status == PurchaseTaskStatus.Received)
            return BadRequest(new { message = "已收货的任务无法取消" });

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        task.Status = PurchaseTaskStatus.Cancelled;
        task.UpdatedAt = DateTime.UtcNow;
        task.UpdatedBy = userId;

        await _repo.UpdateAsync(id, task);
        return Ok(task);
    }
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
