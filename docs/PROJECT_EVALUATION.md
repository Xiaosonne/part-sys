# PartSelectionSystem 项目评价报告

> 生成日期: 2026-03-20

## 整体架构：★★★☆☆ (3/5)

三层清洁架构（Core → Infrastructure → API）结构清晰，但实现质量参差不齐。

---

## 问题清单

### 🔴 高优先级

#### 1. GetPendingTasks N+1 查询问题
**位置**: `WorkflowsController.cs` 第145-188行

**问题**: 每个待审批任务触发4次额外查询
```csharp
var instance = await _workflowService.GetInstanceAsync(task.InstanceId);        // Query 1
var startedByUser = await _userRepo.GetByIdAsync(instance.StartedBy);          // Query 2
var assigneeUser = await _userRepo.GetByIdAsync(task.AssigneeId);              // Query 3
var definition = await _workflowService.GetDefinitionAsync(instance.DefinitionId); // Query 4
```

**影响**: 20个任务 = 80+ 次数据库查询

**优化方案**: 批量查询，在内存中 JOIN

---

#### 2. 前端重复 axios 实例
**位置**:
- `src/api/request.js` - axios 实例1
- `src/api/workflowApi.js` - axios 实例2

**问题**:
- 两套 baseURL 配置
- 认证逻辑重复
- 维护困难

**优化方案**: 统一为单个 axios 实例

---

#### 3. 前端缺少状态管理
**位置**: 所有 .vue 组件

**问题**:
- 无 Pinia/Vuex
- 每个组件独立加载数据
- `Projects.vue`、`ProjectDetail.vue`、`StartWorkflow.vue` 都调用 `loadParts()`
- 项目详情缓存用 Map 但未复用

**优化方案**: 引入 Pinia

---

### 🟡 中优先级

#### 4. 缺少全局异常处理
**位置**: 所有 Controllers

**问题**:
- 无 global exception middleware
- 部分 Controller 有 try-catch，部分没有
- 错误响应格式不统一

#### 5. PartsController 无日志
**位置**: `PartsController.cs`

**问题**:
- 关键业务操作无日志记录
- 异常时无错误追踪

#### 6. 代码重复（前端）
| 函数 | 重复位置 |
|------|---------|
| `getNodeTypeLabel` | WorkflowDesigner.vue, WorkflowDetail.vue |
| `formatSize` | 多个组件 |
| `formatDate` | 多个组件 |
| `buildFileTree` | StartWorkflow.vue, ProjectDetail.vue |

#### 7. 组件过大
| 组件 | 行数 | 问题 |
|------|------|------|
| WorkflowDesigner.vue | 844 | 拖拽逻辑、表单配置、渲染混在一起 |
| StartWorkflow.vue | 531 | 动态表单渲染逻辑 |
| ProjectDetail.vue | 454 | 文件管理、选型计划、文件夹创建 |

---

### 🟢 低优先级

#### 8. API 设计不RESTful
```csharp
[HttpPost("tasks/{id}/approve")]   // 应为 PATCH on task
[HttpPost("tasks/{id}/reject")]
```

#### 9. PartsController 查询解析不优雅
```csharp
// 手动解析 "field:op:value" 格式
foreach (var item in criteria.Split(','))
```

---

## 代码质量评分

| 类别 | 评分 | 说明 |
|------|------|------|
| Backend Service Quality | 3/5 | StockService良好，WorkflowService有N+1 |
| Exception Handling | 2/5 | 不一致，缺少全局处理器 |
| API RESTfulness | 3/5 | 基本REST，workflow动作打破规范 |
| Code Reuse DRY | 3/5 | MongoRepository好，Controller违反DRY |
| Frontend Component Split | 3/5 | 拆分合理但部分组件过大 |
| Frontend API Management | 2/5 | axios实例重复 |
| State Management | 2/5 | 无集中式store |
| Frontend Code Repetition | 2/5 | 多处重复函数 |

**综合评分: 2.7/5**

---

## 优化计划

### Phase 1: 紧急修复
- [ ] 修复 GetPendingTasks N+1 查询
- [ ] 合并前端 axios 实例
- [ ] 添加全局异常处理中间件

### Phase 2: 架构改进
- [ ] 引入 Pinia 状态管理
- [ ] 提取公共工具函数
- [ ] 拆分超大组件

### Phase 3: 功能增强
- [ ] 操作审计日志
- [ ] 配件分类管理
- [ ] 库存预警系统
- [ ] WebSocket实时更新

---

## 建议新增功能

1. **操作审计日志** - 记录所有库存操作（谁、什么时候、做了什么）
2. **配件分类管理** - 多级分类 + 分类筛选
3. **库存预警** - 可用数量低于阈值时提醒
4. **多语言支持** - 考虑未来国际化
5. **WebSocket实时更新** - 库存变化时实时推送到前端
6. **流程模板市场** - 预置常见审批流程模板
