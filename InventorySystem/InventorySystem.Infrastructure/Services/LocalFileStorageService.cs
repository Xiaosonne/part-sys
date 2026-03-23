using InventorySystem.Core.Interfaces;

namespace InventorySystem.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath;

    public LocalFileStorageService(string basePath = "wwwroot/files")
    {
        _basePath = basePath;
        Directory.CreateDirectory(_basePath);
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string bucket, string? path = null)
    {
        var bucketPath = Path.Combine(_basePath, bucket, path ?? "");
        Directory.CreateDirectory(bucketPath);

        var objectKey = $"{Guid.NewGuid()}_{fileName}";
        var filePath = Path.Combine(bucketPath, objectKey);

        using (var fileWriter = new FileStream(filePath, FileMode.Create, FileAccess.Write))
        {
            await fileStream.CopyToAsync(fileWriter);
        }

        return path != null ? $"{path}/{objectKey}" : objectKey;
    }

    public Task<Stream> DownloadFileAsync(string bucket, string objectKey)
    {
        var filePath = Path.Combine(_basePath, bucket, objectKey);
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {objectKey}");

        return Task.FromResult<Stream>(new FileStream(filePath, FileMode.Open, FileAccess.Read));
    }

    public Task DeleteFileAsync(string bucket, string objectKey)
    {
        var filePath = Path.Combine(_basePath, bucket, objectKey);
        if (File.Exists(filePath))
            File.Delete(filePath);
        return Task.CompletedTask;
    }

    public Task DeleteFolderAsync(string bucket, string folderPath)
    {
        var fullPath = Path.Combine(_basePath, bucket, folderPath);
        if (Directory.Exists(fullPath))
            Directory.Delete(fullPath, true);
        return Task.CompletedTask;
    }

    public Task CreateFolderAsync(string bucket, string folderPath)
    {
        var fullPath = Path.Combine(_basePath, bucket, folderPath);
        Directory.CreateDirectory(fullPath);
        return Task.CompletedTask;
    }

    public Task<List<FileItem>> ListFilesAsync(string bucket, string? path = null)
    {
        var fullPath = Path.Combine(_basePath, bucket, path ?? "");
        var items = new List<FileItem>();

        if (!Directory.Exists(fullPath))
            return Task.FromResult(items);

        var dirInfo = new DirectoryInfo(fullPath);

        foreach (var dir in dirInfo.GetDirectories())
        {
            items.Add(new FileItem
            {
                Name = dir.Name,
                Path = path != null ? $"{path}/{dir.Name}" : dir.Name,
                IsFolder = true,
                Size = 0,
                Modified = dir.LastWriteTime
            });
        }

        foreach (var file in dirInfo.GetFiles())
        {
            items.Add(new FileItem
            {
                Name = file.Name,
                Path = path != null ? $"{path}/{file.Name}" : file.Name,
                IsFolder = false,
                Size = file.Length,
                Modified = file.LastWriteTime
            });
        }

        return Task.FromResult(items);
    }

    public bool FileExists(string bucket, string objectKey)
    {
        var filePath = Path.Combine(_basePath, bucket, objectKey);
        return File.Exists(filePath);
    }
}
