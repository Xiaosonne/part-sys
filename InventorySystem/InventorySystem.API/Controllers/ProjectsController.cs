using InventorySystem.Core.Interfaces;
using InventorySystem.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventorySystem.API.Controllers;

[ApiController]
[Route("api/projects")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly IProjectNodeRepository _repo;
    private readonly IWorkspaceInitializer _workspaceInitializer;
    private readonly IFileMetadataRepository _fileRepo;
    private readonly IFileStorageService _storageService;
    private readonly IWorkspaceStructureRepository _workspaceStructureRepo;

    public ProjectsController(IProjectNodeRepository repo, IWorkspaceInitializer workspaceInitializer, IFileMetadataRepository fileRepo, IFileStorageService storageService, IWorkspaceStructureRepository workspaceStructureRepo)
    {
        _repo = repo;
        _workspaceInitializer = workspaceInitializer;
        _fileRepo = fileRepo;
        _storageService = storageService;
        _workspaceStructureRepo = workspaceStructureRepo;
    }

    [HttpGet]
    public async Task<IActionResult> GetTree([FromQuery] string? parentId)
    {
        var nodes = await _repo.GetChildrenAsync(parentId);
        return Ok(nodes);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var node = await _repo.GetByIdAsync(id);
        return node == null ? NotFound() : Ok(node);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProjectNode node)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        node.OwnerId = userId;
        node.CreatedAt = DateTime.UtcNow;
        node.UpdatedAt = DateTime.UtcNow;
        await _repo.CreateAsync(node);

        // Initialize workspace folders for new projects
        if (node.Type == "project")
        {
            _ = _workspaceInitializer.InitializeProjectWorkspaceAsync(node.Id);
        }

        return CreatedAtAction(nameof(GetById), new { id = node.Id }, node);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] ProjectNode node)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing == null) return NotFound();
        node.Id = id;
        node.OwnerId = existing.OwnerId;
        node.CreatedAt = existing.CreatedAt;
        node.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(id, node);
        return Ok(node);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing == null) return NotFound();
        await _repo.DeleteAsync(id);
        return NoContent();
    }

    [HttpPost("{id}/reinitialize-workspace")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> ReinitializeWorkspace(string id)
    {
        var project = await _repo.GetByIdAsync(id);
        if (project == null) return NotFound();
        if (project.Type != "project") return BadRequest("Only projects can have workspace reinitialized");

        try
        {
            // Delete existing files from storage and database
            var allFiles = await _fileRepo.GetByRelatedIdAsync(id);
            foreach (var file in allFiles)
            {
                await _storageService.DeleteFileAsync(file.Bucket, file.ObjectKey);
                await _fileRepo.DeleteAsync(file.Id);
            }

            // Delete entire project folder
            await _storageService.DeleteFolderAsync("projects", id);

            // Delete workspace structure metadata
            await _workspaceStructureRepo.DeleteByProjectIdAsync(id);

            // Reinitialize workspace
            await _workspaceInitializer.InitializeProjectWorkspaceAsync(id);
            return Ok(new { message = "Workspace reinitialized successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
