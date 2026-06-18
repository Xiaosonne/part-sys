using InventorySystem.Core.Interfaces;
using InventorySystem.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventorySystem.API.Controllers;

/// <summary>
/// 项目管理 — 项目/文件夹树结构维护，工作空间初始化与重建
/// </summary>
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

    public ProjectsController(
        IProjectNodeRepository repo, IWorkspaceInitializer workspaceInitializer,
        IFileMetadataRepository fileRepo, IFileStorageService storageService,
        IWorkspaceStructureRepository workspaceStructureRepo)
    {
        _repo = repo; _workspaceInitializer = workspaceInitializer;
        _fileRepo = fileRepo; _storageService = storageService;
        _workspaceStructureRepo = workspaceStructureRepo;
    }

    /// <summary>获取项目/文件夹树（可按父节点过滤）</summary>
    [HttpGet]
    public async Task<IActionResult> GetTree([FromQuery] string? parentId)
    {
        var nodes = await _repo.GetChildrenAsync(parentId);
        return Ok(nodes);
    }

    /// <summary>根据 ID 获取项目或文件夹</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var node = await _repo.GetByIdAsync(id);
        return node == null ? NotFound() : Ok(node);
    }

    /// <summary>创建项目或文件夹（项目自动初始化工作空间目录结构）</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProjectNode node)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        node.OwnerId = userId; node.CreatedAt = DateTime.UtcNow; node.UpdatedAt = DateTime.UtcNow;
        await _repo.CreateAsync(node);

        if (node.Type == "project")
        {
            try { await _workspaceInitializer.InitializeProjectWorkspaceAsync(node.Id); }
            catch (Exception ex) { Console.WriteLine($"[ProjectsController] Workspace init failed: {ex.Message}"); }
        }
        return CreatedAtAction(nameof(GetById), new { id = node.Id }, node);
    }

    /// <summary>更新项目或文件夹信息</summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] ProjectNode node)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing == null) return NotFound();
        node.Id = id; node.OwnerId = existing.OwnerId;
        node.CreatedAt = existing.CreatedAt; node.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(id, node);
        return Ok(node);
    }

    /// <summary>删除项目或文件夹</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing == null) return NotFound();
        await _repo.DeleteAsync(id);
        return NoContent();
    }

    /// <summary>
    /// 重建项目工作空间 — 清空并重新初始化目录结构（仅 admin）
    /// </summary>
    [HttpPost("{id}/reinitialize-workspace")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> ReinitializeWorkspace(string id)
    {
        var project = await _repo.GetByIdAsync(id);
        if (project == null) return NotFound();
        if (project.Type != "project") return BadRequest("Only projects can have workspace reinitialized");

        try
        {
            var allFiles = await _fileRepo.GetByRelatedIdAsync(id);
            foreach (var file in allFiles)
            {
                await _storageService.DeleteFileAsync(file.Bucket, file.ObjectKey);
                await _fileRepo.DeleteAsync(file.Id);
            }
            await _storageService.DeleteFolderAsync("projects", id);
            await _workspaceStructureRepo.DeleteByProjectIdAsync(id);
            await _workspaceInitializer.InitializeProjectWorkspaceAsync(id);
            return Ok(new { message = "Workspace reinitialized successfully" });
        }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }
}
