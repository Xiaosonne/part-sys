using InventorySystem.Core.Models;

namespace InventorySystem.Core.Interfaces;

public interface IProjectNodeRepository : IRepository<ProjectNode>
{
    Task<List<ProjectNode>> GetChildrenAsync(string? parentId);
}
