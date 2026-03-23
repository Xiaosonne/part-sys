# 工作流审批系统技术文档

## 1. 架构概述

```
┌─────────────────────────────────────────────────────────────────┐
│                         前端 Vue 3                               │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐             │
│  │ PendingTasks │  │ MyWorkflows │  │WorkflowDetail│            │
│  │   .vue      │  │   .vue      │  │   .vue       │             │
│  └─────────────┘  └─────────────┘  └─────────────┘             │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                      API Controller                              │
│              WorkflowsController.cs                              │
│  POST /tasks/{id}/approve  ──► ApproveTask()                    │
│  POST /tasks/{id}/reject   ──► RejectTask()                     │
│  GET  /tasks/pending       ──► GetPendingTasks()                │
│  GET  /tasks/history       ──► GetHistoryTasks()                │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                      Service Layer                               │
│  ┌───────────────────┐    ┌───────────────────┐                 │
│  │  WorkflowService  │    │ WorkflowTaskService│                │
│  │  - StartInstance  │    │ - ApproveTask     │                │
│  │  - CancelInstance │    │ - RejectTask      │                │
│  └───────────────────┘    └───────────────────┘                 │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Repository Layer                               │
│  ┌─────────────────┐  ┌─────────────────┐  ┌───────────────┐   │
│  │WorkflowInstance │  │  WorkflowTask   │  │WorkflowHistory│   │
│  │  Repository     │  │  Repository     │  │  Repository   │   │
│  └─────────────────┘  └─────────────────┘  └───────────────┘   │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                      MongoDB                                     │
│  WorkflowDefinitions / WorkflowInstances / WorkflowTasks /      │
│  WorkflowHistories                                               │
└─────────────────────────────────────────────────────────────────┘
```

## 2. 数据模型

### 2.1 核心实体

```csharp
// 流程定义 - 定义审批流程模板
public class WorkflowDefinition
{
    public string Id { get; set; }              // MongoDB ObjectId
    public string Name { get; set; }            // 流程名称
    public List<WorkflowNode> Nodes { get; set; } // 节点列表
    public StartConfig StartConfig { get; set; } // 启动配置
}

// 流程节点
public class WorkflowNode
{
    public string Id { get; set; }                    // 节点唯一标识
    public string NodeType { get; set; }              // 节点类型: Start, SingleApproval, End
    public string Name { get; set; }                  // 节点名称: "第一级审批", "第二级审批"
    public List<string> Approvers { get; set; }       // 审批人ID列表
    public string ApproverSource { get; set; }        // 审批人来源: "Static"
    public List<FormField> FormFields { get; set; }   // 表单字段
    public List<string> NextNodes { get; set; }       // 下一节点ID列表
}

// 流程实例 - 运行时实例
public class WorkflowInstance
{
    public string Id { get; set; }
    public string DefinitionId { get; set; }          // 关联的流程定义ID
    public string Name { get; set; }                 // 实例名称（用户自定义）
    public string Status { get; set; }               // Running/Completed/Rejected/Cancelled
    public string CurrentNodeId { get; set; }        // 当前节点ID
    public Dictionary<string, object> FormData { get; set; } // 启动时填写的表单数据
    public string StartedBy { get; set; }            // 发起人ID
    public DateTime StartedAt { get; set; }
}

// 审批任务 - 待审批的任务项
public class WorkflowTask
{
    public string Id { get; set; }
    public string InstanceId { get; set; }            // 关联的流程实例ID
    public string NodeId { get; set; }               // 对应的节点ID
    public string NodeName { get; set; }              // 节点名称
    public string AssigneeId { get; set; }            // 审批人ID
    public string Status { get; set; }               // Pending/Approved/Rejected
    public Dictionary<string, object> FormData { get; set; } // 审批时填写的表单数据
    public string Comment { get; set; }               // 审批意见
    public DateTime CreatedAt { get; set; }
    public DateTime? DueDate { get; set; }            // 截止时间
    public DateTime? CompletedAt { get; set; }
}

// 审批历史
public class WorkflowHistory
{
    public string Id { get; set; }
    public string InstanceId { get; set; }
    public string NodeId { get; set; }
    public string NodeName { get; set; }
    public string Action { get; set; }                // Start/Approve/Reject
    public string OperatorId { get; set; }            // 操作人ID
    public string OperatorName { get; set; }          // 操作人姓名
    public string Comment { get; set; }               // 审批意见
    public Dictionary<string, object> FormData { get; set; } // 操作时的表单数据
    public DateTime CreatedAt { get; set; }
}
```

