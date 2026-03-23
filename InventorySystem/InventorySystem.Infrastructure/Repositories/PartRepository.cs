using InventorySystem.Core.Interfaces;
using InventorySystem.Core.Models;
using MongoDB.Driver;

namespace InventorySystem.Infrastructure.Repositories;

public class PartRepository : MongoRepository<Part>, IPartRepository
{
    public PartRepository(IMongoDatabase database) : base(database, "parts") { }

    public async Task<List<Part>> FilterBySpecsAsync(string? category, List<(string key, string op, string value)> criteria)
    {
        var builder = Builders<Part>.Filter;
        var filters = new List<FilterDefinition<Part>>();

        if (!string.IsNullOrEmpty(category))
            filters.Add(builder.Eq(p => p.Category, category));

        var parts = await _collection.Find(filters.Count > 0 ? builder.And(filters) : builder.Empty).ToListAsync();

        // Apply spec filters in memory
        if (criteria.Count == 0) return parts;

        return parts.Where(p => criteria.All(c =>
        {
            var spec = p.Specs.FirstOrDefault(s => s.Key == c.key);
            if (spec == null) return false;
            return c.op switch
            {
                "eq" => spec.Value == c.value,
                "contains" => spec.Value.Contains(c.value, StringComparison.OrdinalIgnoreCase),
                "gte" => double.TryParse(spec.Value, out var sv) && double.TryParse(c.value, out var cv) && sv >= cv,
                "lte" => double.TryParse(spec.Value, out var sv2) && double.TryParse(c.value, out var cv2) && sv2 <= cv2,
                _ => false
            };
        })).ToList();
    }

    public async Task UpdateQuantitiesAsync(string id, int availableDelta, int lockedDelta, int totalDelta)
    {
        var filter = Builders<Part>.Filter.Eq("_id", MongoDB.Bson.ObjectId.Parse(id));
        var update = Builders<Part>.Update
            .Inc(p => p.AvailableQty, availableDelta)
            .Inc(p => p.LockedQty, lockedDelta)
            .Inc(p => p.TotalQty, totalDelta)
            .Set(p => p.UpdatedAt, DateTime.UtcNow);
        await _collection.UpdateOneAsync(filter, update);
    }
}
