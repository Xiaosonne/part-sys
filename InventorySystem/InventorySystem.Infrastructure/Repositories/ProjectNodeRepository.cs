using InventorySystem.Core.Interfaces;
using InventorySystem.Core.Models;
using MongoDB.Driver;

namespace InventorySystem.Infrastructure.Repositories;

public class ProjectNodeRepository : MongoRepository<ProjectNode>, IProjectNodeRepository
{
    public ProjectNodeRepository(IMongoDatabase database) : base(database, "project_nodes") { }

    public async Task<List<ProjectNode>> GetChildrenAsync(string? parentId)
    {
        if (parentId == null)
            return await _collection.Find(n => n.ParentId == null).ToListAsync();
        return await _collection.Find(n => n.ParentId == parentId).ToListAsync();
    }
}
