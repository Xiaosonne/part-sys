# 通用审批流程系统需求文档

**文档版本**: 1.0
**创建日期**: 2026-03-18
**目标项目**: PartSelectionSystem - 库存管理系统
**首个应用场景**: 项目文件审批（图纸审批）

---

## 1. 需求背景

### 1.1 业务场景

在库存管理系统中，需要对项目相关的文件、配件选型、库存操作等业务进行审批管理。当前系统已有简单的状态流转（如 SelectionPlan 的 draft → submitted → approved），但缺乏灵活的审批流程配置能力。

### 1.2 核心诉求

- 支持动态配置审批流程，无需修改代码
- 支持多种审批模式（单人、多人协同）
- 支持复杂的流程结构（分支、合并）
- 支持自定义审批表单
- 与现有业务实体（项目、文件）深度集成

## 2. 功能需求

### 2.1 审批流程引擎

#### 2.1.1 技术选型

- **底层框架**: workflow-core
- **存储方式**: MongoDB（与现有系统保持一致）
- **流程定义**: 数据库驱动，支持动态配置

#### 2.1.2 流程生命周期

```
创建流程定义 → 启动流程实例 → 执行审批节点 → 流程完成/终止
```

### 2.2 审批节点类型

#### 2.2.1 单人审批节点

- 指定单个审批人，该审批人审批后流程继续
- 支持动态指定审批人（如流程发起人的上级）
- 支持超时时间和自动通过/拒绝规则

#### 2.2.2 多人会签节点（AND）

- 指定多个审批人，所有人都审批通过后流程才继续
- 支持审批顺序（串行/并行）
- 支持一票否决机制

#### 2.2.3 多人或签节点（OR）

- 指定多个审批人，任意一人审批通过后流程继续
- 支持最少通过人数配置

#### 2.2.4 条件分支节点

- 根据条件表达式决定流程走向
- 支持审批结果、表单字段、业务数据作为条件

#### 2.2.5 合并节点

- 多个分支汇聚到同一节点
- 支持等待所有分支或等待任一分支

### 2.3 自定义表单系统

支持的字段类型：

- 文本输入、多行文本、数字、日期、下拉选择、多选框
- 文件上传、项目文件选择、审批意见

每个审批节点可配置独立的表单，支持条件显示和表单模板复用。

### 2.4 文件管理功能

#### 2.4.1 文件上传

- 审批人在审批时可上传新文件（如签字文件、补充材料）
- 使用现有文件存储系统，bucket 为 `approvals`

#### 2.4.2 项目文件选择

- 流程启动时从项目工作区域选择文件进行审批
- 支持单个/多个/按文件夹选择
- 审批过程中文件只读

### 2.5 业务集成

#### 2.5.1 项目审批流程

- 项目文件提交审批（如设计图纸审批）
- 项目阶段审批（如项目立项、验收）
- 配件选型审批（SelectionPlan 审批）

#### 2.5.2 其他业务实体

- 预留扩展接口，支持关联其他业务实体（Part、StockTransaction 等）

### 2.6 权限控制

- **流程定义权限**: 只有 admin 可创建/修改/删除
- **流程实例权限**: 发起人可查看和撤回自己的流程，审批人可查看待审批流程
- **审批权限**: 只有指定审批人可审批对应节点

## 3. 数据模型设计

### 3.1 核心实体

#### WorkflowDefinition（流程定义）

- Id, Name, Description, Version, IsActive
- Category（流程分类）, EntityType（关联业务实体类型）
- Nodes（流程节点定义列表）
- CreatedBy, CreatedAt, UpdatedAt

#### WorkflowNode（流程节点定义）

- Id, NodeType, Name, ApprovalMode
- Approvers（审批人列表）, ApproverSource（静态/动态）
- FormFields（表单字段定义）, Conditions（条件表达式）
- NextNodes（下一节点ID列表）
- TimeoutMinutes, TimeoutAction

#### FormField（表单字段定义）

- Id, FieldType, Label, Key, Required
- DefaultValue, Placeholder, Options
- Validation, DisplayCondition

