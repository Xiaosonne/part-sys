using InventorySystem.Core.Interfaces;
using InventorySystem.Core.Models;
using MongoDB.Driver;

namespace InventorySystem.Infrastructure.Repositories;

public class FileMetadataRepository : MongoRepository<FileMetadata>, IFileMetadataRepository
{
    public FileMetadataRepository(IMongoDatabase database)
        : base(database, "FileMetadata")
    {
    }

    public async Task<List<FileMetadata>> GetByRelatedIdAsync(string relatedId) =>
        await FindAsync(f => f.RelatedId == relatedId && !f.IsDeleted);

    public async Task<List<FileMetadata>> GetByBucketAsync(string bucket) =>
        await FindAsync(f => f.Bucket == bucket && !f.IsDeleted);

    public async Task<List<FileMetadata>> GetByFileTypeAsync(FileType fileType) =>
        await FindAsync(f => f.FileType == fileType && !f.IsDeleted);
}