### 2.2 表单字段

```csharp
public class FormField
{
    public string Id { get; set; }
    public string FieldType { get; set; }    // text/textarea/select/number/checkbox
    public string Label { get; set; }        // 显示标签
    public string Key { get; set; }          // 数据键名
    public bool Required { get; set; }       // 是否必填
    public string Placeholder { get; set; } // 占位文本
    public List<string> Options { get; set; } // 下拉选项
}
```

## 3. API 接口

### 3.1 审批相关

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/api/workflows/tasks/pending` | 获取待审批任务 |
| GET | `/api/workflows/tasks/history` | 获取历史审批记录 |
| POST | `/api/workflows/tasks/{id}/approve` | 审批通过 |
| POST | `/api/workflows/tasks/{id}/reject` | 审批拒绝 |

### 3.2 请求/响应格式

**ApproveTaskRequest:**
```json
{
    "comment": "同意该项目",
    "formData": {
        "审批意见": "同意",
        "优先级": "高"
    }
}
```

**RejectTaskRequest:**
```json
{
    "comment": "材料不全，需要补充"
}
```

**WorkflowTaskDto (响应):**
```json
{
    "id": "65f1a2b3c4d5e6f7a8b9c0d1",
    "instanceId": "65f1a2b3c4d5e6f7a8b9c0d2",
    "nodeId": "node_1",
    "nodeName": "第一级审批",
    "assigneeId": "65f1a2b3c4d5e6f7a8b9c0d3",
    "assigneeName": "admin",
    "status": "Pending",
    "createdAt": "2026-03-21T10:00:00Z",
    "dueDate": "2026-03-23T10:00:00Z",
    "instanceName": "项目A选型审批",
    "startedBy": "65f1a2b3c4d5e6f7a8b9c0d4",
    "startedByName": "张三",
    "instance": {
        "id": "...",
        "name": "项目A选型审批",
        "status": "Running",
        "definition": { ... }
    }
}
```

## 4. 审批流程

### 4.1 审批通过 (ApproveTask)

```
用户点击"通过"
       │
       ▼
API: POST /tasks/{id}/approve
       │
       ▼
WorkflowTaskService.ApproveTaskAsync()
       │
       ├──► 1. 更新 WorkflowTask 状态为 "Approved"
       │         - 保存 comment 到 Comment 字段
       │         - 保存 formData 到 FormData 字段
       │         - 设置 CompletedAt = UTC Now
       │
       ├──► 2. 创建 WorkflowHistory 记录
       │         - Action = "Approve"
       │         - 保存操作人和表单数据
       │
       ├──► 3. 获取 WorkflowInstance
       │
       ├──► 4. 查找当前节点的下一节点
       │
       ├──► 5. 如果是 End 节点：
       │         - 更新 Instance 状态为 "Completed"
       │         - 设置 CompletedAt
       │
       └──► 6. 如果是审批节点：
                 - 创建新的 WorkflowTask
                 - 指定下一节点审批人为 Assignee
```

### 4.2 审批拒绝 (RejectTask)

```
用户点击"拒绝"
       │
       ▼
API: POST /tasks/{id}/reject
       │
       ▼
WorkflowTaskService.RejectTaskAsync()
       │
       ├──► 1. 更新 WorkflowTask 状态为 "Rejected"
       │
       ├──► 2. 创建 WorkflowHistory 记录
       │         - Action = "Reject"
       │
       └──► 3. 更新 WorkflowInstance 状态为 "Rejected"
                 - 不创建新任务
                 - 流程结束
