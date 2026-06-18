using InventorySystem.API.DTOs;
using InventorySystem.Core.Interfaces;
using InventorySystem.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InventorySystem.API.Controllers;

/// <summary>
/// 文件管理 — 上传、下载、目录浏览、软删除（与项目工作空间绑定）
/// </summary>
[ApiController]
[Route("api/files")]
[Authorize]
public class FilesController : ControllerBase
{
    private readonly IFileMetadataRepository _fileRepo;
    private readonly IFileStorageService _storageService;
    private readonly IWorkspaceStructureRepository _workspaceStructureRepo;
    private const long MaxFileSize = 100 * 1024 * 1024; // 100MB

    public FilesController(IFileMetadataRepository fileRepo, IFileStorageService storageService, IWorkspaceStructureRepository workspaceStructureRepo)
    {
        _fileRepo = fileRepo;
        _storageService = storageService;
        _workspaceStructureRepo = workspaceStructureRepo;
    }

    /// <summary>上传文件到指定存储桶的目录下</summary>
    [HttpPost("upload")]
    [Authorize(Roles = "admin,warehouse")]
    public async Task<IActionResult> Upload()
    {
        var form = await Request.ReadFormAsync();
        var file = form.Files["file"];
        var bucket = form["bucket"].ToString();
        var relatedId = form["relatedId"].ToString();
        var fileTypeStr = form["fileType"].ToString();
        var subPath = form["path"].ToString();

        if (file == null || file.Length == 0)
            return BadRequest("File is required");
        if (string.IsNullOrEmpty(bucket) || string.IsNullOrEmpty(relatedId))
            return BadRequest("Bucket and RelatedId are required");
        if (!Enum.TryParse<FileType>(fileTypeStr, out var fileType))
            return BadRequest("Invalid FileType");
        if (file.Length > MaxFileSize)
            return BadRequest($"File size exceeds {MaxFileSize / (1024 * 1024)}MB limit");

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
        var storagePath = string.IsNullOrEmpty(subPath) ? relatedId : $"{relatedId}/{subPath}";

        using var stream = file.OpenReadStream();
        var objectKey = await _storageService.UploadFileAsync(stream, file.FileName, bucket, storagePath);

        var metadata = new FileMetadata
        {
            FileName = file.FileName, FileSize = file.Length,
            MimeType = file.ContentType ?? "application/octet-stream",
            Bucket = bucket, ObjectKey = objectKey, FileType = fileType,
            RelatedId = relatedId, UploadedBy = userId,
            Description = form["description"].ToString() ?? string.Empty
        };
        await _fileRepo.CreateAsync(metadata);
        return Ok(metadata);
    }

    /// <summary>在存储桶中创建文件夹</summary>
    [HttpPost("folder")]
    [Authorize(Roles = "admin,warehouse")]
    public async Task<IActionResult> CreateFolder(string bucket, string relatedId, string folderPath)
    {
        if (string.IsNullOrEmpty(bucket) || string.IsNullOrEmpty(relatedId) || string.IsNullOrEmpty(folderPath))
            return BadRequest("Bucket, relatedId and folderPath are required");
        await _storageService.CreateFolderAsync(bucket, $"{relatedId}/{folderPath}");
        return Ok(new { message = "Folder created" });
    }

    /// <summary>列出指定目录下的文件和文件夹（含工作空间名称映射）</summary>
    [HttpGet("list")]
    public async Task<IActionResult> ListFiles(string bucket, string? path = null, string? relatedId = null)
    {
        if (string.IsNullOrEmpty(bucket)) return BadRequest("Bucket is required");
        if (string.IsNullOrEmpty(relatedId)) return BadRequest("RelatedId is required");

        var listPath = string.IsNullOrEmpty(path) ? relatedId : $"{relatedId}/{path}";
        var items = await _storageService.ListFilesAsync(bucket, listPath);
        var metadata = (await _fileRepo.GetByBucketAsync(bucket)).Where(m => m.RelatedId == relatedId).ToList();
        var workspaceStructure = await _workspaceStructureRepo.GetByProjectIdAsync(relatedId);
        var pathToNameMap = BuildPathToNameMap(workspaceStructure?.Structure ?? new());

        var filteredItems = new List<FileItem>();
        foreach (var item in items)
        {
            if (item.Path == relatedId && item.IsFolder) continue;
            item.Path = item.Path.StartsWith($"{relatedId}/") ? item.Path[(relatedId.Length + 1)..] : item.Path == relatedId ? "" : item.Path;
            if (!item.IsFolder)
            {
                var fileMetadata = metadata.FirstOrDefault(m => m.ObjectKey == item.Path || m.ObjectKey.EndsWith(item.Name));
                if (fileMetadata != null) { item.Id = fileMetadata.Id; item.OriginalName = fileMetadata.FileName; }
            }
            else if (pathToNameMap.ContainsKey(item.Path))
                item.DisplayName = pathToNameMap[item.Path];
            filteredItems.Add(item);
        }
        return Ok(filteredItems);
    }

