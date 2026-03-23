using InventorySystem.Core.Models;

namespace InventorySystem.Core.Interfaces;

public interface ISpecTemplateRepository : IRepository<SpecTemplate>
{
    Task<SpecTemplate?> GetByCategoryAsync(string category);
}
