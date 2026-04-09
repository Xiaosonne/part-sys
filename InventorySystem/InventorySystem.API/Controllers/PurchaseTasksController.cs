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
    private readonly IStockService _stockService;
    private readonly ISelectionPlanRepository _planRepo;
    private readonly IProjectNodeRepository _projectRepo;
    private readonly IUserRepository _userRepo;

    public PurchaseTasksController(
        IPurchaseTaskRepository repo,
        IStockService stockService,
        ISelectionPlanRepository planRepo,
        IProjectNodeRepository projectRepo,
        IUserRepository userRepo)
    {
        _repo = repo;
        _stockService = stockService;
        _planRepo = planRepo;
        _projectRepo = projectRepo;
        _userRepo = userRepo;
    }

    /// <summary>
    /// 获取所有采购任务（带关联信息）
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PurchaseTaskStatus? status = null)
    {
        List<PurchaseTask> tasks;
        if (status.HasValue)
            tasks = await _repo.GetByStatusAsync(status.Value);
        else
            tasks = await _repo.GetAllAsync();

        var result = new List<PurchaseTaskListDto>();
        foreach (var task in tasks)
        {
            var dto = new PurchaseTaskListDto
            {
                Id = task.Id,
                SelectionPlanId = task.SelectionPlanId,
                SelectionItemId = task.SelectionItemId,
                PartId = task.PartId,
                PartName = task.PartName,
                LockedQty = task.LockedQty,
                RequiredQty = task.RequiredQty,
                Status = task.Status,
                CreatedBy = task.CreatedBy,
                CreatedAt = task.CreatedAt,
                UpdatedBy = task.UpdatedBy,
                UpdatedAt = task.UpdatedAt,
                Remark = task.Remark
            };

            // Enrich with selection plan name
            if (!string.IsNullOrEmpty(task.SelectionPlanId))
            {
                var plan = await _planRepo.GetByIdAsync(task.SelectionPlanId);
                if (plan != null)
                {
                    dto.SelectionPlanName = plan.Name;
                    dto.ProjectId = plan.ProjectId;

                    // Enrich with project name
                    if (!string.IsNullOrEmpty(plan.ProjectId))
                    {
                        var project = await _projectRepo.GetByIdAsync(plan.ProjectId);
                        if (project != null)
                        {
                            dto.ProjectName = project.Name;
                        }
                    }
                }
            }

            // Enrich with creator name
            if (!string.IsNullOrEmpty(task.CreatedBy))
            {
                var creator = await _userRepo.GetByIdAsync(task.CreatedBy);
                dto.CreatedByName = creator?.DisplayName ?? creator?.Username ?? "Unknown";
            }

            // Enrich with updater name
            if (!string.IsNullOrEmpty(task.UpdatedBy))
            {
                var updater = await _userRepo.GetByIdAsync(task.UpdatedBy);
                dto.UpdatedByName = updater?.DisplayName ?? updater?.Username ?? "Unknown";
            }

            result.Add(dto);
        }

        return Ok(result);
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
    /// 采购到货 - 状态从 InProgress 设置为 Received，同时执行入库操作
    /// </summary>
    [HttpPost("{id}/receive")]
    public async Task<IActionResult> Receive(string id, [FromBody] ReceiveRequest? request = null)
    {
        var task = await _repo.GetByIdAsync(id);
        if (task == null) return NotFound();

        if (task.Status != PurchaseTaskStatus.InProgress)
            return BadRequest(new { message = $"当前状态为{task.Status}，无法标记为已收货" });

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

        // 执行入库操作：采购到货数量增加库存
        await _stockService.InboundAsync(
            task.PartId,
            task.RequiredQty,
            userId,
            StockSourceType.Purchase,
            $"采购入库: {task.PartName} x{task.RequiredQty}");

        // 更新选型单配件的待采购数量为 0
        if (!string.IsNullOrEmpty(task.SelectionPlanId) && !string.IsNullOrEmpty(task.SelectionItemId))
        {
            var plan = await _planRepo.GetByIdAsync(task.SelectionPlanId);
            if (plan != null)
            {
                var item = plan.Items.FirstOrDefault(i => i.Id == task.SelectionItemId);
                if (item != null)
                {
                    item.PendingQty = 0;
                    await _planRepo.UpdateAsync(plan.Id, plan);
                }
            }
        }

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

/// <summary>
/// 采购任务列表 DTO（带关联信息）
/// </summary>
public class PurchaseTaskListDto
{
    public string Id { get; set; }
    public string SelectionPlanId { get; set; }
    public string SelectionItemId { get; set; }
    public string PartId { get; set; }
    public string PartName { get; set; }
    public int LockedQty { get; set; }
    public int RequiredQty { get; set; }
    public PurchaseTaskStatus Status { get; set; }
    public string CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? Remark { get; set; }

    // 关联信息
    public string? SelectionPlanName { get; set; }
    public string? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public string? CreatedByName { get; set; }
    public string? UpdatedByName { get; set; }
}
