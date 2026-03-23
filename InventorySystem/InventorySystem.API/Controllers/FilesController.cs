using InventorySystem.API.DTOs;
using InventorySystem.Core.Interfaces;
using InventorySystem.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InventorySystem.API.Controllers;

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

        // Always organize files under relatedId directory
        var storagePath = string.IsNullOrEmpty(subPath) ? relatedId : $"{relatedId}/{subPath}";

        using var stream = file.OpenReadStream();
        var objectKey = await _storageService.UploadFileAsync(stream, file.FileName, bucket, storagePath);

        var metadata = new FileMetadata
        {
            FileName = file.FileName,
            FileSize = file.Length,
            MimeType = file.ContentType ?? "application/octet-stream",
            Bucket = bucket,
            ObjectKey = objectKey,
            FileType = fileType,
            RelatedId = relatedId,
            UploadedBy = userId,
            Description = form["description"].ToString() ?? string.Empty
        };

        await _fileRepo.CreateAsync(metadata);
        return Ok(metadata);
    }

    [HttpPost("folder")]
    [Authorize(Roles = "admin,warehouse")]
    public async Task<IActionResult> CreateFolder(string bucket, string relatedId, string folderPath)
    {
        if (string.IsNullOrEmpty(bucket) || string.IsNullOrEmpty(relatedId) || string.IsNullOrEmpty(folderPath))
            return BadRequest("Bucket, relatedId and folderPath are required");

        // Always organize folders under relatedId directory
        var storagePath = $"{relatedId}/{folderPath}";
        await _storageService.CreateFolderAsync(bucket, storagePath);
        return Ok(new { message = "Folder created" });
    }

    [HttpGet("list")]
    public async Task<IActionResult> ListFiles(string bucket, string? path = null, string? relatedId = null)
    {
        if (string.IsNullOrEmpty(bucket))
            return BadRequest("Bucket is required");

        if (string.IsNullOrEmpty(relatedId))
            return BadRequest("RelatedId is required");

        // Always list files under relatedId directory
        var listPath = string.IsNullOrEmpty(path) ? relatedId : $"{relatedId}/{path}";
        var items = await _storageService.ListFilesAsync(bucket, listPath);
        var metadata = await _fileRepo.GetByBucketAsync(bucket);

        // Filter by relatedId
        metadata = metadata.Where(m => m.RelatedId == relatedId).ToList();

        // Get workspace structure metadata for display names
        var workspaceStructure = await _workspaceStructureRepo.GetByProjectIdAsync(relatedId);
        var pathToNameMap = BuildPathToNameMap(workspaceStructure?.Structure ?? new());

        // Remove relatedId prefix from paths and filter out the relatedId folder itself
        var filteredItems = new List<FileItem>();
        foreach (var item in items)
        {
            // Skip the relatedId folder itself when listing root
            if (item.Path == relatedId && item.IsFolder)
            {
                continue;
            }

            if (item.Path.StartsWith($"{relatedId}/"))
            {
                item.Path = item.Path.Substring(relatedId.Length + 1);
            }
            else if (item.Path == relatedId)
            {
                item.Path = "";
            }

            if (!item.IsFolder)
            {
                var fileMetadata = metadata.FirstOrDefault(m => m.ObjectKey == item.Path || m.ObjectKey.EndsWith(item.Name));
                if (fileMetadata != null)
                {
                    item.Id = fileMetadata.Id;
                    item.OriginalName = fileMetadata.FileName;
                }
            }
            else if (pathToNameMap.ContainsKey(item.Path))
            {
                item.DisplayName = pathToNameMap[item.Path];
            }

            filteredItems.Add(item);
        }

        return Ok(filteredItems);
    }

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
                foreach (var kvp in childMap)
                {
                    map[kvp.Key] = kvp.Value;
                }
            }
        }
        return map;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetMetadata(string id)
    {
        var file = await _fileRepo.GetByIdAsync(id);
        return file == null || file.IsDeleted ? NotFound() : Ok(file);
    }

    [HttpGet("{id}/download")]
    public async Task<IActionResult> Download(string id)
    {
        var metadata = await _fileRepo.GetByIdAsync(id);
        if (metadata == null || metadata.IsDeleted)
            return NotFound();

        var stream = await _storageService.DownloadFileAsync(metadata.Bucket, metadata.ObjectKey);
        return File(stream, metadata.MimeType, metadata.FileName);
    }

    [HttpDelete("folder")]
    [Authorize(Roles = "admin,warehouse")]
    public async Task<IActionResult> DeleteFolder(string bucket, string relatedId, string folderPath)
    {
        if (string.IsNullOrEmpty(bucket) || string.IsNullOrEmpty(relatedId) || string.IsNullOrEmpty(folderPath))
            return BadRequest("Bucket, relatedId and folderPath are required");

        var storagePath = $"{relatedId}/{folderPath}";

        // Get all files in this folder
        var allMetadata = await _fileRepo.GetByBucketAsync(bucket);
        var folderFiles = allMetadata.Where(m =>
            m.RelatedId == relatedId &&
            m.ObjectKey.StartsWith(storagePath + "/") &&
            !m.IsDeleted
        ).ToList();

        // Soft delete all files in folder
        foreach (var file in folderFiles)
        {
            file.IsDeleted = true;
            file.UpdatedAt = DateTime.UtcNow;
            await _fileRepo.UpdateAsync(file.Id, file);
            await _storageService.DeleteFileAsync(bucket, file.ObjectKey);
        }

        // Delete folder
        await _storageService.DeleteFolderAsync(bucket, storagePath);
        return Ok();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "admin,warehouse")]
    public async Task<IActionResult> Delete(string id)
    {
        var metadata = await _fileRepo.GetByIdAsync(id);
        if (metadata == null)
            return NotFound();

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        if (metadata.UploadedBy != userId && userRole != "admin")
            return Forbid();

        metadata.IsDeleted = true;
        metadata.UpdatedAt = DateTime.UtcNow;
        await _fileRepo.UpdateAsync(id, metadata);

        await _storageService.DeleteFileAsync(metadata.Bucket, metadata.ObjectKey);
        return Ok();
    }

    [HttpGet("part/{partId}")]
    public async Task<IActionResult> GetPartFiles(string partId)
    {
        var files = await _fileRepo.GetByRelatedIdAsync(partId);
        return Ok(files.Where(f => !f.IsDeleted).ToList());
    }

    [HttpGet("project/{projectId}")]
    public async Task<IActionResult> GetProjectFiles(string projectId)
    {
        var files = await _fileRepo.GetByRelatedIdAsync(projectId);
        return Ok(files.Where(f => !f.IsDeleted).ToList());
    }
}
