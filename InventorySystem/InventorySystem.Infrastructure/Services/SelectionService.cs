using InventorySystem.Core.Interfaces;
using InventorySystem.Core.Models;
using Microsoft.Extensions.Logging;

namespace InventorySystem.Infrastructure.Services;

public class SelectionService : ISelectionService
{
    private readonly ISelectionPlanRepository _planRepo;
    private readonly IPurchaseTaskRepository _purchaseTaskRepo;
    private readonly IPartRepository _partRepo;
    private readonly IStockService _stockService;
    private readonly ILogger<SelectionService> _logger;

    public SelectionService(
        ISelectionPlanRepository planRepo,
        IPurchaseTaskRepository purchaseTaskRepo,
        IPartRepository partRepo,
        IStockService stockService,
        ILogger<SelectionService> logger)
    {
        _planRepo = planRepo;
        _purchaseTaskRepo = purchaseTaskRepo;
        _partRepo = partRepo;
        _stockService = stockService;
        _logger = logger;
    }

    public async Task<SelectionSubmitResult> SubmitAsync(string planId, string operatorId)
    {
        var result = new SelectionSubmitResult();
        var plan = await _planRepo.GetByIdAsync(planId)
            ?? throw new InvalidOperationException("选型单不存在");

        if (plan.Status != SelectionPlanStatus.Draft)
            throw new InvalidOperationException($"选型单状态为{plan.Status}，不能提交");

        // Track committed operations for rollback on failure
        var lockedItems = new List<(string PartId, int Qty, string? ProjectId)>();
        var createdTaskIds = new List<string>();

        try
        {
            foreach (var item in plan.Items)
            {
                if (string.IsNullOrEmpty(item.SelectedPartId))
                    throw new InvalidOperationException($"配件未选择: {item.PartName}");

                var part = await _partRepo.GetByIdAsync(item.SelectedPartId)
                    ?? throw new InvalidOperationException("配件不存在");

                int availableToLock = Math.Min(part.AvailableQty, item.RequiredQty);
                int pendingQty = item.RequiredQty - availableToLock;

                if (availableToLock > 0)
                {
                    await _stockService.LockAsync(
                        item.SelectedPartId, availableToLock, operatorId,
                        plan.ProjectId, plan.Id, item.Id,
                        $"选型单锁定: {plan.Name}",
                        StockSourceType.SelectionLock);
                    lockedItems.Add((item.SelectedPartId, availableToLock, plan.ProjectId));
                }

                item.LockedQty = availableToLock;
                item.PendingQty = pendingQty;

                result.LockedItems.Add(new LockResult
                {
                    ItemId = item.Id, PartId = part.Id, PartName = part.Name,
                    RequestedQty = item.RequiredQty, LockedQty = availableToLock, PendingQty = pendingQty
                });

                if (pendingQty > 0)
                {
                    var purchaseTask = new PurchaseTask
                    {
                        SelectionPlanId = planId, SelectionItemId = item.Id,
                        PartId = part.Id, PartName = part.Name,
                        LockedQty = availableToLock, RequiredQty = pendingQty,
                        Status = PurchaseTaskStatus.Pending, CreatedBy = operatorId
                    };
                    await _purchaseTaskRepo.CreateAsync(purchaseTask);
                    item.PurchaseTaskId = purchaseTask.Id;
                    createdTaskIds.Add(purchaseTask.Id);
                    result.CreatedPurchaseTasks.Add(purchaseTask);
                    _logger.LogInformation("生成采购任务: {PartName} x{RequiredQty} for 选型单 {PlanId}",
                        part.Name, pendingQty, planId);
                }
            }

            plan.Status = SelectionPlanStatus.Submitted;
            plan.SubmittedAt = DateTime.UtcNow;
            await _planRepo.UpdateAsync(planId, plan);

            result.Success = true;
            result.Message = $"成功锁定{result.LockedItems.Count}个配件，生成{result.CreatedPurchaseTasks.Count}个采购任务";
            _logger.LogInformation("选型单 {PlanId} 已提交: 锁定{LockedCount}个, 采购任务{CreatedCount}个",
                planId, result.LockedItems.Count, result.CreatedPurchaseTasks.Count);
            return result;
        }
        catch
        {
            // Rollback: unlock items that were already locked
            foreach (var (partId, qty, projectId) in lockedItems)
            {
                try
                {
                    await _stockService.UnlockAsync(partId, qty, operatorId,
                        projectId, planId, null,
                        "提交失败回滚",
                        StockSourceType.SelectionUnlock);
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(rollbackEx,
                        "回滚锁定失败: part={PartId}, qty={Qty}", partId, qty);
                }
            }
            // Delete purchase tasks that were created
            foreach (var taskId in createdTaskIds)
            {
                try { await _purchaseTaskRepo.DeleteAsync(taskId); }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(rollbackEx,
                        "删除采购任务失败: task={TaskId}", taskId);
                }
            }
            throw; // Re-throw so caller sees the error
        }
    }

    public async Task<SelectionOutboundResult> OutboundAsync(
        string planId, string itemId, int qty,
        string operatorId, string projectId,
        string? recipientId, string? recipientName)
    {
        var plan = await _planRepo.GetByIdAsync(planId)
            ?? throw new InvalidOperationException("选型单不存在");
        var item = plan.Items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new InvalidOperationException("选型配件不存在");

        if (qty > item.LockedQty)
            throw new InvalidOperationException($"出库数量{qty}超过已锁定数量{item.LockedQty}");

        await _stockService.OutboundLockedAsync(
            item.SelectedPartId, qty, operatorId,
            projectId, recipientId, recipientName,
            $"选型单出库: {plan.Name}",
            StockSourceType.SelectionOutbound);

        item.LockedQty -= qty;
        item.OutboundQty += qty;

        bool allCompleted = plan.Items.All(i => i.IsCompleted);
        if (allCompleted) plan.Status = SelectionPlanStatus.Completed;

        await _planRepo.UpdateAsync(planId, plan);
        _logger.LogInformation("选型单 {PlanId} 配件 {ItemId} 出库 {Qty}", planId, itemId, qty);

        return new SelectionOutboundResult
        {
            Success = true, ItemId = itemId, OutboundQty = qty,
            RemainingLockedQty = item.LockedQty, IsItemCompleted = item.IsCompleted,
            IsPlanCompleted = allCompleted
        };
    }

    public async Task CancelAsync(string planId, string operatorId)
    {
        var plan = await _planRepo.GetByIdAsync(planId)
            ?? throw new InvalidOperationException("选型单不存在");

        if (plan.Status != SelectionPlanStatus.Submitted)
            throw new InvalidOperationException($"选型单状态为{plan.Status}，不能取消");

        foreach (var item in plan.Items.Where(i => i.LockedQty > 0))
        {
            await _stockService.UnlockAsync(
                item.SelectedPartId, item.LockedQty, operatorId,
                plan.ProjectId, plan.Id, item.Id,
                $"选型单取消: {plan.Name}",
                StockSourceType.SelectionUnlock);
            item.LockedQty = 0;
        }

        var purchaseTasks = await _purchaseTaskRepo.GetBySelectionPlanIdAsync(planId);
        foreach (var task in purchaseTasks.Where(t => t.Status == PurchaseTaskStatus.Pending))
        {
            task.Status = PurchaseTaskStatus.Cancelled;
            task.UpdatedAt = DateTime.UtcNow;
            task.UpdatedBy = operatorId;
            await _purchaseTaskRepo.UpdateAsync(task.Id, task);
        }

        plan.Status = SelectionPlanStatus.Cancelled;
        await _planRepo.UpdateAsync(planId, plan);
        _logger.LogInformation("选型单 {PlanId} 已取消", planId);
    }
}
