using InventorySystem.Core.Models;

namespace InventorySystem.Core.Interfaces;

public interface IPartCategoryRepository : IRepository<PartCategory>
{
    Task<List<PartCategory>> GetByParentIdAsync(string? parentId);
    Task<List<PartCategory>> GetTreeAsync();
    Task<PartCategory?> GetByPathAsync(string path);
    Task<List<PartCategory>> GetBySpecTemplateIdAsync(string specTemplateId);
}
