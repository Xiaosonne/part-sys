using InventorySystem.Core.Interfaces;
using InventorySystem.Core.Models;
using MongoDB.Driver;

namespace InventorySystem.Infrastructure.Repositories;

public class SpecTemplateRepository : MongoRepository<SpecTemplate>, ISpecTemplateRepository
{
    public SpecTemplateRepository(IMongoDatabase database) : base(database, "spec_templates") { }

    public async Task<SpecTemplate?> GetByCategoryAsync(string category)
    {
        return await _collection.Find(t => t.Category == category).FirstOrDefaultAsync();
    }
}
