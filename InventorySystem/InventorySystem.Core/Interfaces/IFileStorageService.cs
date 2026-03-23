namespace InventorySystem.Core.Interfaces;

public interface IFileStorageService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string bucket, string? path = null);
    Task<Stream> DownloadFileAsync(string bucket, string objectKey);
    Task DeleteFileAsync(string bucket, string objectKey);
    Task DeleteFolderAsync(string bucket, string folderPath);
    Task CreateFolderAsync(string bucket, string folderPath);
    Task<List<FileItem>> ListFilesAsync(string bucket, string? path = null);
    bool FileExists(string bucket, string objectKey);
}

public class FileItem
{
    public string? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? OriginalName { get; set; }
    public string? DisplayName { get; set; }
    public string Path { get; set; } = string.Empty;
    public bool IsFolder { get; set; }
    public long Size { get; set; }
    public DateTime Modified { get; set; }
}

