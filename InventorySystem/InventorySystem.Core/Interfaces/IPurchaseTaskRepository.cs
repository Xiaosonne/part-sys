using InventorySystem.Core.Models;

namespace InventorySystem.Core.Interfaces;

public interface IPurchaseTaskRepository : IRepository<PurchaseTask>
{
    Task<List<PurchaseTask>> GetBySelectionPlanIdAsync(string selectionPlanId);
    Task<List<PurchaseTask>> GetPendingTasksAsync();
    Task<List<PurchaseTask>> GetByStatusAsync(PurchaseTaskStatus status);
}
