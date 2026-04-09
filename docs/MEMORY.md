# Project Memory

## Frontend Status
✅ **inventory-frontend** (C:\Users\xiaos\Desktop\demo\inventory-frontend)
- Vue 3 + Vite + Element Plus frontend completed
- All 7 pages: Login, Layout, Parts, Stock, Transactions, Projects, Selections
- API: http://localhost:5128/api
- JWT auth with localStorage
- Role-based menu (admin/warehouse/user)
- ✅ Login test passed (admin/admin123)
- Run: `npm run dev` (port 5173)

## InventorySystem Backend

### Stack
- .NET 8, ASP.NET Core Web API
- MongoDB (remote): mongodb://admin:ganwei.123@211.159.151.178:21117
- JWT auth, BCrypt password hashing
- Swagger via Swashbuckle.AspNetCore

### Architecture
Three-layer clean architecture:
- **Core** — Models + Interfaces only
- **Infrastructure** — MongoRepository<T> base + repos + StockService
- **API** — Controllers, DI in Program.cs, JWT config, admin seed

### Models
**Part**: Name, Model, Description, Manufacturer, Brand, Category, Tags, Attachments, SpecTemplateId, Specs, TotalQty, AvailableQty, LockedQty, CreatedAt, UpdatedAt

**User**: Username, PasswordHash, DisplayName, Email, Role (admin/warehouse/user)

**ProjectNode**: ParentId, Type (folder/project), Name, Status, CreatedAt

**SelectionPlan**: ProjectId, Name, Description, Status (draft/submitted/approved), Items, CreatedAt

**StockTransaction**: PartId, Type (INBOUND/OUTBOUND/LOCK/UNLOCK/RETURN), Quantity, OperatorId, ProjectId, RecipientId, RecipientName, Note, CreatedAt

**SpecTemplate**: Category, ParamDefs (Key, Label, Unit, DataType)

**FileMetadata** ✅ NEW: Id, FileName, FileSize, MimeType, Bucket, ObjectKey, FileType (PART/PROJECT/TEMPLATE/APPROVAL/SYSTEM), RelatedId, UploadedBy, UploadedAt, Description, Tags, IsDeleted, CreatedAt, UpdatedAt

### Stock Operations
| Operation | TotalQty | LockedQty | AvailableQty |
|-----------|----------|-----------|--------------|
| Inbound   | +qty     | 0         | +qty         |
| Outbound  | -qty     | 0         | -qty         |
| Lock      | 0        | +qty      | -qty         |
| Unlock    | 0        | -qty      | +qty         |
| Return    | +qty     | 0         | +qty         |

### API Endpoints
- POST /api/auth/login, POST /api/auth/register (admin), GET /api/auth/me
- GET/POST/PUT/DELETE /api/parts
- POST /api/stock/inbound|outbound|lock|unlock|return, GET /api/stock/transactions
- GET/POST/PUT/DELETE /api/projects
- GET/POST/PUT/DELETE /api/selections, POST /api/selections/{id}/submit, GET /api/selections/{id}/match
- GET/POST/PUT/DELETE /api/templates
- **POST /api/files/upload** ✅ NEW: Upload file with FormData (file, bucket, relatedId, fileType, description)
- **GET /api/files/{id}** ✅ NEW: Get file metadata
- **GET /api/files/{id}/download** ✅ NEW: Download file
- **DELETE /api/files/{id}** ✅ NEW: Delete file (soft delete)
- **GET /api/files/part/{partId}** ✅ NEW: Get all files for part
- **GET /api/files/project/{projectId}** ✅ NEW: Get all files for project

### Controllers
- AuthController: Login, Register, GetMe
- PartsController: CRUD operations
- StockController: Inbound, Outbound, Lock, Unlock, Return, GetTransactions
- ProjectsController: CRUD with hierarchy
- SelectionController: CRUD + Submit + Match
- SpecTemplatesController: CRUD by category
- **FilesController** ✅ NEW: Upload, GetMetadata, Download, Delete, GetPartFiles, GetProjectFiles

### Default Admin
- Username: admin / Password: admin123

### Build & Run
- Build: `cd InventorySystem && dotnet build`
- Run: `cd InventorySystem/InventorySystem.API && dotnet run`
- Swagger: http://localhost:5128/swagger
- DB: inventory_db

### Roles & Permissions
- **admin**: All operations + user management
- **warehouse**: Parts CRUD, stock operations (inbound/outbound/lock/unlock), file upload
- **user**: Read inventory, return, project + selection management, file download

