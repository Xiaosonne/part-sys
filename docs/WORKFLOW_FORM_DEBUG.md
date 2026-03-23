# 流程表单字段功能调试指南

## 功能概述

本次实现添加了流程表单字段功能，包括：
1. **流程创建时的表单字段** - 在启动流程时填写初始信息
2. **审批时的表单字段** - 在审批任务时填写审批信息
3. **流程详情显示** - 在流程详情页面显示所有节点提交的表单数据

## 实现细节

### 后端修改

#### 1. WorkflowDefinition 模型
- `WorkflowNode` 添加 `FormFields` 属性，包含该节点的表单字段定义
- `FormField` 模型定义表单字段的属性（类型、标签、必填等）

#### 2. WorkflowInstance 模型
- 添加 `FormData` 字典，存储流程启动时提交的表单数据

#### 3. WorkflowTask 模型
- `FormData` 字典存储审批时提交的表单数据

#### 4. WorkflowHistory 模型
- `FormData` 字典记录每个节点提交的表单数据

#### 5. API 端点修改
- `POST /api/workflows/instances` - 支持 `formData` 参数
- `POST /api/workflows/tasks/{id}/approve` - 支持 `formData` 参数
- `GET /api/workflows/tasks/pending` - 返回完整的 `WorkflowInstanceDetailDto`

### 前端修改

#### 1. StartWorkflow.vue
- 选择流程定义后，自动加载第一个节点的表单字段
- 动态渲染表单字段（支持 text、textarea、select、number、checkbox）
- 验证必填字段
- 提交时包含 `formData`

#### 2. PendingTasks.vue
- 打开审批对话框时，加载当前节点的表单字段
- 动态渲染审批表单字段
- 验证必填字段
- 提交审批时包含 `formData`

#### 3. WorkflowDetail.vue
- 在审批历史中显示每个节点提交的表单数据
- 以卡片形式展示表单数据

## 调试步骤

### 1. 启动后端服务
```bash
cd /e/PartSelectionSystem/InventorySystem/InventorySystem.API
dotnet run
```

### 2. 启动前端服务
```bash
cd /e/PartSelectionSystem/inventory-frontend
npm run dev
```

### 3. 创建测试流程定义

使用 Swagger UI 或 Postman 创建流程定义，包含表单字段：

```json
{
  "name": "项目审批流程",
  "description": "测试表单字段功能",
  "category": "project",
  "entityType": "Project",
  "nodes": [
    {
      "id": "start",
      "nodeType": "Start",
      "name": "开始",
      "formFields": [
        {
          "id": "field1",
          "fieldType": "text",
          "label": "项目编号",
          "key": "projectCode",
          "required": true,
          "placeholder": "输入项目编号"
        },
        {
          "id": "field2",
          "fieldType": "textarea",
          "label": "项目描述",
          "key": "projectDesc",
          "required": false,
          "placeholder": "输入项目描述"
        }
      ]
    },
    {
      "id": "approval1",
      "nodeType": "SingleApproval",
      "name": "部门经理审批",
      "approvers": ["user1"],
      "formFields": [
        {
          "id": "field3",
          "fieldType": "select",
          "label": "审批意见",
          "key": "approvalOpinion",
          "required": true,
          "options": ["同意", "不同意", "需要修改"]
        }
      ],
      "nextNodes": ["approval2"]
    },
    {
      "id": "approval2",
      "nodeType": "SingleApproval",
      "name": "总经理审批",
      "approvers": ["admin"],
      "formFields": [],
      "nextNodes": ["end"]
    },
    {
      "id": "end",
      "nodeType": "End",
      "name": "结束"
    }
  ]
}
```

### 4. 测试流程启动

1. 登录前端应用
2. 导航到"发起新流程"页面
3. 选择刚创建的流程定义
4. 填写流程启动表单字段（项目编号、项目描述）
5. 选择项目和文件
6. 点击"启动流程"

**验证点**：
- 表单字段正确显示
- 必填字段验证生效
- 表单数据成功提交

### 5. 测试审批流程

1. 以 user1 身份登录
2. 导航到"待办任务"页面
3. 点击任务的"通过"按钮
4. 填写审批表单字段（审批意见）
5. 点击"通过"

**验证点**：
- 审批表单字段正确显示
- 必填字段验证生效
- 审批数据成功提交

### 6. 查看流程详情

1. 导航到"我发起的流程"页面
2. 点击流程名称查看详情
3. 在"审批历史"中查看表单数据

**验证点**：
- 流程启动时的表单数据显示
- 每个审批节点的表单数据显示
- 表单数据格式正确

## 支持的表单字段类型

| 类型 | 说明 | 示例 |
|------|------|------|
| text | 单行文本 | 项目编号、部门名称 |
| textarea | 多行文本 | 项目描述、审批备注 |
| select | 下拉选择 | 审批意见、优先级 |
| number | 数字输入 | 预算金额、工期 |
| checkbox | 复选框 | 是否同意条款 |

## 常见问题

### Q1: 表单字段不显示
**A**: 检查流程定义中是否正确配置了 `formFields`，确保 `fieldType` 是支持的类型。

### Q2: 必填字段验证不生效
**A**: 检查前端代码中的验证逻辑，确保 `field.required` 为 `true`。

### Q3: 表单数据未保存
**A**: 检查后端 API 是否正确接收 `formData` 参数，查看数据库中是否保存了数据。

### Q4: 审批历史中看不到表单数据
**A**: 确保 `WorkflowHistory` 中正确保存了 `FormData`，检查前端是否正确显示。

## 数据流向

```
启动流程:
用户填写表单 → StartWorkflow.vue → API → WorkflowService → WorkflowInstance.FormData

审批流程:
用户填写表单 → PendingTasks.vue → API → WorkflowTaskService → WorkflowTask.FormData + WorkflowHistory.FormData

查看详情:
WorkflowDetail.vue → API → WorkflowHistory → 显示表单数据
```

## 测试用例

### 用例1: 完整流程测试
1. 创建包含表单字段的流程定义
2. 启动流程，填写表单数据
3. 审批流程，填写审批表单数据
4. 查看流程详情，验证所有表单数据

### 用例2: 必填字段验证
1. 创建包含必填字段的流程定义
2. 尝试不填写必填字段启动流程
3. 验证错误提示

### 用例3: 多种字段类型
1. 创建包含各种字段类型的流程定义
2. 填写各种类型的表单数据
3. 验证数据正确保存和显示

## 后续改进

1. 支持条件显示（DisplayCondition）
2. 支持自定义验证规则
3. 支持文件上传字段
4. 支持动态选项加载
5. 支持表单数据导出
