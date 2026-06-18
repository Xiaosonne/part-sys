using InventorySystem.Core.Interfaces;
using InventorySystem.Core.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace InventorySystem.Infrastructure.Services;

public class WorkspaceInitializer : IWorkspaceInitializer
{
    private readonly IFileStorageService _storageService;
    private readonly IWorkspaceStructureRepository _workspaceStructureRepo;
    private readonly string _configPath;
    private readonly ILogger<WorkspaceInitializer> _logger;

    public WorkspaceInitializer(
        IFileStorageService storageService,
        IWorkspaceStructureRepository workspaceStructureRepo,
        string configPath,
        ILogger<WorkspaceInitializer> logger)
    {
        _storageService = storageService;
        _workspaceStructureRepo = workspaceStructureRepo;
        _configPath = configPath;
        _logger = logger;
    }

    public async Task InitializeProjectWorkspaceAsync(string projectId)
    {
        _logger.LogInformation("Starting workspace initialization for project: {ProjectId}", projectId);
        _logger.LogDebug("Config path: {ConfigPath}, exists: {Exists}", _configPath, File.Exists(_configPath));

        if (!File.Exists(_configPath))
        {
            _logger.LogWarning("Workspace config file not found: {ConfigPath}", _configPath);
            return;
        }

        var json = await File.ReadAllTextAsync(_configPath);
        _logger.LogDebug("Config content length: {Length}", json.Length);

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var config = JsonSerializer.Deserialize<WorkspaceConfig>(json, options);

        if (config?.WorkspaceAreas == null)
        {
            _logger.LogWarning("Config or WorkspaceAreas is null");
            return;
        }

        _logger.LogInformation("Found {Count} workspace areas", config.WorkspaceAreas.Count);

        // Create physical folders
        foreach (var area in config.WorkspaceAreas)
        {
            _logger.LogDebug("Creating area: {AreaId}", area.Id);
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
        _logger.LogInformation("Workspace initialization completed for project: {ProjectId}", projectId);
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
        _logger.LogDebug("Creating folder: {FullPath}", fullPath);

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