#### WorkflowInstance（流程实例）

- Id, DefinitionId, DefinitionVersion, Name, Status
- EntityType, EntityId（关联业务实体）
- SelectedFileIds（选中的项目文件ID列表）
- CurrentNodeId, FormData, StartedBy
- StartedAt, CompletedAt, UpdatedAt

#### WorkflowTask（审批任务）

- Id, InstanceId, NodeId, NodeName
- AssigneeId（审批人）, Status
- FormData, Comment, UploadedFileIds
- CreatedAt, CompletedAt, DueDate

#### WorkflowHistory（流程历史）

- Id, InstanceId, NodeId, NodeName
- Action（Start, Approve, Reject, Transfer, Cancel）
- OperatorId, OperatorName, Comment, FormData
- CreatedAt

## 4. API 设计

### 4.1 流程定义管理

- POST /api/workflows/definitions - 创建流程定义
- GET /api/workflows/definitions - 获取流程定义列表
- GET /api/workflows/definitions/{id} - 获取流程定义详情
- PUT /api/workflows/definitions/{id} - 更新流程定义
- DELETE /api/workflows/definitions/{id} - 删除流程定义
- POST /api/workflows/definitions/{id}/publish - 发布流程定义

### 4.2 流程实例管理

- POST /api/workflows/instances - 启动流程
- GET /api/workflows/instances - 获取流程实例列表
- GET /api/workflows/instances/{id} - 获取流程实例详情
- POST /api/workflows/instances/{id}/cancel - 撤回流程

### 4.3 审批任务

- GET /api/workflows/tasks/pending - 获取待办任务
- GET /api/workflows/tasks/completed - 获取已办任务
- POST /api/workflows/tasks/{id}/approve - 审批任务
- POST /api/workflows/tasks/{id}/reject - 拒绝任务
- POST /api/workflows/tasks/{id}/transfer - 转交任务

### 4.4 文件选择

- GET /api/workflows/projects/{projectId}/files - 获取项目文件树

## 5. 前端 UI 设计

### 5.1 流程定义管理界面（管理员）

- 流程定义列表（表格展示）
- 可视化流程设计器（拖拽式节点、连线、配置面板）
- 表单设计器（字段拖拽、属性配置、预览）

### 5.2 流程发起界面

- 选择流程定义（分类筛选、卡片展示）
- 填写启动表单（动态表单、项目文件选择、文件上传）
- 流程预览（流程图、审批节点）

### 5.3 待办任务界面

- 任务列表（表格展示、筛选、操作）
- 审批详情页（流程信息、审批表单、关联文件、审批历史）

### 5.4 流程跟踪界面

- 我发起的流程（列表、状态筛选、操作）
- 流程详情页（进度可视化、审批历史、关联文件）

### 5.5 项目集成界面

- 在 ProjectDetail.vue 中添加"审批流程"标签页
- 展示项目的所有审批流程
- 快速发起审批按钮

## 6. 技术实现要点

### 6.1 workflow-core 集成

- 安装 WorkflowCore 和 WorkflowCore.Persistence.MongoDB
- 使用 workflow-core 的 DSL 或代码方式定义工作流
- 将数据库中的 WorkflowDefinition 转换为 workflow-core 的工作流定义
- 实现自定义审批步骤、条件分支步骤、文件处理步骤

### 6.2 数据持久化

- 使用 WorkflowCore.Persistence.MongoDB
- 实现仓储：WorkflowDefinitionRepository, WorkflowInstanceRepository, WorkflowTaskRepository, WorkflowHistoryRepository

### 6.3 服务层设计

- WorkflowService：流程定义管理、流程实例管理、流程启动和执行
- WorkflowTaskService：任务分配、任务审批、任务转交
- WorkflowNotificationService：任务通知（站内消息）、超时提醒、流程状态变更通知

### 6.4 前端技术栈

- 流程设计器：bpmn-js 或 LogicFlow
- 表单渲染：动态表单组件（基于 Element Plus）
- 流程可视化：流程图展示、节点状态高亮、进度追踪

