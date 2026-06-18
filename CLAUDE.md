# PartSelectionSystem

> 本文件为 Claude Code 工作指引，项目根目录 

## 🚀 快速启动

```bash
# 后端
cd InventorySystem/InventorySystem.API && dotnet run
# Swagger: http://localhost:5000/swagger
# MongoDB: mongodb://localhost:27017

# 前端
cd inventory-frontend && npm run dev
```

---

## 📋 项目背景

**系统定位**：工业机械设备设计方案管理系统

**目标用户**：技术员（负责技术方案设计与项目管理）

### 核心业务场景

#### 1. 项目管理
- 技术员根据技术方案建立项目
- 项目包含：数模、图纸、技术文档、说明文档等
- 项目以文件夹结构组织，支持工作区初始化

#### 2. 文档审批
- 项目文件需走审批流程
- 支持多级审批、审批人指定、条件分支
- 审批时可填写表单、附加审批意见

#### 3. 配件选型（核心功能）
- 根据技术方案选择所需配件
- 配件信息：**品牌、厂商、规格参数**
- 配件与库存关联，显示可用数量

#### 4. 库存管理
- 配件库存：总数(TotalQty)、占用(LockedQty)、可用(AvailableQty)
- 操作：入库、出库、锁定、解锁、退还
- 库存不足时触发采买流程（待扩展）

### 业务流程图

```
技术员创建项目
      ↓
上传图纸/文档 ←→ 项目工作区
      ↓
发起审批流程 → 审批人审批 → 通过/驳回
      ↓
配件选型（选择配件、查看库存）
      ↓
锁定配件（技术方案锁定库存）
      ↓
库存不足 → 触发采买（待实现）
```

---

## 🏗️ 架构

三层清洁架构：
- **Core** — 模型 + 接口，无业务逻辑
- **Infrastructure** — 实现 Core 接口，`MongoRepository<T>` 基类
- **API** — 控制器，DI 配置

依赖方向：API → Infrastructure → Core

---

## 后端核心概念

### Part 库存模型
| 字段 | 说明 |
|------|------|
| TotalQty | 总数 |
| LockedQty | 占用（已锁定） |
| AvailableQty | 可用（TotalQty - LockedQty） |

### 库存操作
| 操作 | TotalQty | LockedQty | AvailableQty |
|------|----------|-----------|--------------|
| Inbound（入库） | +qty | 0 | +qty |
| Outbound（出库） | -qty | 0 | -qty |
| Lock（锁定） | 0 | +qty | -qty |
| Unlock（解锁） | 0 | -qty | +qty |
| Return（退还） | +qty | 0 | +qty |

### 认证
- JWT Bearer Token
- 角色：`admin` / `warehouse` / `user`
- 默认账号：`admin` / `admin123`

### 配件分类与规格模板关联

**设计原则**：
- **数据冗余**：在 PartCategory 存储 SpecParams，减少查询索引
- **子级覆盖父级**：多级分类中，子分类规格覆盖父级同名参数
- **级联更新**：模板更新时自动更新引用它的所有分类

**级联规则**：
| 操作 | 行为 |
|------|------|
| 创建分类 + 指定模板 | 自动复制模板的 ParamDefs 到分类的 SpecParams |
| 更新分类 + 改变模板 | 重新复制新模板的参数，或清空 |
| 更新模板 | 自动更新所有引用该模板的分类的 SpecParams |

---

## 前端规范 ⚠️

### 开发自检（每次交付前必做）⚠️

```
✅ 交付前检查清单：

代码完整性：
□ 能编译通过吗？（dotnet build / npm run build）
□ 符合项目目录结构规范吗？
□ 考虑了空数据情况吗？（列表为空、对象为 null）
□ 考虑了错误情况吗？（API 失败、网络异常）

API 调用：
□ 使用统一的 request 实例吗？
□ 正确使用 .data 获取数据吗？（不是 res，是 res.data）
□ 不存在两层 .data.data 吗？

功能验证：
□ 手动验证过功能吗？
□ 边界情况测试了吗？（空输入、极值）
```

### API 层（强制）

**所有 API 调用必须使用统一的 `request` 实例**

- 位置: `src/api/request.js`
- 禁止: 直接 import `axios` 或创建新实例
- 禁止: 修改 `axios.interceptors`

正确用法:
```javascript
import request from '@/api/request'
request.get('/xxx')
request.post('/xxx', data)
```

新增 API 模块:
```javascript
// src/api/xxx.js
import request from './request'
export function getXxx() {
  return request.get('/xxx')
}
```

### API 响应数据获取（强制）⚠️

**`request` 拦截器返回完整 axios 响应对象，调用方必须使用 `.data` 获取实际数据**

```javascript
// ✅ 正确用法
const res = await getSomeData()
const data = res.data           // 从 axios 响应中提取数据
const list = (await getList()).data || []  // 带默认值

// ❌ 错误用法 - 两层 data
const data = res.data.data      // res.data 已经是实际数据，再加 .data 变成 undefined

// ❌ 错误用法 - 没有 .data
const data = res                // res 是 axios 响应对象 {data: ..., status: ...}
```

**注意：** 此项目后端 API 返回的是裸数据 `{ token: "xxx" }`，不是 `{ data: { token: "xxx" } }` 包装格式。

**常见错误场景：**
```javascript
// loadXxx 函数中的错误模式
const loadUsers = async () => {
  const res = await getUsers()
  users.value = res      // ❌ 错误：res 是 axios 响应对象
  users.value = res.data // ✅ 正确：从响应中提取数据
}
```

### 目录结构
```
src/
├── api/           # API 模块（统一使用 request）
├── services/      # 业务服务
├── views/         # 页面组件
├── components/    # 公共组件
└── utils/         # 工具函数
```

---

## 开发流程

1. **Models** — `InventorySystem.Core/Models/`
2. **Interfaces** — `InventorySystem.Core/Interfaces/`
3. **Repositories** — `InventorySystem.Infrastructure/Repositories/`
4. **Services** — `InventorySystem.Infrastructure/Services/`
5. **Controllers** — `InventorySystem.API/Controllers/`
6. **DI Registration** — `Program.cs`

---

## 重要约束

- **库存更新**：必须通过 `IStockService`，禁止直接操作 repo
- **API 调用**：前端统一使用 `request` 实例
- **日志**：关键业务点添加日志，使用 Serilog
- **配置**：磁盘文件路径需在 `appsettings.json` 配置

---

## 需求提交

功能需求、UI 改进、Bug 修复走 `requirements-tracker` agent：
```
/requirements-tracker
```

或描述需求，Claude 会自动分析是否需要追踪。
