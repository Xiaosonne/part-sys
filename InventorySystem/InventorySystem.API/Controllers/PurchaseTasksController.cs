using InventorySystem.Core.DTOs;
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
        IPurchaseTaskRepository repo, IStockService stockService,
        ISelectionPlanRepository planRepo, IProjectNodeRepository projectRepo,
        IUserRepository userRepo)
    {
        _repo = repo; _stockService = stockService;
        _planRepo = planRepo; _projectRepo = projectRepo; _userRepo = userRepo;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PurchaseTaskStatus? status = null)
    {
        List<PurchaseTask> tasks = status.HasValue
            ? await _repo.GetByStatusAsync(status.Value)
            : await _repo.GetAllAsync();

        var result = new List<PurchaseTaskListDto>();
        foreach (var task in tasks)
        {
            var dto = new PurchaseTaskListDto
            {
                Id = task.Id, SelectionPlanId = task.SelectionPlanId,
                SelectionItemId = task.SelectionItemId, PartId = task.PartId,
                PartName = task.PartName, LockedQty = task.LockedQty,
                RequiredQty = task.RequiredQty, Status = task.Status,
                CreatedBy = task.CreatedBy, CreatedAt = task.CreatedAt,
                UpdatedBy = task.UpdatedBy, UpdatedAt = task.UpdatedAt, Remark = task.Remark
            };

            if (!string.IsNullOrEmpty(task.SelectionPlanId))
            {
                var plan = await _planRepo.GetByIdAsync(task.SelectionPlanId);
                if (plan != null)
                {
                    dto.SelectionPlanName = plan.Name;
                    dto.ProjectId = plan.ProjectId;
                    if (!string.IsNullOrEmpty(plan.ProjectId))
                    {
                        var project = await _projectRepo.GetByIdAsync(plan.ProjectId);
                        if (project != null) dto.ProjectName = project.Name;
                    }
                }
            }
            if (!string.IsNullOrEmpty(task.CreatedBy))
            {
                var creator = await _userRepo.GetByIdAsync(task.CreatedBy);
                dto.CreatedByName = creator?.DisplayName ?? creator?.Username ?? "Unknown";
            }
            if (!string.IsNullOrEmpty(task.UpdatedBy))
            {
                var updater = await _userRepo.GetByIdAsync(task.UpdatedBy);
                dto.UpdatedByName = updater?.DisplayName ?? updater?.Username ?? "Unknown";
            }
            result.Add(dto);
        }
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var task = await _repo.GetByIdAsync(id);
        return task == null ? NotFound() : Ok(task);
    }

    [HttpGet("selection/{selectionPlanId}")]
    public async Task<IActionResult> GetBySelectionPlan(string selectionPlanId)
        => Ok(await _repo.GetBySelectionPlanIdAsync(selectionPlanId));

    [HttpGet("pending")]
    public async Task<IActionResult> GetPending()
        => Ok(await _repo.GetPendingTasksAsync());

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateStatusRequest request)
    {
        var task = await _repo.GetByIdAsync(id);
        if (task == null) return NotFound();
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        task.Status = request.Status; task.UpdatedAt = DateTime.UtcNow; task.UpdatedBy = userId;
        if (!string.IsNullOrEmpty(request.Remark)) task.Remark = request.Remark;
        await _repo.UpdateAsync(id, task);
        return Ok(task);
    }

    /// <summary>
    /// 采购到货 — 入库并锁定新库存到选型单，更新待采购数量为 0
    /// </summary>
    [HttpPost("{id}/receive")]
    public async Task<IActionResult> Receive(string id, [FromBody] ReceiveRequest? request = null)
    {
        var task = await _repo.GetByIdAsync(id);
        if (task == null) return NotFound();
        if (task.Status != PurchaseTaskStatus.InProgress)
            return BadRequest(new { message = $"当前状态为{task.Status}，无法标记为已收货" });

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

        // 1. 执行入库操作
        await _stockService.InboundAsync(task.PartId, task.RequiredQty, userId,
            StockSourceType.Purchase, $"采购入库: {task.PartName} x{task.RequiredQty}");

        // 2. 锁定新入库的库存到选型单（补齐 Submit 时未锁定的缺口）
        if (!string.IsNullOrEmpty(task.SelectionPlanId) && !string.IsNullOrEmpty(task.SelectionItemId))
        {
            var plan = await _planRepo.GetByIdAsync(task.SelectionPlanId);
            if (plan != null)
            {
                var item = plan.Items.FirstOrDefault(i => i.Id == task.SelectionItemId);
                if (item != null)
                {
                    // 锁定这批新入库的库存
                    await _stockService.LockAsync(
                        task.PartId, task.RequiredQty, userId,
                        plan.ProjectId, task.SelectionPlanId, task.SelectionItemId,
                        $"采购到货锁定: {task.PartName}",
                        StockSourceType.Purchase);

                    // 增加选型项的已锁定数量
                    item.LockedQty += task.RequiredQty;
                    item.PendingQty = 0;
                    await _planRepo.UpdateAsync(plan.Id, plan);
                }
            }
        }

        task.Status = PurchaseTaskStatus.Received; task.UpdatedAt = DateTime.UtcNow; task.UpdatedBy = userId;
        if (request != null && !string.IsNullOrEmpty(request.Remark)) task.Remark = request.Remark;
        await _repo.UpdateAsync(id, task);
        return Ok(task);
    }

    [HttpPost("{id}/start")]
    public async Task<IActionResult> Start(string id)
    {
        var task = await _repo.GetByIdAsync(id);
        if (task == null) return NotFound();
        if (task.Status != PurchaseTaskStatus.Pending)
            return BadRequest(new { message = $"当前状态为{task.Status}，无法开始采购" });
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        task.Status = PurchaseTaskStatus.InProgress; task.UpdatedAt = DateTime.UtcNow; task.UpdatedBy = userId;
        await _repo.UpdateAsync(id, task);
        return Ok(task);
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(string id)
    {
        var task = await _repo.GetByIdAsync(id);
        if (task == null) return NotFound();
        if (task.Status == PurchaseTaskStatus.Received)
            return BadRequest(new { message = "已收货的任务无法取消" });

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

        if (!string.IsNullOrEmpty(task.SelectionPlanId) && !string.IsNullOrEmpty(task.SelectionItemId))
        {
            var plan = await _planRepo.GetByIdAsync(task.SelectionPlanId);
            if (plan != null)
            {
                var item = plan.Items.FirstOrDefault(i => i.Id == task.SelectionItemId);
                if (item != null) { item.PendingQty = 0; item.PurchaseTaskId = null; await _planRepo.UpdateAsync(plan.Id, plan); }
            }
        }

        task.Status = PurchaseTaskStatus.Cancelled; task.UpdatedAt = DateTime.UtcNow; task.UpdatedBy = userId;
        await _repo.UpdateAsync(id, task);
        return Ok(task);
    }
}
