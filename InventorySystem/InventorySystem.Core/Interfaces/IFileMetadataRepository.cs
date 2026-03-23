namespace InventorySystem.Core.Interfaces;

using InventorySystem.Core.Models;

public interface IFileMetadataRepository : IRepository<FileMetadata>
{
    Task<List<FileMetadata>> GetByRelatedIdAsync(string relatedId);
    Task<List<FileMetadata>> GetByBucketAsync(string bucket);
    Task<List<FileMetadata>> GetByFileTypeAsync(FileType fileType);
}
