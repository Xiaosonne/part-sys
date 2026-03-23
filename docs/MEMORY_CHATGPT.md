# ChatGPT Project Memory

## Systems Overview
- **Frontend**: `inventory-frontend` (Vue 3 + Vite + Element Plus) lives at `C:\Users\xiaos\Desktop\demo\inventory-frontend`, points to `http://localhost:5128/api`, uses JWT in `localStorage`, and exposes Login/Layout/Parts/Stock/Transactions/Projects/Selections pages. Run with `npm run dev` on port 5173.
- **Backend**: `InventorySystem` (.NET 8, ASP.NET Core Web API) in `InventorySystem/InventorySystem.API`, backed by MongoDB `mongodb://admin:ganwei.123@211.159.151.178:21117`. Run with `dotnet run`; Swagger served at `http://localhost:5128/swagger`.
- **Key roles**: `admin` (full + user mgmt), `warehouse` (parts CRUD, stock ops, upload files), `user` (read inventory, manage projects/selections, download files). Default admin creds: `admin / admin123`.

## Domain Building Blocks
- **Inventory Core Models**: Part (spec + stock counts), User, ProjectNode (folder/project tree), SelectionPlan (draft→submitted→approved), StockTransaction (INBOUND/OUTBOUND/LOCK/UNLOCK/RETURN), SpecTemplate, FileMetadata (bucket, object, fileType, relatedId, uploader, soft-delete flags).
- **Stock math**: inbound adds to `TotalQty` & `AvailableQty`; outbound subtracts both; lock adjusts `LockedQty`/`AvailableQty`; unlock reverses; return acts like inbound.
- **APIs**: Auth, Parts, Stock, Projects, Selections (plus submit/match), SpecTemplates, Files (upload/list/download/delete + part/project scopes). `FilesController` enriches listings with metadata IDs for download streams.

## Workflow Platform Highlights
- Built on **workflow-core** with Mongo persistence. Entities: WorkflowDefinition/Node/FormField, WorkflowInstance, WorkflowTask, WorkflowHistory.
- **Node types**: start, single approval, multi-approver AND/OR, conditional branches, merge nodes, end.
- **Form system**: each node can define dynamic fields (text/textarea/select/number/checkbox now; future goals include file uploads, conditional display, custom validators). Data flows through `WorkflowInstance.FormData`, `WorkflowTask.FormData`, and history records.
- **File integration**: flows can pick project files (tree fed by `/api/workflows/projects/{projectId}/files`) and approval tasks can upload files into the `approvals` bucket.
- **Front-end surfaces**: WorkflowDesigner.vue (drag/drop + field config), StartWorkflow.vue (definition selection, file pick, dynamic form), PendingTasks.vue (task list with approval dialog + project snapshot), MyWorkflows.vue, WorkflowDetail.vue (progress + history w/ form data).
- **Status** (2026‑03‑20): core engine, designer, multi-role approvals, conditional routing, project-file linkage, Chinese workspace names, and Serilog logging completed. Outstanding: site notifications, branch-condition UI polish, workflow template library, end-to-end/perf tests.

## Requirements Rituals (`docs/REQUIREMENTS-STANDARD.md`)
- Always capture: `【需求类型】` (功能/UI改进/Bug/性能), `【功能模块】`, bullet `【需求描述】` (state *what*, not *how*), measurable `【验收标准】`, `【优先级】` (高/中/低), and `【相关文件】` (code paths + APIs).
- Acceptance templates define mandatory validation, error messaging, boundary handling, tests, and code quality checks. UI work must ensure layout clarity, feedback, consistent styling, responsive behavior, and acceptable performance.
- Workflow: submit → analyze → implement → accept → update docs (esp. MEMORY.md). Startup checklist: read REQUIREMENTS-STANDARD, MEMORY.md, CLAUDE.md, confirm priority + endpoints before coding.

## Workspace & File Management
- Project creation reads `docs/project-workspace-structure.json` to build folder trees under `{projectId}/...`; config currently defines docs/models/drawings (with sensor_box, equipment_lubrication, etc.), parts_config (outsourced/standard/mechanical/electrical/shipping_docs), software, materials.
- WorkspaceInitializer (`InventorySystem.Infrastructure.Services.WorkspaceInitializer`) drives folder creation; ProjectsController exposes a `reinitialize-workspace` route (admin-only) that also refreshes stored display metadata for proper中文 labels.
- File storage buckets under `InventorySystem.API/wwwroot/files/`: `parts`, `projects`, `templates`, `approvals`, `system`. Multi-level folder CRUD, breadcrumbs, upload/delete, and metadata-aware download pipelines already in place.

## Project List Detail Feature (plan reference)
- Projects.vue must use Element Plus expand rows to reveal per-project SelectionPlans, including part name, `requiredQty`, and live stock numbers (`totalQty`, `lockedQty`, `availableQty`). Data loads on demand, caches per project, and hides expanders for folder nodes or empty selections. APIs: `getSelections(projectId)` + `getParts()`.

## Testing & Operational Notes
- **E2E script**: `inventory-frontend/test-project-selection-lock.js` (Node) covers project→selection→inventory lock flow. Reminders: use returned `id` (not `_id`), call `response.status()`, run inbound before lock, lock references `selectedPartId`.
- **Running services**: start backend first (`dotnet run`), then frontend (`npm run dev`). API base/ports already wired in env.
- **Logging**: Serilog writes per-process rolling files (`app-{processId}-YYYY-MM-DD.log`) with 30-day retention; process IDs recorded under `process.pid`.

## Frontend API Call Convention (Critical - Fixed 2026-03-21)
**`src/api/request.js` 拦截器返回完整 axios 响应对象，调用方必须使用 `.data` 获取实际数据**

```javascript
// ✅ 正确
const res = await getUsers()
users.value = res.data

// ❌ 错误 - 两层 data (曾导致严重 Bug)
users.value = res.data.data   // res.data 已经是实际数据

// ❌ 错误 - 没有 .data
users.value = res            // res 是 axios 响应对象 {data: ..., status: ...}
```

**后端返回裸数据 `{ token: "xxx" }`，不是 `{ data: { token: "xxx" } }` 包装格式。**

## Immediate Watch Items
1. Finish internal notification system + workflow template library.
2. Expand workflow condition editor for visual branch rules.
3. Keep MEMORY.md + this file updated when workflows, workspace schemas, or requirement standards evolve.
