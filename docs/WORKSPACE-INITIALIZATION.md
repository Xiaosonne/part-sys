# 项目工作区域初始化系统

## 概述

项目创建时会自动根据 `project-workspace-structure.json` 配置文件初始化工作区域文件夹结构。

## 配置文件

**位置**: `docs/project-workspace-structure.json`

**结构**:
```json
{
  "workspaceAreas": [
    {
      "id": "folder_id",
      "name": "文件夹显示名称",
      "type": "folder",
      "children": [
        // 子文件夹
      ]
    }
  ]
}
```

## 工作流程

### 1. 修改配置文件

编辑 `project-workspace-structure.json` 来定义项目的文件夹结构。

### 2. 项目创建时自动初始化

当创建新项目时（type="project"），系统会：
1. 读取 `project-workspace-structure.json`
2. 根据配置递归创建文件夹结构
3. 所有文件夹都在项目ID下创建

### 3. 文件夹路径规则

- 根文件夹: `{projectId}/{folder_id}`
- 子文件夹: `{projectId}/{parent_id}/{child_id}`

## 示例

### 配置文件示例

```json
{
  "workspaceAreas": [
    {
      "id": "docs",
      "name": "文档区",
      "type": "folder",
      "children": []
    },
    {
      "id": "drawings",
      "name": "图纸区",
      "type": "folder",
      "children": [
        {
          "id": "sensor_box",
          "name": "传感器箱图纸区",
          "type": "folder",
          "children": []
        }
      ]
    }
  ]
}
```

### 创建的文件夹结构

对于项目ID `proj_123`:
```
proj_123/
├── docs/
└── drawings/
    └── sensor_box/
```

## 修改配置时的步骤

1. **编辑 JSON 文件**
   - 修改 `docs/project-workspace-structure.json`
   - 添加、删除或修改文件夹配置

2. **新项目自动应用**
   - 创建新项目时会使用最新配置
   - 现有项目不受影响

3. **现有项目手动初始化**
   - 如需为现有项目应用新配置，需要手动创建文件夹
   - 或通过 API 调用 `POST /api/files/folder` 创建

## 相关代码

- **接口**: `InventorySystem.Core.Interfaces.IWorkspaceInitializer`
- **实现**: `InventorySystem.Infrastructure.Services.WorkspaceInitializer`
- **配置**: `docs/project-workspace-structure.json`
- **触发点**: `InventorySystem.API.Controllers.ProjectsController.Create()`

## 错误处理

- 如果配置文件不存在，初始化会被跳过
- 如果初始化失败，不会影响项目创建
- 错误会被记录到控制台
