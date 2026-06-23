using InventorySystem.Core.Interfaces;
using InventorySystem.Core.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace InventorySystem.Infrastructure.Repositories;

public class PartRepository : MongoRepository<Part>, IPartRepository
{
    public PartRepository(IMongoDatabase database) : base(database, "parts") { }

    public override async Task UpdateAsync(string id, Part entity)
    {
        var filter = Builders<Part>.Filter.Eq("_id", MongoDB.Bson.ObjectId.Parse(id))
            & Builders<Part>.Filter.Eq(p => p.Version, entity.Version);

        entity.Version++;
        var result = await _collection.ReplaceOneAsync(filter, entity);

        if (result.ModifiedCount == 0)
        {
            var current = await GetByIdAsync(id);
            if (current == null) throw new InvalidOperationException("配件不存在");
            throw new InvalidOperationException($"配件 '{current.Name}' 已被其他操作修改，请刷新后重试");
        }
    }

    public async Task<List<Part>> FilterBySpecsAsync(string? category, List<(string key, string op, string value)> criteria)
    {
        var builder = Builders<Part>.Filter;
        var filters = new List<FilterDefinition<Part>>();

        if (!string.IsNullOrEmpty(category))
            filters.Add(builder.Eq(p => p.Category, category));

        var parts = await _collection.Find(filters.Count > 0 ? builder.And(filters) : builder.Empty).ToListAsync();

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

    public async Task<bool> UpdateQuantitiesAsync(string id, int lockedDelta, int totalDelta, int? minAvailableQty = null, int? minLockedQty = null)
    {
        var builder = Builders<Part>.Filter;
        var filter = builder.Eq("_id", MongoDB.Bson.ObjectId.Parse(id));

        if (minAvailableQty.HasValue)
        {
            filter &= new BsonDocumentFilterDefinition<Part>(
                new BsonDocument("$expr", new BsonDocument("$gte", new BsonArray {
                    new BsonDocument("$subtract", new BsonArray { "$TotalQty", "$LockedQty" }),
                    minAvailableQty.Value
                }))
            );
        }
        if (minLockedQty.HasValue)
            filter &= builder.Gte(p => p.LockedQty, minLockedQty.Value);

        var update = Builders<Part>.Update
            .Inc(p => p.LockedQty, lockedDelta)
            .Inc(p => p.TotalQty, totalDelta)
            .Inc(p => p.Version, 1)
            .Set(p => p.UpdatedAt, DateTime.UtcNow);

        var result = await _collection.UpdateOneAsync(filter, update);
        return result.ModifiedCount > 0;
    }
}
