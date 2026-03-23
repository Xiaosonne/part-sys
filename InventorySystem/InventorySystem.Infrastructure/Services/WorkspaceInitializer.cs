using InventorySystem.Core.Interfaces;
using InventorySystem.Core.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace InventorySystem.Infrastructure.Services;

public class WorkspaceInitializer : IWorkspaceInitializer
{
    private readonly IFileStorageService _storageService;
    private readonly IWorkspaceStructureRepository _workspaceStructureRepo;
    private readonly string _configPath;

    public WorkspaceInitializer(IFileStorageService storageService, IWorkspaceStructureRepository workspaceStructureRepo, string configPath)
    {
        _storageService = storageService;
        _workspaceStructureRepo = workspaceStructureRepo;
        _configPath = configPath;
    }

    public async Task InitializeProjectWorkspaceAsync(string projectId)
    {
        try
        {
            Console.WriteLine($"[WorkspaceInitializer] Starting initialization for project: {projectId}");
            Console.WriteLine($"[WorkspaceInitializer] Config path: {_configPath}");
            Console.WriteLine($"[WorkspaceInitializer] Config exists: {File.Exists(_configPath)}");

            if (!File.Exists(_configPath))
            {
                Console.WriteLine($"[WorkspaceInitializer] Config file not found at: {_configPath}");
                return;
            }

            var json = await File.ReadAllTextAsync(_configPath);
            Console.WriteLine($"[WorkspaceInitializer] Config content length: {json.Length}");

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var config = JsonSerializer.Deserialize<WorkspaceConfig>(json, options);

            if (config?.WorkspaceAreas == null)
            {
                Console.WriteLine($"[WorkspaceInitializer] Config or WorkspaceAreas is null");
                return;
            }

            Console.WriteLine($"[WorkspaceInitializer] Found {config.WorkspaceAreas.Count} workspace areas");

            // Create physical folders
            foreach (var area in config.WorkspaceAreas)
            {
                Console.WriteLine($"[WorkspaceInitializer] Creating area: {area.Id}");
                await CreateFolderRecursiveAsync(projectId, area, "");
            }

            // Build nested structure for database
            var structure = new WorkspaceStructure
            {
                ProjectId = projectId,
                Structure = config.WorkspaceAreas.Select(a => BuildWorkspaceNode(a)).ToList(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _workspaceStructureRepo.UpdateAsync(projectId, structure);
            Console.WriteLine($"[WorkspaceInitializer] Initialization completed for project: {projectId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WorkspaceInitializer] ERROR: {ex.Message}");
            Console.WriteLine($"[WorkspaceInitializer] Stack trace: {ex.StackTrace}");
        }
    }

    private WorkspaceNode BuildWorkspaceNode(WorkspaceArea area)
    {
        return new WorkspaceNode
        {
            FolderId = area.Id,
            FolderName = area.Name,
            Children = area.Children?.Select(c => BuildWorkspaceNode(c)).ToList() ?? new()
        };
    }

    private async Task CreateFolderRecursiveAsync(string projectId, WorkspaceArea area, string parentPath)
    {
        var folderPath = string.IsNullOrEmpty(parentPath) ? area.Id : $"{parentPath}/{area.Id}";
        var fullPath = $"{projectId}/{folderPath}";
        Console.WriteLine($"[WorkspaceInitializer] Creating folder: {fullPath}");

        await _storageService.CreateFolderAsync("projects", fullPath);

        if (area.Children != null)
        {
            foreach (var child in area.Children)
            {
                await CreateFolderRecursiveAsync(projectId, child, folderPath);
            }
        }
    }
}

public class WorkspaceConfig
{
    [JsonPropertyName("workspaceAreas")]
    public List<WorkspaceArea>? WorkspaceAreas { get; set; }
}

public class WorkspaceArea
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("children")]
    public List<WorkspaceArea>? Children { get; set; }
}
