namespace InventorySystem.Core.Interfaces;

public interface IWorkspaceInitializer
{
    /// <summary>
    /// Initialize project workspace folders based on configuration
    /// </summary>
    Task InitializeProjectWorkspaceAsync(string projectId);
}
