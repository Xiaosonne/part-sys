using InventorySystem.Core.Interfaces;
using InventorySystem.Core.Models;
using MongoDB.Driver;

namespace InventorySystem.Infrastructure.Repositories;

public class WorkspaceStructureRepository : MongoRepository<WorkspaceStructure>, IWorkspaceStructureRepository
{
    public WorkspaceStructureRepository(IMongoDatabase database) : base(database, "workspace_structure")
    {
    }

    public async Task<WorkspaceStructure> GetByProjectIdAsync(string projectId)
    {
        var filter = Builders<WorkspaceStructure>.Filter.Eq(x => x.ProjectId, projectId);
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    public new async Task UpdateAsync(string projectId, WorkspaceStructure structure)
    {
        var filter = Builders<WorkspaceStructure>.Filter.Eq(x => x.ProjectId, projectId);
        var existing = await _collection.Find(filter).FirstOrDefaultAsync();

        structure.UpdatedAt = DateTime.UtcNow;

        if (existing != null)
        {
            structure.Id = existing.Id;
            structure.CreatedAt = existing.CreatedAt;
            await _collection.ReplaceOneAsync(filter, structure);
        }
        else
        {
            structure.CreatedAt = DateTime.UtcNow;
            await _collection.InsertOneAsync(structure);
        }
    }

    public async Task DeleteByProjectIdAsync(string projectId)
    {
        var filter = Builders<WorkspaceStructure>.Filter.Eq(x => x.ProjectId, projectId);
        await _collection.DeleteOneAsync(filter);
    }
}
