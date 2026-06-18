using InventorySystem.Core.Interfaces;
using InventorySystem.Core.Models;
using MongoDB.Driver;

namespace InventorySystem.Infrastructure.Repositories;

public class WorkflowDefinitionRepository : MongoRepository<WorkflowDefinition>, IWorkflowDefinitionRepository
{
    public WorkflowDefinitionRepository(IMongoDatabase database)
        : base(database, "WorkflowDefinitions") { }

    public async Task<List<WorkflowDefinition>> GetByCategoryAsync(string category) =>
        await _collection.Find(x => x.Category == category).ToListAsync();
}

public class WorkflowInstanceRepository : MongoRepository<WorkflowInstance>, IWorkflowInstanceRepository
{
    public WorkflowInstanceRepository(IMongoDatabase database)
        : base(database, "WorkflowInstances") { }

    public async Task<List<WorkflowInstance>> GetByStartedByAsync(string userId) =>
        await _collection.Find(x => x.StartedBy == userId).ToListAsync();

    public async Task<List<WorkflowInstance>> GetByEntityAsync(string entityType, string entityId) =>
        await _collection.Find(x => x.EntityType == entityType && x.EntityId == entityId).ToListAsync();
}

public class WorkflowTaskRepository : MongoRepository<WorkflowTask>, IWorkflowTaskRepository
{
    public WorkflowTaskRepository(IMongoDatabase database)
        : base(database, "WorkflowTasks") { }

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
}

public class WorkflowHistoryRepository : MongoRepository<WorkflowHistory>, IWorkflowHistoryRepository
{
    public WorkflowHistoryRepository(IMongoDatabase database)
        : base(database, "WorkflowHistories") { }

    public async Task<List<WorkflowHistory>> GetByInstanceAsync(string instanceId) =>
        await _collection.Find(x => x.InstanceId == instanceId).ToListAsync();
}