    /// <summary>获取文件元数据</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetMetadata(string id)
    {
        var file = await _fileRepo.GetByIdAsync(id);
        return file == null || file.IsDeleted ? NotFound() : Ok(file);
    }

    /// <summary>下载文件</summary>
    [HttpGet("{id}/download")]
    public async Task<IActionResult> Download(string id)
    {
        var metadata = await _fileRepo.GetByIdAsync(id);
        if (metadata == null || metadata.IsDeleted) return NotFound();
        var stream = await _storageService.DownloadFileAsync(metadata.Bucket, metadata.ObjectKey);
        return File(stream, metadata.MimeType, metadata.FileName);
    }

    /// <summary>删除整个文件夹及其中的文件</summary>
    [HttpDelete("folder")]
    [Authorize(Roles = "admin,warehouse")]
    public async Task<IActionResult> DeleteFolder(string bucket, string relatedId, string folderPath)
    {
        if (string.IsNullOrEmpty(bucket) || string.IsNullOrEmpty(relatedId) || string.IsNullOrEmpty(folderPath))
            return BadRequest("Bucket, relatedId and folderPath are required");
        var storagePath = $"{relatedId}/{folderPath}";
        var allMetadata = await _fileRepo.GetByBucketAsync(bucket);
        foreach (var file in allMetadata.Where(m => m.RelatedId == relatedId && m.ObjectKey.StartsWith(storagePath + "/") && !m.IsDeleted))
        {
            file.IsDeleted = true; file.UpdatedAt = DateTime.UtcNow;
            await _fileRepo.UpdateAsync(file.Id, file);
            await _storageService.DeleteFileAsync(bucket, file.ObjectKey);
        }
        await _storageService.DeleteFolderAsync(bucket, storagePath);
        return Ok();
    }

    /// <summary>软删除单个文件</summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "admin,warehouse")]
    public async Task<IActionResult> Delete(string id)
    {
        var metadata = await _fileRepo.GetByIdAsync(id);
        if (metadata == null) return NotFound();
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        if (metadata.UploadedBy != userId && userRole != "admin") return Forbid();
        metadata.IsDeleted = true; metadata.UpdatedAt = DateTime.UtcNow;
        await _fileRepo.UpdateAsync(id, metadata);
        await _storageService.DeleteFileAsync(metadata.Bucket, metadata.ObjectKey);
        return Ok();
    }

    /// <summary>获取配件的所有关联文件</summary>
    [HttpGet("part/{partId}")]
    public async Task<IActionResult> GetPartFiles(string partId)
    {
        var files = await _fileRepo.GetByRelatedIdAsync(partId);
        return Ok(files.Where(f => !f.IsDeleted).ToList());
    }

    /// <summary>获取项目的所有关联文件</summary>
    [HttpGet("project/{projectId}")]
    public async Task<IActionResult> GetProjectFiles(string projectId)
    {
        var files = await _fileRepo.GetByRelatedIdAsync(projectId);
        return Ok(files.Where(f => !f.IsDeleted).ToList());
    }

    /// <summary>递归构建工作空间文件夹路径到显示名称的映射</summary>
    private Dictionary<string, string> BuildPathToNameMap(List<WorkspaceNode> nodes, string parentPath = "")
    {
        var map = new Dictionary<string, string>();
        foreach (var node in nodes)
        {
            var path = string.IsNullOrEmpty(parentPath) ? node.FolderId : $"{parentPath}/{node.FolderId}";
            map[path] = node.FolderName;
            if (node.Children?.Count > 0)
            {
                var childMap = BuildPathToNameMap(node.Children, path);
                foreach (var kvp in childMap) map[kvp.Key] = kvp.Value;
            }
        }
        return map;
    }
}
