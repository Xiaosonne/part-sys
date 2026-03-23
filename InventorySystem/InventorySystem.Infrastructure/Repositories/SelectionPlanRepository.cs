using InventorySystem.Core.Interfaces;
using InventorySystem.Core.Models;
using MongoDB.Driver;

namespace InventorySystem.Infrastructure.Repositories;

public class SelectionPlanRepository : MongoRepository<SelectionPlan>, ISelectionPlanRepository
{
    public SelectionPlanRepository(IMongoDatabase database) : base(database, "selection_plans") { }

    public async Task<List<SelectionPlan>> GetByProjectIdAsync(string projectId)
    {
        return await _collection.Find(s => s.ProjectId == projectId).ToListAsync();
    }
}