## 7. 实施计划

**用户反馈调整：**

- 首个应用场景：项目文件审批（图纸审批）
- 第一阶段需要可视化流程设计器
- 通知方式：站内消息
- 时间安排：8-11周分阶段实施

### 7.1 第一阶段：基础框架 + 可视化设计器（3-4周）

- [X] workflow-core 集成和配置
- [X] 数据模型设计和实现（WorkflowDefinition, WorkflowNode, FormField, WorkflowInstance, WorkflowTask, WorkflowHistory）
- [X] 基础 Repository 实现
- [X] 基础 Service 实现
- [X] 基础 API 开发（流程定义 CRUD）
- [X] 可视化流程设计器（bpmn-js 或 LogicFlow）
- [X] 简单的流程执行引擎（支持单人审批节点）

### 7.2 第二阶段：项目文件审批功能（2-3周）

- [X] 审批节点实现（单人、多人AND、多人OR）
- [X] 审批任务管理
- [X] 审批 API 开发
- [X] 项目文件选择功能
- [X] 前端审批界面

### 7.3 第三阶段：高级功能（2-3周）

- [X] 条件分支和合并节点（多人审批支持）
- [X] 自定义表单系统
- [X] 文件上传功能（项目文件选择）
- [X] 流程历史和进度追踪

### 7.4 第四阶段：通知和集成（1-2周）

- [ ] 站内消息通知系统
- [X] 项目详情页集成（重新初始化工作区）
- [X] 权限控制完善
- [ ] 流程模板库

### 7.5 第五阶段：测试和优化（1-2周）

- [X] 单元测试（12个测试全部通过）
- [ ] 集成测试
- [ ] 性能优化
- [ ] 用户培训文档

## 8. 实施进度

**开始日期**: 2026-03-18

### 第一阶段进度

- **2026-03-18**:

  - ✅ 完成数据模型设计和实现
    - WorkflowDefinition（流程定义）
    - WorkflowNode（流程节点）
    - FormField（表单字段）
    - WorkflowInstance（流程实例）
    - WorkflowTask（审批任务）
    - WorkflowHistory（流程历史）
  - ✅ 完成基础 Repository 实现
    - IWorkflowDefinitionRepository & WorkflowDefinitionRepository
    - IWorkflowInstanceRepository & WorkflowInstanceRepository
    - IWorkflowTaskRepository & WorkflowTaskRepository
    - IWorkflowHistoryRepository & WorkflowHistoryRepository
  - ✅ 完成基础 Service 实现
    - IWorkflowService & WorkflowService（流程定义和实例管理）
    - IWorkflowTaskService & WorkflowTaskService（任务审批）
  - ✅ 完成基础 API 开发
    - WorkflowsController with 11 endpoints
    - 流程定义 CRUD（创建、查询、更新、删除）
    - 流程实例管理（启动、查询、撤回）
    - 审批任务操作（获取待办、通过、拒绝）
  - ✅ workflow-core 集成和配置
    - 在 Program.cs 中注册所有依赖
    - 支持单人审批节点的基础流程执行
  - ✅ 代码编译成功
  - ✅ 单元测试实现和验证
    - InventorySystem.Tests 项目创建
    - WorkflowServiceTests (4个测试用例)
    - WorkflowTaskServiceTests (4个测试用例)
    - 所有8个测试通过 ✅
  - ✅ 前端可视化流程设计器实现
    - WorkflowDesigner.vue - 拖拽式流程设计器
    - PendingTasks.vue - 待办任务列表
    - MyWorkflows.vue - 我发起的流程列表
    - workflowApi.js - 工作流API服务
    - 路由配置更新
