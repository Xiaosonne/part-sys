using InventorySystem.Core.Models;
using InventorySystem.Core.DTOs;

namespace InventorySystem.Core.Interfaces;

public interface IStockTransactionRepository : IRepository<StockTransaction>
{
    Task<List<StockTransaction>> GetByPartIdAsync(string partId);
    Task<List<StockTransaction>> GetByProjectIdAsync(string projectId);
    Task<List<StockTransaction>> QueryAsync(TransactionQueryDto query);
    Task<TransactionListResponseDto> QueryWithCountAsync(TransactionQueryDto query);
    Task<List<StockTransaction>> GetLocksByPartIdAsync(string partId);
    Task<Dictionary<string, int>> GetLockSummaryByPartIdAsync(string partId);
}
