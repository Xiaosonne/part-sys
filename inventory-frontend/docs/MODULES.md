# 前端项目模块需求总结

## 项目概述
Vue 3 + Vite + Element Plus 库存管理系统前端

**API 基地址**: http://localhost:5128/api
**认证方式**: JWT Bearer Token (localStorage)
**角色权限**: admin / warehouse / user

---

## 1. 认证模块 (Auth)

### Login.vue
**主要需求**:
- 用户名/密码登录表单
- JWT token 存储到 localStorage
- 登录成功后跳转到 Parts 页面
- 错误提示显示

**API 调用**:
- POST `/api/auth/login` - 用户登录
- POST `/api/auth/register` - 注册（admin 专用）
- GET `/api/auth/me` - 获取当前用户信息

**权限**: 无需认证

---

## 2. 零件管理模块 (Parts)

### Parts.vue
**主要需求**:
- 零件列表展示（表格）
- 零件 CRUD 操作
- 零件文件管理
- 库存数量显示（总数、可用、锁定）

**表格字段**:
- Name, Model, Brand, Category
- TotalQty, AvailableQty, LockedQty
- Actions (Edit, Files, Delete)

**API 调用**:
- GET `/api/parts` - 获取零件列表
- POST `/api/parts` - 创建零件
- PUT `/api/parts/{id}` - 编辑零件
- DELETE `/api/parts/{id}` - 删除零件
- GET `/api/files/part/{partId}` - 获取零件文件

**权限**:
- admin/warehouse: 完全 CRUD
- user: 只读

---

## 3. 库存操作模块 (Stock)

### Stock.vue
**主要需求**:
- 5 个操作标签页：Inbound, Outbound, Lock, Unlock, Return
- 每个操作需要：零件选择、数量输入、备注
- 实时库存数量更新

**操作类型**:
| 操作 | TotalQty | LockedQty | AvailableQty |
|------|----------|-----------|--------------|
| Inbound | +qty | 0 | +qty |
| Outbound | -qty | 0 | -qty |
| Lock | 0 | +qty | -qty |
| Unlock | 0 | -qty | +qty |
| Return | +qty | 0 | +qty |

**API 调用**:
- POST `/api/stock/inbound` - 入库
- POST `/api/stock/outbound` - 出库
- POST `/api/stock/lock` - 锁定
- POST `/api/stock/unlock` - 解锁
- POST `/api/stock/return` - 退货

**权限**:
- admin/warehouse: 所有操作
- user: 仅 Return

---

## 4. 交易记录模块 (Transactions)

### Transactions.vue
**主要需求**:
- 库存交易历史记录表格
- 显示交易类型、数量、操作人、时间
- 支持筛选和搜索

**表格字段**:
- PartId, Type, Quantity, OperatorId
- ProjectId, RecipientId, RecipientName
- Note, CreatedAt

**API 调用**:
- GET `/api/stock/transactions` - 获取交易记录

**权限**: 所有认证用户可读

---

## 5. 项目管理模块 (Projects)

### Projects.vue
**主要需求**:
- 项目树形结构展示
- 项目 CRUD 操作
- 支持文件夹和项目两种节点类型
- 项目状态管理

**节点类型**:
- folder: 文件夹（用于组织）
- project: 项目

**API 调用**:
- GET `/api/projects` - 获取项目列表
- POST `/api/projects` - 创建项目
- PUT `/api/projects/{id}` - 编辑项目
- DELETE `/api/projects/{id}` - 删除项目

**权限**:
- admin: 完全 CRUD
- warehouse/user: 只读

---

## 6. 项目详情模块 (ProjectDetail)

### ProjectDetail.vue
**主要需求**:
- 左侧树形导航（文件、选择计划、零件文件）
- 右侧内容区域（文件管理、选择计划、零件详情）
- 多级目录支持
- 文件上传/下载/删除

**子功能**:

#### 6.1 文件管理
- 多级目录浏览（面包屑导航）
- 文件上传（拖拽或点击）
- 文件下载
- 文件/文件夹删除
- 创建文件夹

**API 调用**:
- GET `/api/files/list` - 列出文件
- POST `/api/files/upload` - 上传文件
- POST `/api/files/folder` - 创建文件夹
- GET `/api/files/{id}/download` - 下载文件
- DELETE `/api/files/{id}` - 删除文件
- DELETE `/api/files/folder` - 删除文件夹

#### 6.2 选择计划
- 选择计划列表
- 选择计划详情（包含零件列表）
- 计划状态：draft → submitted → approved

**API 调用**:
- GET `/api/selections` - 获取选择计划
- POST `/api/selections` - 创建计划
- PUT `/api/selections/{id}` - 编辑计划
- POST `/api/selections/{id}/submit` - 提交计划
- GET `/api/selections/{id}/match` - 匹配零件

#### 6.3 零件文件
- 零件详情展示
- 零件文件多级目录管理
- 文件上传/下载/删除

**权限**:
- admin/warehouse: 文件上传、创建文件夹、删除
- 所有用户: 文件下载

---

## 7. 选择计划模块 (Selections)

### Selections.vue
**主要需求**:
- 选择计划列表
- 计划状态管理
- 计划项目管理

