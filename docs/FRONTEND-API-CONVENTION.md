# 前端 API 调用规范

## 核心原则

`src/api/request.js` 的 axios 拦截器返回**完整 axios 响应对象**，调用方必须使用 `.data` 获取实际数据。

## 正确用法

```javascript
import { getUsers } from '@/api/users'

// 标准用法
const res = await getUsers()
const users = res.data

// 简化赋值
users.value = (await getUsers()).data

// 带默认值
const list = (await getUsers()).data || []
```

## 错误用法

```javascript
// ❌ 两层 data - 会导致数据为 undefined
const users = res.data.data

// ❌ 没有 .data - res 是 axios 响应对象，不是实际数据
const users = res  // { data: [...], status: 200, ... }

// ❌ 在条件判断中漏掉 .data
if (res) {  // 总是 true，因为 res 是对象
    users.value = res  // 错误
}
```

## 为什么后端返回的不是 { data: {...} } 包装格式

本项目后端 ASP.NET Core Controller 直接返回对象：

```csharp
// 后端代码
return Ok(new { token, user });  // 返回 { token: "...", user: {...} }
```

前端 axios 收到：
```javascript
{
  data: { token: "...", user: {...} },  // axios 自动包装
  status: 200,
  statusText: "OK",
  headers: {...},
  config: {...}
}
```

拦截器返回 `response`（不拆包），所以调用方用 `.data` 获取实际数据。

## 检查清单

新增 API 调用时，确保：

- [ ] 导入使用 `request` 实例，不是直接 import axios
- [ ] await 调用后使用 `.data` 获取数据
- [ ] 数组API考虑使用 `|| []` 提供默认值
- [ ] 避免 `res.data.data` 两层 data

## 相关文件

- `src/api/request.js` - axios 实例和拦截器配置
- `src/api/*.js` - 各模块 API 定义
- `CLAUDE.md` - 项目开发规范
