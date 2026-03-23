using InventorySystem.Core.Models;

namespace InventorySystem.Core.Interfaces;

public interface IStockTransactionRepository : IRepository<StockTransaction>
{
    Task<List<StockTransaction>> GetByPartIdAsync(string partId);
    Task<List<StockTransaction>> GetByProjectIdAsync(string projectId);
}