```

## 5. 前端集成

### 5.1 PendingTasks.vue 结构

```vue
<el-tabs v-model="activeTab" align="center">
    <!-- 待审批 Tab -->
    <el-tab-pane label="待审批" name="pending">
        <el-select v-model="pendingFilter"> <!-- 筛选 -->
        <el-table :data="paginatedPendingTasks"> <!-- 分页表格 -->
        <el-pagination v-model:current-page="pendingCurrentPage">
    </el-tab-pane>

    <!-- 历史记录 Tab -->
    <el-tab-pane label="历史记录" name="history">
        <el-select v-model="historyFilter"> <!-- 筛选 -->
        <el-table :data="paginatedHistoryTasks"> <!-- 分页表格 -->
        <el-pagination v-model:current-page="historyCurrentPage">
    </el-tab-pane>
</el-tabs>
```

### 5.2 审批对话框

```vue
<el-dialog v-model="showApprovalDialog" title="审批任务">
    <!-- 项目信息 -->
    <el-descriptions :column="2">

    <!-- 动态表单字段 -->
    <el-form-item
        v-for="field in approvalFormFields"
        :label="field.label"
        :required="field.required"
    >
        <el-input v-if="field.fieldType === 'text'" ... />
        <el-select v-else-if="field.fieldType === 'select'" ... />
        <el-input-number v-else-if="field.fieldType === 'number'" ... />
    </el-form-item>

    <!-- 审批意见 -->
    <el-form-item label="审批意见">
        <el-input type="textarea" v-model="approvalForm.comment" />
    </el-form-item>
</el-dialog>
```

### 5.3 API 调用

```javascript
// workflowApi.js
export const approveTask = async (taskId, comment, formData) => {
    return request.post(`/workflows/tasks/${taskId}/approve`, {
        comment,
        formData
    })
}

export const rejectTask = async (taskId, comment) => {
    return request.post(`/workflows/tasks/${taskId}/reject`, {
        comment
    })
}
```

## 6. 易错点和注意事项

### 6.1 MongoDB ObjectId 验证 ⚠️ 关键

**问题：** 旧数据中 `AssigneeId` 存储的是用户名（如 "admin"）而不是 ObjectId

**错误现象：** 待审批任务列表为空，但数据库中有数据

**根因：** `MongoRepository.GetByIdAsync()` 使用 `ObjectId.TryParse()` 验证ID，非 ObjectId 格式会抛出 `FormatException`

**修复代码 (MongoRepository.cs):**
```csharp
public async Task<T?> GetByIdAsync(string id)
{
    // 必须先验证 ObjectId 格式，无效返回 default
    if (string.IsNullOrEmpty(id) || !MongoDB.Bson.ObjectId.TryParse(id, out var objectId))
        return default;  // 不能 return null

    var filter = Builders<T>.Filter.Eq("_id", objectId);
    return await _collection.Find(filter).FirstOrDefaultAsync();
}

public async Task UpdateAsync(string id, T entity)
{
    if (string.IsNullOrEmpty(id) || !MongoDB.Bson.ObjectId.TryParse(id, out var objectId))
        return;  // 无效ID直接返回，不抛异常

    var filter = Builders<T>.Filter.Eq("_id", objectId);
    await _collection.ReplaceOneAsync(filter, entity);
}

