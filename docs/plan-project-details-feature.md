# 项目列表页面添加项目详情功能

## Context

用户需要在项目列表页面添加项目详情展示功能，具体需求：
1. 展示项目关联的选型计划内容
2. 展示每个选型配件的库存信息：
   - 当前库存（totalQty）
   - 占用的库存（lockedQty）
   - 剩余库存（availableQty）

**当前状态：**
- Projects.vue 使用 el-table 展示项目树形结构
- 显示字段：name, type, status, createdAt
- 有 Edit 和 Delete 操作
- 没有详情展示功能

**可用API：**
- `getSelections(projectId)` - 获取项目的选型计划
- `getParts()` - 获取所有配件信息

## 实现方案

### UI设计：使用 el-table 展开行功能

**选择理由：**
- 最小化代码修改，Element Plus 原生支持
- 用户体验流畅，直接在表格中展开查看
- 性能优化：点击时才加载详情数据
- 避免额外的弹窗，保持界面简洁

### 数据加载策略

**按需加载（点击展开时加载）：**
1. 用户点击展开按钮时触发 `expand-change` 事件
2. 检查缓存，如果未加载则调用API
3. 获取选型计划列表
4. 关联配件信息，提取库存数据
5. 缓存结果，避免重复请求

### 实现步骤

#### Step 1: 添加展开列
在 el-table 中添加 `type="expand"` 的列，用于展示项目详情。

#### Step 2: 引入API和数据管理
- 引入 `getSelections` 和 `getParts` API
- 创建 `parts` 响应式变量存储配件列表
- 创建 `projectDetails` Map 缓存已加载的项目详情

#### Step 3: 实现数据加载逻辑
- 在 `onMounted` 中加载配件列表（用于后续关联）
- 实现 `handleExpand` 函数处理展开事件
- 实现 `loadProjectDetails` 函数加载和处理数据

#### Step 4: 展示详情内容
在展开区域展示：
- 选型计划名称
- 选型配件列表表格：
  - 配件名称
  - 需求数量（requiredQty）
  - 当前库存（totalQty）
  - 占用库存（lockedQty）
  - 剩余库存（availableQty）

### 数据处理流程

```
1. getSelections(projectId) → 获取选型计划列表
2. 遍历每个选型计划的 items 数组
3. 通过 selectedPartId 在 parts 中查找配件
4. 提取库存信息：totalQty, lockedQty, availableQty
5. 组装展示数据
```

### 边界情况处理

1. **项目类型为 folder**：folder 不应该有选型计划，不显示展开按钮
2. **没有选型计划**：展开后显示 "暂无选型计划"
3. **配件已删除**：显示配件ID和"(已删除)"提示
4. **API调用失败**：显示错误提示，不影响其他功能

## 关键文件

**需要修改：**
- `C:\Users\xiaos\Desktop\demo\inventory-frontend\src\views\Projects.vue` - 主要修改文件

**需要引入：**
- `C:\Users\xiaos\Desktop\demo\inventory-frontend\src\api\selections.js` - 获取选型计划
- `C:\Users\xiaos\Desktop\demo\inventory-frontend\src\api\parts.js` - 获取配件信息

## 验证方法

### 功能验证
1. 启动前端和后端服务
2. 登录系统，进入Projects页面
3. 点击项目行的展开按钮
4. 验证显示选型计划列表
5. 验证库存数据正确显示

### 性能验证
1. 打开浏览器开发者工具 Network 面板
2. 首次展开项目，观察API调用
3. 折叠后再次展开，确认使用缓存（无新请求）

### 边界验证
1. 测试没有选型计划的项目
2. 测试folder类型的项目
3. 测试选型中引用已删除配件的情况

## 实施状态

✅ 已完成实施（2026-03-13）
