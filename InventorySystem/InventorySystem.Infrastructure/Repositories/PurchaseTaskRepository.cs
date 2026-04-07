using InventorySystem.Core.Interfaces;
using InventorySystem.Core.Models;
using MongoDB.Driver;

namespace InventorySystem.Infrastructure.Repositories;

public class PurchaseTaskRepository : MongoRepository<PurchaseTask>, IPurchaseTaskRepository
{
    public PurchaseTaskRepository(IMongoDatabase database) : base(database, "purchase_tasks") { }

    public async Task<List<PurchaseTask>> GetBySelectionPlanIdAsync(string selectionPlanId)
    {
        return await _collection.Find(t => t.SelectionPlanId == selectionPlanId).ToListAsync();
    }

    public async Task<List<PurchaseTask>> GetPendingTasksAsync()
    {
        return await _collection.Find(t => t.Status == PurchaseTaskStatus.Pending).ToListAsync();
    }

    public async Task<List<PurchaseTask>> GetByStatusAsync(PurchaseTaskStatus status)
    {
        return await _collection.Find(t => t.Status == status).ToListAsync();
    }
}
