namespace InventorySystem.Core.Models;

/// <summary>
/// 库存操作来源类型
/// </summary>
public enum StockSourceType
{
    /// <summary>手动操作</summary>
    Manual,

    /// <summary>采购入库</summary>
    Purchase,

    /// <summary>选型锁定</summary>
    SelectionLock,

    /// <summary>选型解锁</summary>
    SelectionUnlock,

    /// <summary>选型出库</summary>
    SelectionOutbound
}
