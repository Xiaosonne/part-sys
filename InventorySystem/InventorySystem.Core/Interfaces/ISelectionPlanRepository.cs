using InventorySystem.Core.Models;

namespace InventorySystem.Core.Interfaces;

public interface ISelectionPlanRepository : IRepository<SelectionPlan>
{
    Task<List<SelectionPlan>> GetByProjectIdAsync(string projectId);
}