**计划状态流**:
draft → submitted → approved

**API 调用**:
- GET `/api/selections` - 获取计划列表
- POST `/api/selections` - 创建计划
- PUT `/api/selections/{id}` - 编辑计划
- DELETE `/api/selections/{id}` - 删除计划
- POST `/api/selections/{id}/submit` - 提交计划

**权限**:
- admin: 完全管理
- warehouse/user: 创建和管理自己的计划

---

## 8. 布局模块 (Layout)

### Layout.vue
**主要需求**:
- 顶部导航栏（用户信息、登出）
- 左侧菜单栏（根据角色显示）
- 主内容区域

**菜单项**:
- Parts (所有用户)
- Stock (admin/warehouse)
- Transactions (所有用户)
- Projects (所有用户)
- Selections (所有用户)

**角色菜单差异**:
- admin: 显示所有菜单
- warehouse: 隐藏 Projects/Selections
- user: 隐藏 Stock

---

## 9. 用户管理模块 (Users)

### Users.vue
**主要需求**:
- 用户列表展示（表格）
- 用户 CRUD 操作
- 密码重置功能
- 用户状态管理（Active/Inactive）

**表格字段**:
- Username, Display Name, Email
- Role (admin/warehouse/user)
- Status (Active/Inactive)
- Actions (Edit, Reset Password, Delete)

**API 调用**:
- GET `/api/users` - 获取用户列表
- GET `/api/users/{id}` - 获取用户详情
- POST `/api/users` - 创建用户
- PUT `/api/users/{id}` - 编辑用户
- DELETE `/api/users/{id}` - 删除用户
- POST `/api/users/{id}/reset-password` - 重置密码

**权限**: admin 专用

---

## API 模块 (api/)

### request.js
- Axios 实例配置
- JWT token 自动注入
- 错误处理

### auth.js
- login(username, password)
- register(username, password, displayName, email)
- getMe()

### parts.js
- getParts()
- createPart(data)
- updatePart(id, data)
- deletePart(id)

### stock.js
- inbound(partId, quantity, remark)
- outbound(partId, quantity, remark)
- lock(partId, quantity, projectId, remark)
- unlock(partId, quantity, remark)
- return(partId, quantity, remark)
- getTransactions()

### projects.js
- getProjects()
- getProject(id)
- createProject(data)
- updateProject(id, data)
- deleteProject(id)

### selections.js
- getSelections(projectId)
- createSelection(data)
- updateSelection(id, data)
- deleteSelection(id)
- submitSelection(id)
- matchSelection(id)

### files.js
- uploadFile(formData)
- listFiles(bucket, path, relatedId)
- createFolder(bucket, relatedId, folderPath)
- deleteFile(id)
- deleteFolder(bucket, relatedId, folderPath)
- downloadFile(id)

---

## 关键业务流程

### 1. 项目选择计划锁定库存流程
1. 创建项目
2. 创建选择计划（关联项目）
3. 添加零件到计划
4. 提交选择计划
5. 入库操作（增加库存）
6. 锁定库存（关联项目）

### 2. 文件管理流程
1. 进入项目详情
2. 浏览多级目录
3. 上传文件到指定路径
4. 下载或删除文件

---

## 数据模型

### Part
```
{
  id, name, model, description, manufacturer, brand, category,
  tags, attachments, specTemplateId, specs,
  totalQty, availableQty, lockedQty,
  createdAt, updatedAt
}
```

### User
```
{
  id, username, displayName, email, role (admin/warehouse/user),
  createdAt
}
```

### ProjectNode
```
{
  id, parentId, type (folder/project), name, status,
  createdAt, updatedAt
}
```

### SelectionPlan
```
{
  id, projectId, name, description,
  status (draft/submitted/approved),
  items: [{ selectedPartId, requiredQty, filterCriteria }],
  createdAt, updatedAt
}
```

### StockTransaction
```
{
  id, partId, type (INBOUND/OUTBOUND/LOCK/UNLOCK/RETURN),
  quantity, operatorId, projectId, recipientId, recipientName,
  note, createdAt
}
```

### FileMetadata
```
{
  id, fileName, fileSize, mimeType, bucket, objectKey,
  fileType (PART/PROJECT/TEMPLATE/APPROVAL/SYSTEM),
  relatedId, uploadedBy, uploadedAt, description, tags,
  isDeleted, createdAt, updatedAt
}
```

---

## 权限矩阵

| 功能 | admin | warehouse | user |
|------|-------|-----------|------|
| Parts CRUD | ✅ | ✅ | ❌ |
| Stock Inbound | ✅ | ✅ | ❌ |
| Stock Outbound | ✅ | ✅ | ❌ |
| Stock Lock | ✅ | ✅ | ❌ |
| Stock Unlock | ✅ | ✅ | ❌ |
| Stock Return | ✅ | ✅ | ✅ |
| View Transactions | ✅ | ✅ | ✅ |
| Projects CRUD | ✅ | ❌ | ❌ |
| Selections CRUD | ✅ | ✅ | ✅ |
| File Upload | ✅ | ✅ | ❌ |
| File Download | ✅ | ✅ | ✅ |
| File Delete | ✅ | ✅ | ❌ |

