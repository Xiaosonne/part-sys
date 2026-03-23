using InventorySystem.Core.Models;

namespace InventorySystem.Core.Interfaces;

public interface IWorkspaceStructureRepository
{
    Task<WorkspaceStructure> GetByProjectIdAsync(string projectId);
    Task CreateAsync(WorkspaceStructure structure);
    Task UpdateAsync(string projectId, WorkspaceStructure structure);
    Task DeleteByProjectIdAsync(string projectId);
}
