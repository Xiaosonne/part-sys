# 单元测试运行指南

## 项目结构
```
InventorySystem.Tests/
├── InventorySystem.Tests.csproj
└── Services/
    ├── WorkflowServiceTests.cs
    └── WorkflowTaskServiceTests.cs
```

## 运行测试

### 方式1：命令行运行
```bash
cd InventorySystem
dotnet test InventorySystem.Tests/InventorySystem.Tests.csproj
```

### 方式2：Visual Studio Test Explorer
1. 打开 Visual Studio
2. 菜单 → Test → Test Explorer
3. 点击"Run All Tests"

### 方式3：Visual Studio Code
```bash
cd InventorySystem
dotnet test --logger "console;verbosity=detailed"
```

## 测试覆盖

### WorkflowServiceTests (4个测试)
- ✅ CreateDefinitionAsync - 创建流程定义
- ✅ StartInstanceAsync - 启动流程实例并创建首个任务
- ✅ GetDefinitionsAsync - 按分类查询流程定义
- ✅ CancelInstanceAsync - 撤回流程

### WorkflowTaskServiceTests (4个测试)
- ✅ ApproveTaskAsync - 审批任务并自动流转到下一节点
- ✅ ApproveTaskAsync_ShouldCompleteWhenReachingEndNode - 到达结束节点时完成流程
- ✅ RejectTaskAsync - 拒绝任务并标记流程为已拒绝
- ✅ GetPendingTasksAsync - 获取用户待办任务

## 测试框架
- **xUnit**: 测试框架
- **Moq**: Mock库，用于模拟Repository依赖

## 关键测试场景
1. **流程创建** - 验证流程定义创建和时间戳
2. **流程启动** - 验证实例创建和首个任务生成
3. **任务审批** - 验证自动流转到下一节点
4. **流程完成** - 验证到达End节点时标记为Completed
5. **任务拒绝** - 验证流程状态变更为Rejected
