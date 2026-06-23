using InventorySystem.Core.Models;

namespace InventorySystem.Core.Interfaces;

public interface IStockService
{
    Task InboundAsync(string partId, int qty, string operatorId, StockSourceType sourceType, string? note);
    Task OutboundAsync(string partId, int qty, string operatorId, string? projectId, string? recipientId, string? recipientName, string? note);
    Task OutboundLockedAsync(string partId, int qty, string operatorId, string? projectId, string? recipientId, string? recipientName, string? note, StockSourceType sourceType = StockSourceType.Manual);
    Task LockAsync(string partId, int qty, string operatorId, string? projectId, string? selectionPlanId, string? selectionItemId, string? note, StockSourceType sourceType = StockSourceType.Manual);
    Task UnlockAsync(string partId, int qty, string operatorId, string? projectId, string? selectionPlanId, string? selectionItemId, string? note, StockSourceType sourceType = StockSourceType.Manual);
    Task ReturnAsync(string partId, int qty, string operatorId, string? projectId, string? note);
}
