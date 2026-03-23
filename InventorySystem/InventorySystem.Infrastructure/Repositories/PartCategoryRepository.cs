using InventorySystem.Core.Interfaces;
using InventorySystem.Core.Models;
using MongoDB.Driver;

namespace InventorySystem.Infrastructure.Repositories;

public class PartCategoryRepository : MongoRepository<PartCategory>, IPartCategoryRepository
{
    public PartCategoryRepository(IMongoDatabase database) : base(database, "part_categories") { }

    public async Task<List<PartCategory>> GetByParentIdAsync(string? parentId)
    {
        if (string.IsNullOrEmpty(parentId))
        {
            return await _collection.Find(c => c.ParentId == null).SortBy(c => c.SortOrder).ToListAsync();
        }
        return await _collection.Find(c => c.ParentId == parentId).SortBy(c => c.SortOrder).ToListAsync();
    }

    public async Task<List<PartCategory>> GetTreeAsync()
    {
        return await _collection.Find(_ => true).SortBy(c => c.Path).ToListAsync();
    }

    public async Task<PartCategory?> GetByPathAsync(string path)
    {
        return await _collection.Find(c => c.Path == path).FirstOrDefaultAsync();
    }
}