public async Task DeleteAsync(string id)
{
    if (string.IsNullOrEmpty(id) || !MongoDB.Bson.ObjectId.TryParse(id, out var objectId))
        return;  // 无效ID直接返回，不抛异常

    var filter = Builders<T>.Filter.Eq("_id", objectId);
    await _collection.DeleteOneAsync(filter);
}
```

### 6.2 C# 可空类型返回 ⚠️ 关键

**问题：** `Task<T?>` 返回类型不能 return null

**错误代码:**
```csharp
public async Task<T?> GetByIdAsync(string id)
{
    if (!ObjectId.TryParse(id, out _))
        return null;  // ❌ CS8603: 可能返回 null
}
```

**正确代码:**
```csharp
public async Task<T?> GetByIdAsync(string id)
{
    if (!ObjectId.TryParse(id, out _))
        return default;  // ✅ default(T) 对值类型返回 0，对引用类型返回 null
}
```

### 6.3 JSON 表单数据转换 ⚠️ 关键

**问题：** 前端传递的 JSON 经过 System.Text.Json 反序列化后，值为 `JsonElement` 类型

**症状：** 表单数据保存成功，但读取时类型不对

**解决方案：** 在 WorkflowTaskService 中实现 `ConvertFormData` 方法

```csharp
private object ConvertValue(object? value)
{
    if (value is JsonElement jsonElement)
    {
        return jsonElement.ValueKind switch
        {
            JsonValueKind.String => jsonElement.GetString() ?? string.Empty,
            JsonValueKind.Number => jsonElement.TryGetInt32(out var i) ? i :
                                     jsonElement.TryGetInt64(out var l) ? l :
                                     jsonElement.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => string.Empty,
            JsonValueKind.Array => jsonElement.EnumerateArray().Select(e => ConvertValue(e)).ToList(),
            _ => jsonElement.ToString()
        };
    }
    return value ?? string.Empty;
}
```

### 6.4 审批人数据关联

**问题：** 任务中只存了 AssigneeId，需要展示用户名

**解决方案：** API 层联表查询 User 表

```csharp
var assigneeUser = await _userRepo.GetByIdAsync(task.AssigneeId);
var assigneeName = assigneeUser?.Username ?? task.AssigneeId;
```

### 6.5 查找审批节点的逻辑

**问题：** 如何确定哪个节点是第一个需要审批的节点

**当前逻辑：** 查找第一个 `NodeType == "SingleApproval"` 的节点

```csharp
var firstApprovalNode = definition.Nodes
    .FirstOrDefault(n => n.NodeType == "SingleApproval");
```

**注意：** 流程定义的 Nodes 数组中，第一个节点通常是 Start 节点，不是审批节点

### 6.6 多层审批流转

**当前实现：** 每个审批节点只创建一个任务给第一个审批人

```csharp
if (nextNode.NodeType == "SingleApproval" && nextNode.Approvers.Count > 0)
{
    await _taskRepo.CreateAsync(new WorkflowTask
    {
        AssigneeId = nextNode.Approvers[0],  // 只取第一个审批人
        ...
    });
}
```

**限制：** 不支持会签（多人同时审批）、依次审批（一人审批完再交给下一个人）

### 6.7 前端表格列宽

**问题：** 表格列宽度不适配，内容溢出或留白

**解决方案：** 使用 `min-width` 而非固定 `width`

```vue
<el-table-column
    prop="instanceName"
    label="流程名称"
    min-width="150"
    show-overflow-tooltip  <!-- 文字溢出显示tooltip -->
/>
```

### 6.8 任务超时

**问题：** 如何处理审批超时

**当前实现：** 只记录 DueDate，不自动处理

```csharp
DueDate = firstApprovalNode.TimeoutMinutes > 0
    ? DateTime.UtcNow.AddMinutes(firstApprovalNode.TimeoutMinutes)
    : null
```

**待完善：** 超时后自动通过/拒绝/通知

## 7. 数据库集合

| 集合名 | 说明 |
|--------|------|
| WorkflowDefinitions | 流程定义模板 |
| WorkflowInstances | 流程实例 |
| WorkflowTasks | 审批任务 |
| WorkflowHistories | 审批历史 |
| Users | 用户（用于关联用户名） |

## 8. 关键文件路径

### 后端
- 模型: `InventorySystem.Core/Models/Workflow*.cs`
- 服务: `InventorySystem.Infrastructure/Services/WorkflowService.cs`
- 仓库: `InventorySystem.Infrastructure/Repositories/WorkflowRepository.cs`
- 控制器: `InventorySystem.API/Controllers/WorkflowsController.cs`

### 前端
- API: `inventory-frontend/src/services/workflowApi.js`
- 待审批页面: `inventory-frontend/src/views/PendingTasks.vue`
- 我的流程: `inventory-frontend/src/views/MyWorkflows.vue`

## 9. 测试要点

1. **正常审批流程**: 启动 → 待审批 → 通过 → 流转到下一节点
2. **拒绝流程**: 启动 → 待审批 → 拒绝 → 实例状态变为 Rejected
3. **历史记录**: 查看已审批任务的完整记录
4. **表单数据**: 填写表单 → 审批时能看到数据
5. **权限**: admin 看到所有任务，普通用户只看自己待审批的任务