- **2026-03-19**:

  - ✅ 发起新流程功能实现
    - StartWorkflow.vue - 发起新流程页面
    - 流程定义选择（分类筛选、卡片展示）
    - 启动表单填写（动态表单、项目选择）
    - 项目文件树选择（多文件选择、树形结构）
    - 流程启动和提交
  - ✅ 前端路由和菜单集成
    - 在 Layout.vue 中添加"发起流程"菜单项
    - 路由配置：/workflows/start
  - ✅ 文件树选择功能
    - 支持多文件选择
    - 树形结构展示项目文件
    - 数据格式转换和处理

  - ✅ 待办任务功能完善
    - 修复待办任务空列表问题（查找第一个SingleApproval节点）
    - 实现流程实例自定义名称保存和显示
    - 增强待办任务API返回发起人姓名、审批人姓名、流程名称
    - 增强流程实例详情API返回用户名信息
    - 添加审批历史查询端点 GET /api/workflows/instances/{id}/history
    - 修复 WorkflowDesigner.vue 审批人使用用户下拉选择

  - ✅ 自动调试系统实现
    - 添加 Serilog 日志框架（结构化日志）
    - 进程ID追踪（process.pid 文件）
    - 日志文件按进程ID分离（app-{processId}-YYYY-MM-DD.log）
    - 日志滚动策略（按天滚动，保留30天）

  - ✅ 流程表单字段功能
    - WorkflowNode 添加 FormFields 属性
    - WorkflowInstance/Task/History 添加 FormData 字典
    - WorkflowDesigner.vue 表单字段配置界面（支持 text/textarea/select/number/checkbox）
    - PendingTasks.vue 审批对话框动态渲染表单字段
    - WorkflowDetail.vue 显示审批历史中的表单数据
    - 必填字段验证
    - 单元测试（12个测试全部通过）

  - ✅ 流程系统UI改进
    - StartWorkflow.vue 简化（移除动态表单，只保留流程名称和文件选择）
    - 文件平铺展示（卡片网格，按文件夹分组，按后缀显示图标）
    - 文件详情弹窗（名称、大小、上传时间、上传人）
    - PendingTasks.vue 审批对话框增强（显示项目信息：名称、状态、发起人、发起时间）
    - WorkflowDetail.vue 样式修复（流程完成时进度条全绿）

- **2026-03-20**:

  - ✅ 流程关联项目信息显示
    - WorkflowInstanceDetailDto 添加 EntityType 和 EntityId 字段
    - WorkflowDetail.vue 根据 entityType/entityId 获取项目详情并显示
    - 显示项目名称、编号、描述等信息

  - ✅ 中文目录显示支持
    - 创建 WorkspaceStructure 模型存储目录元数据
    - 实现 IWorkspaceStructureRepository 接口
    - 修改 WorkspaceInitializer 创建文件夹时保存元数据到数据库
    - 修改 FilesController.ListFiles 返回 displayName
    - 修改 ProjectsController.ReinitializeWorkspace 删除旧元数据
    - 前端 ProjectDetail.vue 使用 displayName 显示中文目录名

### 已实现的功能

1. **流程定义管理**

   - 创建、查询、更新、删除流程定义
   - 支持按分类查询
   - 版本管理
2. **流程实例管理**

   - 启动流程实例
   - 关联业务实体（EntityType/EntityId）
   - 支持选择项目文件
   - 查询用户发起的流程
   - 撤回流程
3. **审批任务管理**

   - 获取待办任务（包含发起人姓名、审批人姓名、流程名称）
   - 审批通过（自动流转到下一节点）
   - 审批拒绝（标记流程为已拒绝）
   - 审批时填写表单数据
   - 记录审批历史（包含表单数据）
   - 查询已办任务历史

4. **流程执行引擎**

   - 支持单人审批节点
   - 支持多人审批节点（多审批人配置）
   - 自动流转到下一节点
   - 支持流程结束节点
   - 记录完整的审批历史

5. **自定义表单系统**

   - 流程节点表单字段配置（text/textarea/select/number/checkbox）
   - 动态表单渲染（发起流程和审批时）
   - 必填字段验证
   - 表单数据持久化到数据库

6. **工作区管理**

   - 中文目录显示（displayName 元数据）
   - 项目工作区重新初始化
   - 文件详情查看（名称、大小、上传时间、上传人）

