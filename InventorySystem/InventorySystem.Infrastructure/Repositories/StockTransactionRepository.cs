using InventorySystem.Core.Interfaces;
using InventorySystem.Core.Models;
using InventorySystem.Core.DTOs;
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

    public async Task<List<StockTransaction>> QueryAsync(TransactionQueryDto query)
    {
        var builder = Builders<StockTransaction>.Filter;
        var filters = new List<FilterDefinition<StockTransaction>>();

        if (!string.IsNullOrEmpty(query.PartId))
            filters.Add(builder.Eq(t => t.PartId, query.PartId));

        if (!string.IsNullOrEmpty(query.ProjectId))
            filters.Add(builder.Eq(t => t.ProjectId, query.ProjectId));

        if (!string.IsNullOrEmpty(query.Type))
            filters.Add(builder.Eq(t => t.Type, query.Type));

        if (query.SourceType.HasValue)
            filters.Add(builder.Eq(t => t.SourceType, query.SourceType.Value));

        if (!string.IsNullOrEmpty(query.OperatorId))
            filters.Add(builder.Eq(t => t.OperatorId, query.OperatorId));

        if (query.StartDate.HasValue)
            filters.Add(builder.Gte(t => t.CreatedAt, query.StartDate.Value));

        if (query.EndDate.HasValue)
            filters.Add(builder.Lte(t => t.CreatedAt, query.EndDate.Value));

        var filter = filters.Count > 0 ? builder.And(filters) : builder.Empty;

        return await _collection.Find(filter)
            .SortByDescending(t => t.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Limit(query.PageSize)
            .ToListAsync();
    }

    public async Task<TransactionListResponseDto> QueryWithCountAsync(TransactionQueryDto query)
    {
        var builder = Builders<StockTransaction>.Filter;
        var filters = new List<FilterDefinition<StockTransaction>>();

        if (!string.IsNullOrEmpty(query.PartId))
            filters.Add(builder.Eq(t => t.PartId, query.PartId));

        if (!string.IsNullOrEmpty(query.ProjectId))
            filters.Add(builder.Eq(t => t.ProjectId, query.ProjectId));

        if (!string.IsNullOrEmpty(query.Type))
            filters.Add(builder.Eq(t => t.Type, query.Type));

        if (query.SourceType.HasValue)
            filters.Add(builder.Eq(t => t.SourceType, query.SourceType.Value));

        if (!string.IsNullOrEmpty(query.OperatorId))
            filters.Add(builder.Eq(t => t.OperatorId, query.OperatorId));

        if (query.StartDate.HasValue)
            filters.Add(builder.Gte(t => t.CreatedAt, query.StartDate.Value));

        if (query.EndDate.HasValue)
            filters.Add(builder.Lte(t => t.CreatedAt, query.EndDate.Value));

        var filter = filters.Count > 0 ? builder.And(filters) : builder.Empty;

        var totalCount = await _collection.CountDocumentsAsync(filter);
        var items = await _collection.Find(filter)
            .SortByDescending(t => t.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Limit(query.PageSize)
            .ToListAsync();

        return new TransactionListResponseDto { Items = items, TotalCount = totalCount };
    }

    public async Task<List<StockTransaction>> GetLocksByPartIdAsync(string partId)
    {
        // LOCK and SelectionLock type transactions that haven't been unlocked
        return await _collection.Find(t =>
            t.PartId == partId &&
            (t.Type == "LOCK" || t.Type == "SelectionLock")
        ).SortByDescending(t => t.CreatedAt).ToListAsync();
    }

    public async Task<Dictionary<string, int>> GetLockSummaryByPartIdAsync(string partId)
    {
        var locks = await GetLocksByPartIdAsync(partId);
        var summary = new Dictionary<string, int>();

        foreach (var lockTx in locks)
        {
            var key = $"{lockTx.ProjectId}|{lockTx.SelectionPlanId}|{lockTx.SelectionItemId}";
            if (summary.ContainsKey(key))
                summary[key] += lockTx.Quantity;
            else
                summary[key] = lockTx.Quantity;
        }

        return summary;
    }
}
