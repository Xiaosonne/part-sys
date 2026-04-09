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

    /// <summary>
    /// 提交选型单
    /// 1. 检查每个配件的可用库存
    /// 2. 尽可能锁定（AvailableQty vs RequiredQty）
    /// 3. 库存不够的生成采购任务
    /// </summary>
    public async Task<SelectionSubmitResult> SubmitAsync(string planId, string operatorId)
    {
        var result = new SelectionSubmitResult();
        var plan = await _planRepo.GetByIdAsync(planId)
            ?? throw new InvalidOperationException("选型单不存在");

        if (plan.Status != SelectionPlanStatus.Draft)
            throw new InvalidOperationException($"选型单状态为{plan.Status}，不能提交");

        foreach (var item in plan.Items)
        {
            if (string.IsNullOrEmpty(item.SelectedPartId))
                throw new InvalidOperationException($"配件未选择: {item.PartName}");

            var part = await _partRepo.GetByIdAsync(item.SelectedPartId)
                ?? throw new InvalidOperationException("配件不存在");

            // 计算可锁数量
            int availableToLock = Math.Min(part.AvailableQty, item.RequiredQty);
            int pendingQty = item.RequiredQty - availableToLock;

            // 锁定可用库存
            if (availableToLock > 0)
            {
                await _stockService.LockAsync(
                    item.SelectedPartId,
                    availableToLock,
                    operatorId,
                    plan.ProjectId,
                    plan.Id,
                    item.Id,
                    $"选型单锁定: {plan.Name}");
            }

            // 更新配件的使用次数
            // TODO: 实现 PartUsageStats 统计

            // 更新选型配件数量（状态是计算属性，自动从数量派生）
            item.LockedQty = availableToLock;
            item.PendingQty = pendingQty;

            result.LockedItems.Add(new LockResult
            {
                ItemId = item.Id,
                PartId = part.Id,
                PartName = part.Name,
                RequestedQty = item.RequiredQty,
                LockedQty = availableToLock,
                PendingQty = pendingQty
            });

            // 生成采购任务（如果库存不够）
            if (pendingQty > 0)
            {
                var purchaseTask = new PurchaseTask
                {
                    SelectionPlanId = planId,
                    SelectionItemId = item.Id,
                    PartId = part.Id,
                    PartName = part.Name,
                    LockedQty = availableToLock,
                    RequiredQty = pendingQty,
                    Status = PurchaseTaskStatus.Pending,
                    CreatedBy = operatorId
                };
                await _purchaseTaskRepo.CreateAsync(purchaseTask);
                item.PurchaseTaskId = purchaseTask.Id;

                result.CreatedPurchaseTasks.Add(purchaseTask);
                _logger.LogInformation("生成采购任务: {PartName} x{RequiredQty} for 选型单 {PlanId}",
                    part.Name, pendingQty, planId);
            }
        }

        // 更新选型单状态
        plan.Status = SelectionPlanStatus.Submitted;
        plan.SubmittedAt = DateTime.UtcNow;
        await _planRepo.UpdateAsync(planId, plan);

        result.Success = true;
        result.Message = $"成功锁定{result.LockedItems.Count}个配件，生成{result.CreatedPurchaseTasks.Count}个采购任务";

        _logger.LogInformation("选型单 {PlanId} 已提交: 锁定{LockedCount}个, 采购任务{CreatedCount}个",
            planId, result.LockedItems.Count, result.CreatedPurchaseTasks.Count);

        return result;
    }

    /// <summary>
    /// 配件出库 - 从已锁定的配件中出库
    /// </summary>
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

        // 出库：减少锁定，增加已出库
        await _stockService.OutboundAsync(
            item.SelectedPartId,
            qty,
            operatorId,
            projectId,
            recipientId,
            recipientName,
            $"选型单出库: {plan.Name}");

        item.LockedQty -= qty;
        item.OutboundQty += qty;

        // 检查是否所有配件都完成了
        bool allCompleted = plan.Items.All(i => i.IsCompleted);
        if (allCompleted)
        {
            plan.Status = SelectionPlanStatus.Completed;
        }

        await _planRepo.UpdateAsync(planId, plan);

        _logger.LogInformation("选型单 {PlanId} 配件 {ItemId} 出库 {Qty}",
            planId, itemId, qty);

        return new SelectionOutboundResult
        {
            Success = true,
            ItemId = itemId,
            OutboundQty = qty,
            RemainingLockedQty = item.LockedQty,
            IsItemCompleted = item.IsCompleted,
            IsPlanCompleted = allCompleted
        };
    }

    /// <summary>
    /// 取消选型单 - 解锁所有未出库的配件
    /// </summary>
    public async Task CancelAsync(string planId, string operatorId)
    {
        var plan = await _planRepo.GetByIdAsync(planId)
            ?? throw new InvalidOperationException("选型单不存在");

        if (plan.Status != SelectionPlanStatus.Submitted)
            throw new InvalidOperationException($"选型单状态为{plan.Status}，不能取消");

        foreach (var item in plan.Items.Where(i => i.LockedQty > 0))
        {
            // 解锁未出库的配件
            await _stockService.UnlockAsync(
                item.SelectedPartId,
                item.LockedQty,
                operatorId,
                plan.ProjectId,
                $"选型单取消: {plan.Name}");

            item.LockedQty = 0;
        }

        // 更新采购任务状态（如果存在）
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
