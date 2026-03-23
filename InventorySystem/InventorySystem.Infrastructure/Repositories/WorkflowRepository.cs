using InventorySystem.Core.Interfaces;
using InventorySystem.Core.Models;
using MongoDB.Driver;

namespace InventorySystem.Infrastructure.Repositories;

public class WorkflowDefinitionRepository : IWorkflowDefinitionRepository
{
    private readonly IMongoCollection<WorkflowDefinition> _collection;

    public WorkflowDefinitionRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<WorkflowDefinition>("WorkflowDefinitions");
    }

    public async Task<WorkflowDefinition?> GetByIdAsync(string id) =>
        await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();

    public async Task<List<WorkflowDefinition>> GetAllAsync() =>
        await _collection.Find(_ => true).ToListAsync();

    public async Task<List<WorkflowDefinition>> GetByCategoryAsync(string category) =>
        await _collection.Find(x => x.Category == category).ToListAsync();

    public async Task CreateAsync(WorkflowDefinition definition) =>
        await _collection.InsertOneAsync(definition);

    public async Task UpdateAsync(string id, WorkflowDefinition definition) =>
        await _collection.ReplaceOneAsync(x => x.Id == id, definition);

    public async Task DeleteAsync(string id) =>
        await _collection.DeleteOneAsync(x => x.Id == id);
}

public class WorkflowInstanceRepository : IWorkflowInstanceRepository
{
    private readonly IMongoCollection<WorkflowInstance> _collection;

    public WorkflowInstanceRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<WorkflowInstance>("WorkflowInstances");
    }

    public async Task<WorkflowInstance?> GetByIdAsync(string id) =>
        await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();

    public async Task<List<WorkflowInstance>> GetByStartedByAsync(string userId) =>
        await _collection.Find(x => x.StartedBy == userId).ToListAsync();

    public async Task<List<WorkflowInstance>> GetByEntityAsync(string entityType, string entityId) =>
        await _collection.Find(x => x.EntityType == entityType && x.EntityId == entityId).ToListAsync();

    public async Task CreateAsync(WorkflowInstance instance) =>
        await _collection.InsertOneAsync(instance);

    public async Task UpdateAsync(string id, WorkflowInstance instance) =>
        await _collection.ReplaceOneAsync(x => x.Id == id, instance);
}

public class WorkflowTaskRepository : IWorkflowTaskRepository
{
    private readonly IMongoCollection<WorkflowTask> _collection;

    public WorkflowTaskRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<WorkflowTask>("WorkflowTasks");
    }

    public async Task<WorkflowTask?> GetByIdAsync(string id) =>
        await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();

    public async Task<List<WorkflowTask>> GetPendingByAssigneeAsync(string assigneeId) =>
        await _collection.Find(x => x.AssigneeId == assigneeId && x.Status == "Pending").ToListAsync();

    public async Task<List<WorkflowTask>> GetAllPendingAsync() =>
        await _collection.Find(x => x.Status == "Pending").ToListAsync();

    public async Task<List<WorkflowTask>> GetCompletedByAssigneeAsync(string assigneeId) =>
        await _collection.Find(x => x.AssigneeId == assigneeId && (x.Status == "Approved" || x.Status == "Rejected")).ToListAsync();

    public async Task<List<WorkflowTask>> GetAllCompletedAsync() =>
        await _collection.Find(x => x.Status == "Approved" || x.Status == "Rejected").ToListAsync();

    public async Task<List<WorkflowTask>> GetByInstanceAsync(string instanceId) =>
        await _collection.Find(x => x.InstanceId == instanceId).ToListAsync();

    public async Task CreateAsync(WorkflowTask task) =>
        await _collection.InsertOneAsync(task);

    public async Task UpdateAsync(string id, WorkflowTask task) =>
        await _collection.ReplaceOneAsync(x => x.Id == id, task);
}

public class WorkflowHistoryRepository : IWorkflowHistoryRepository
{
    private readonly IMongoCollection<WorkflowHistory> _collection;

    public WorkflowHistoryRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<WorkflowHistory>("WorkflowHistories");
    }

    public async Task<List<WorkflowHistory>> GetByInstanceAsync(string instanceId) =>
        await _collection.Find(x => x.InstanceId == instanceId).ToListAsync();

    public async Task CreateAsync(WorkflowHistory history) =>
        await _collection.InsertOneAsync(history);
}
