using InventorySystem.Core.Models;

namespace InventorySystem.Core.Interfaces;

/// <summary>
/// 选型服务 - 处理选型提交、锁库、出库、取消等业务逻辑
/// </summary>
public interface ISelectionService
{
    /// <summary>
    /// 提交选型单 - 锁定库存，库存不够时生成采购任务
    /// </summary>
    /// <param name="planId">选型单ID</param>
    /// <param name="operatorId">操作人ID</param>
    /// <returns>提交结果</returns>
    Task<SelectionSubmitResult> SubmitAsync(string planId, string operatorId);

    /// <summary>
    /// 配件出库 - 从已锁定的配件中出库
    /// </summary>
    /// <param name="planId">选型单ID</param>
    /// <param name="itemId">选型配件ID</param>
    /// <param name="qty">出库数量</param>
    /// <param name="operatorId">操作人ID</param>
    /// <param name="projectId">项目ID</param>
    /// <param name="recipientId">领用人ID</param>
    /// <param name="recipientName">领用人名称</param>
    /// <returns>出库结果</returns>
    Task<SelectionOutboundResult> OutboundAsync(string planId, string itemId, int qty, string operatorId, string projectId, string? recipientId, string? recipientName);

    /// <summary>
    /// 取消选型单 - 解锁所有未出库的配件
    /// </summary>
    /// <param name="planId">选型单ID</param>
    /// <param name="operatorId">操作人ID</param>
    Task CancelAsync(string planId, string operatorId);
}

/// <summary>
/// 选型提交结果
/// </summary>
public class SelectionSubmitResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public List<LockResult> LockedItems { get; set; } = new();
    public List<PurchaseTask> CreatedPurchaseTasks { get; set; } = new();
}

/// <summary>
/// 锁定结果
/// </summary>
public class LockResult
{
    public string ItemId { get; set; } = string.Empty;
    public string PartId { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
    public int RequestedQty { get; set; }
    public int LockedQty { get; set; }
    public int PendingQty { get; set; }
    public bool IsFullyLocked => PendingQty == 0;
}

/// <summary>
/// 出库结果
/// </summary>
public class SelectionOutboundResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string ItemId { get; set; } = string.Empty;
    public int OutboundQty { get; set; }
    public int RemainingLockedQty { get; set; }
    public bool IsItemCompleted { get; set; }
    public bool IsPlanCompleted { get; set; }
}