### 下一步计划

- [X] 可视化流程设计器（LogicFlow 自定义实现）
- [X] 项目文件选择功能
- [X] 多人审批节点（支持多审批人配置）
- [X] 条件分支和合并节点（支持多人审批）
- [X] 自定义表单系统
- [X] 文件上传功能（项目文件选择）
- [X] 前端审批界面详情页（PendingTasks, WorkflowDetail）
- [ ] 站内消息通知系统
- [ ] 条件分支节点（可视化判断）
- [ ] 流程模板库

## 技术实现细节

### 后端实现

#### 1. 流程定义管理 (WorkflowsController)

- **创建流程**: POST /api/workflows/definitions
- **查询流程**: GET /api/workflows/definitions
- **更新流程**: PUT /api/workflows/definitions/{id}
- **删除流程**: DELETE /api/workflows/definitions/{id}

#### 2. 流程实例管理

- **启动流程**: POST /api/workflows/instances
  - 支持关联业务实体 (EntityType/EntityId)
  - 支持选择项目文件 (SelectedFileIds)
- **查询实例**: GET /api/workflows/instances
- **撤回流程**: POST /api/workflows/instances/{id}/cancel

#### 3. 审批任务管理

- **获取待办**: GET /api/workflows/tasks/pending
- **审批通过**: POST /api/workflows/tasks/{id}/approve
- **审批拒绝**: POST /api/workflows/tasks/{id}/reject

#### 4. 项目文件初始化

- **重新初始化**: POST /api/projects/{id}/reinitialize-workspace
  - 删除现有文件和数据库记录
  - 根据配置重新创建文件夹结构
  - 权限: admin only

### 前端实现

#### 1. 流程设计器 (WorkflowDesigner.vue)

- 拖拽式节点创建和移动
- SVG连线显示节点关系
- 节点配置面板（名称、审批人、超时时间、下一节点）
- 流程加载和XML导出

#### 2. 发起流程 (StartWorkflow.vue)

- 流程定义选择（卡片展示）
- 启动表单填写（动态表单）
- 项目选择和文件树选择
- 多文件选择支持

#### 3. 待办任务 (PendingTasks.vue)

- 任务列表展示
- 审批通过/拒绝操作
- 任务详情查看

#### 4. 我的流程 (MyWorkflows.vue)

- 已发起流程列表
- 流程状态显示
- 流程撤回功能

#### 5. 项目详情集成 (ProjectDetail.vue)

- 文件管理功能
- 项目文件结构重新初始化按钮
- 确认对话框防止误操作

### API 统一规范

#### 文件API

- **项目管理**: GET /api/files/list?bucket=projects&path=...&relatedId=...
  - 返回树形结构（包含文件夹）
  - 用于显示完整的文件树
- **审批流程**: 使用相同的 /api/files/list API
  - 统一使用树形结构
  - 支持多文件选择

#### 认证

- 所有API请求自动附加 Bearer token
- 使用 axios 请求拦截器实现
- 权限控制: admin/warehouse/user 三个角色

## 8. 风险和挑战

### 8.1 技术风险

- workflow-core 学习曲线
- 复杂流程的性能问题
- 流程定义的版本管理

### 8.2 业务风险

- 流程配置的复杂度
- 用户使用习惯的培养
- 与现有业务流程的冲突

### 8.3 应对措施

- 提供流程模板库
- 分阶段上线，先简单后复杂
- 充分的用户培训和文档支持

## 9. 附录

### 9.1 参考资料

- workflow-core 官方文档: https://github.com/danielgerlag/workflow-core
- BPMN 2.0 规范
- 现有系统架构文档（CLAUDE.md）

### 9.2 相关文件

- `InventorySystem/InventorySystem.Core/Models/` - 现有数据模型
- `InventorySystem/InventorySystem.API/Controllers/FilesController.cs` - 文件管理API
- `inventory-frontend/src/views/ProjectDetail.vue` - 项目详情页
- `docs/project-workspace-structure.json` - 项目工作区域配置

# 技术实现细节要求
