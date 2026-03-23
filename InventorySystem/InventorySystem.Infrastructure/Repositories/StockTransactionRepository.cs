using InventorySystem.Core.Interfaces;
using InventorySystem.Core.Models;
using MongoDB.Driver;

namespace InventorySystem.Infrastructure.Repositories;

public class StockTransactionRepository : MongoRepository<StockTransaction>, IStockTransactionRepository
{
    public StockTransactionRepository(IMongoDatabase database) : base(database, "stock_transactions") { }

    public async Task<List<StockTransaction>> GetByPartIdAsync(string partId)
    {
        return await _collection.Find(t => t.PartId == partId).SortByDescending(t => t.CreatedAt).ToListAsync();
    }

    public async Task<List<StockTransaction>> GetByProjectIdAsync(string projectId)
    {
        return await _collection.Find(t => t.ProjectId == projectId).SortByDescending(t => t.CreatedAt).ToListAsync();
    }
}
