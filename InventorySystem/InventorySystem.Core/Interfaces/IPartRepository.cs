using InventorySystem.Core.Models;

namespace InventorySystem.Core.Interfaces;

public interface IPartRepository : IRepository<Part>
{
    Task<List<Part>> FilterBySpecsAsync(string? category, List<(string key, string op, string value)> criteria);
    Task UpdateQuantitiesAsync(string id, int availableDelta, int lockedDelta, int totalDelta);
}
