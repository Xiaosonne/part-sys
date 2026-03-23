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

## 架构

三层清洁架构：
- **Core** — 模型 + 接口，无业务逻辑
- **Infrastructure** — 实现 Core 接口，`MongoRepository<T>` 基类
- **API** — 控制器，DI 配置

依赖方向：API → Infrastructure → Core

## 后端核心概念

### Part 库存模型
| 字段 | 说明 |
|------|------|
| TotalQty | 总数 |
| LockedQty | 占用 |
| AvailableQty | 可用 |

### 库存操作
| 操作 | TotalQty | LockedQty | AvailableQty |
|------|----------|-----------|--------------|
| Inbound | +qty | 0 | +qty |
| Outbound | -qty | 0 | -qty |
| Lock | 0 | +qty | -qty |
| Unlock | 0 | -qty | +qty |
| Return | +qty | 0 | +qty |

### 认证
- JWT Bearer Token
- 角色：`admin` / `warehouse` / `user`
- 默认账号：`admin` / `admin123`

## 前端规范 ⚠️

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

## 开发流程

1. **Models** — `InventorySystem.Core/Models/`
2. **Interfaces** — `InventorySystem.Core/Interfaces/`
3. **Repositories** — `InventorySystem.Infrastructure/Repositories/`
4. **Services** — `InventorySystem.Infrastructure/Services/`
5. **Controllers** — `InventorySystem.API/Controllers/`
6. **DI Registration** — `Program.cs`

## 重要约束

- **库存更新**：必须通过 `IStockService`，禁止直接操作 repo
- **API 调用**：前端统一使用 `request` 实例
- **日志**：关键业务点添加日志，使用 Serilog
- **配置**：磁盘文件路径需在 `appsettings.json` 配置
