using InventorySystem.Core.Models;

namespace InventorySystem.Core.Interfaces;

public interface IPartRepository : IRepository<Part>
{
    Task<List<Part>> FilterBySpecsAsync(string? category, List<(string key, string op, string value)> criteria);
    /// <summary>原子更新库存数量，带可选的条件检查（乐观锁）</summary>
    /// <param name="minAvailableQty">更新需要满足的 AvailableQty >= 此值（AvailableQty = TotalQty - LockedQty）</param>
    /// <param name="minLockedQty">更新需要满足的 LockedQty >= 此值</param>
    /// <returns>是否成功更新（false = 条件不满足）</returns>
    Task<bool> UpdateQuantitiesAsync(string id, int lockedDelta, int totalDelta, int? minAvailableQty = null, int? minLockedQty = null);
}
