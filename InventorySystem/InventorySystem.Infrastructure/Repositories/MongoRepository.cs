using System.Linq.Expressions;
using InventorySystem.Core.Interfaces;
using MongoDB.Driver;

namespace InventorySystem.Infrastructure.Repositories;

public abstract class MongoRepository<T> : IRepository<T>
{
    protected readonly IMongoCollection<T> _collection;

    protected MongoRepository(IMongoDatabase database, string collectionName)
    {
        _collection = database.GetCollection<T>(collectionName);
    }

    public async Task<T?> GetByIdAsync(string id)
    {
        if (string.IsNullOrEmpty(id) || !MongoDB.Bson.ObjectId.TryParse(id, out var objectId))
            return default;

        var filter = Builders<T>.Filter.Eq("_id", objectId);
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<List<T>> GetAllAsync() =>
        await _collection.Find(_ => true).ToListAsync();

    public async Task<List<T>> FindAsync(Expression<Func<T, bool>> filter) =>
        await _collection.Find(filter).ToListAsync();

    public async Task CreateAsync(T entity) =>
        await _collection.InsertOneAsync(entity);

    public virtual async Task UpdateAsync(string id, T entity)
    {
        if (string.IsNullOrEmpty(id) || !MongoDB.Bson.ObjectId.TryParse(id, out var objectId))
            return;

        var filter = Builders<T>.Filter.Eq("_id", objectId);
        await _collection.ReplaceOneAsync(filter, entity);
    }

    public async Task DeleteAsync(string id)
    {
        if (string.IsNullOrEmpty(id) || !MongoDB.Bson.ObjectId.TryParse(id, out var objectId))
            return;

        var filter = Builders<T>.Filter.Eq("_id", objectId);
        await _collection.DeleteOneAsync(filter);
    }
}
