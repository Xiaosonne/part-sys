using InventorySystem.Core.Models;

namespace InventorySystem.API.DTOs;

public class FileUploadRequest
{
    public IFormFile? File { get; set; }
    public string Bucket { get; set; } = string.Empty;
    public string RelatedId { get; set; } = string.Empty;
    public FileType FileType { get; set; }
    public string? Description { get; set; }
}
