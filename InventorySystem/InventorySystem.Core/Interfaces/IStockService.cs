using InventorySystem.Core.Models;

namespace InventorySystem.Core.Interfaces;

public interface IStockService
{
    Task InboundAsync(string partId, int qty, string operatorId, string? note);
    Task OutboundAsync(string partId, int qty, string operatorId, string? projectId, string? recipientId, string? recipientName, string? note);
    Task LockAsync(string partId, int qty, string operatorId, string? projectId, string? note);
    Task UnlockAsync(string partId, int qty, string operatorId, string? projectId, string? note);
    Task ReturnAsync(string partId, int qty, string operatorId, string? projectId, string? note);
}