## File Management Module - Phase 1 & 3 ✅ COMPLETED

### Implementation Details
**Storage**: Local file system at `InventorySystem.API/wwwroot/files/`
- Bucket structure: `files/{bucket-name}/`
  - `files/parts/` - Part files
  - `files/projects/` - Project files
  - `files/templates/` - File directory templates
  - `files/approvals/` - Approval files (reserved)
  - `files/system/` - System files (reserved)
- **Multi-level directory support**: Folders can be created at any path level

**Backend Components**:
1. ✅ FileMetadata model (Core/Models/FileMetadata.cs)
2. ✅ IFileStorageService interface with multi-level support (Core/Interfaces/IFileStorageService.cs)
3. ✅ FileItem class with optional Id field for metadata lookup
4. ✅ IFileMetadataRepository interface (Core/Interfaces/IFileMetadataRepository.cs)
5. ✅ FileMetadataRepository implementation (Infrastructure/Repositories/FileMetadataRepository.cs)
6. ✅ LocalFileStorageService with CreateFolderAsync, ListFilesAsync (Infrastructure/Services/LocalFileStorageService.cs)
7. ✅ FilesController with ListFiles, CreateFolder endpoints (API/Controllers/FilesController.cs)
8. ✅ ListFiles endpoint enriches FileItem with metadata IDs for downloads
9. ✅ DI registration in Program.cs

**Frontend Components**:
1. ✅ ProjectDetail.vue with multi-level directory UI
2. ✅ Breadcrumb navigation for path tracking
3. ✅ Folder creation dialog
4. ✅ File/folder display with icons (📁 for folders, 📄 for files)
5. ✅ Clickable folders for navigation
6. ✅ File upload with path support
7. ✅ File deletion functionality
8. ✅ files.js API module with listFiles, createFolder functions

**Key Features**:
- Multi-level directory structure with folder creation
- File upload with metadata storage
- File download with stream support
- Soft delete (IsDeleted flag)
- File association with Parts/Projects via RelatedId
- File type categorization (PART, PROJECT, TEMPLATE, APPROVAL, SYSTEM)
- User tracking (UploadedBy)
- 100MB file size limit
- Role-based access control (admin/warehouse can upload, all authenticated users can download)
- Breadcrumb navigation for easy path traversal
- File metadata ID enrichment for download functionality

**Test Results**: ✅ Multi-level directory support implemented
- Backend: Supports folder creation and file listing with path parameters
- Frontend: Breadcrumb navigation, folder creation, file upload to subfolders
- File download: FileItem now includes metadata ID for proper download support

## Test Scripts

### test-project-selection-lock.js ✅
**Location**: `C:\Users\xiaos\Desktop\demo\inventory-frontend\test-project-selection-lock.js`

**Purpose**: End-to-end test for complete business flow: Project → Selection → Inventory Lock

**Flow**:
1. Login (admin/admin123)
2. Get parts list (API returns `id` field, not `_id`)
3. Create project via API
4. Create selection plan with items via API
5. Submit selection plan
6. Add inventory (inbound) - **CRITICAL**: Must add inventory before locking
7. Lock inventory for each selected part with projectId association
8. Verify LockedQty increased correctly

**Key Findings**:
- API uses `id` field instead of `_id` for MongoDB IDs
- Must call `status()` as function, not property
- Lock operation requires available inventory (fails with 400 if insufficient)
- Inbound operation must be done before lock to populate availableQty
- SelectionPlan items use `selectedPartId` field

**Test Results**: ✅ All passed
- Arduino Uno: locked 5 units
- Raspberry Pi 4: locked 3 units
- ESP32: locked 10 units

**Run**: `node test-project-selection-lock.js`

# currentDate
Today's date is 2026-04-08.

## 库存管理增强 - 进行中 (2026-04-08)

**设计文档**: `docs/INVENTORY-MANAGEMENT-ENHANCEMENT.md`

### 已完成
- [x] 库存页面组件化拆分（Tab 切换）
- [x] 库存总览/流水/锁定状态 页面
- [x] 入库表单：普通入库/采购入库/归还入库
- [x] 出库表单：项目→自动加载选型单
- [x] 锁定/解锁表单
- [x] Ant Design 风格统一

### 待完成
- [ ] 后端：多配件采购任务支持（Items 列表）
- [ ] 回归测试
