<template>
  <div class="workflow-manager-container">
    <!-- 左侧：流程定义列表 -->
    <div class="left-panel">
      <div class="panel-header">
        <h3>流程定义</h3>
        <div class="header-actions">
          <el-button type="primary" size="small" @click="openDesigner">
            设计流程
          </el-button>
          <el-button size="small" @click="handleDelete" :disabled="!selectedDefinition">
            删除
          </el-button>
          <el-button size="small" @click="handleExport" :disabled="!selectedDefinition">
            导出
          </el-button>
        </div>
        <el-input
          v-model="searchText"
          placeholder="搜索流程..."
          clearable
          size="small"
          style="margin-top: 8px;"
        />
      </div>
      <div class="definition-list">
        <div
          v-for="def in filteredDefinitions"
          :key="def.id"
          class="definition-item"
          :class="{ active: selectedDefinition?.id === def.id }"
          @click="selectDefinition(def)"
        >
          <div class="def-name">{{ def.name }}</div>
          <div class="def-category">{{ def.category || '未分类' }}</div>
        </div>
        <div v-if="filteredDefinitions.length === 0" class="empty-list">
          暂无流程定义
        </div>
      </div>
    </div>

    <!-- 右侧：流程详情预览 -->
    <div class="right-panel">
      <div v-if="!selectedDefinition" class="empty-detail">
        <el-empty description="请从左侧选择一个流程查看详情" />
      </div>
      <div v-else class="workflow-detail">
        <div class="detail-header">
          <h3>{{ selectedDefinition.name }}</h3>
          <p class="form-desc">{{ selectedDefinition.description }}</p>
        </div>

        <el-tabs v-model="activeTab">
          <el-tab-pane label="流程节点" name="nodes">
            <div class="nodes-preview">
              <div v-for="node in selectedDefinition.nodes" :key="node.id" class="node-item">
                <el-tag :type="getNodeTagType(node.nodeType)">{{ getNodeTypeLabel(node.nodeType) }}</el-tag>
                <span class="node-name">{{ node.name }}</span>
                <span v-if="node.approvers && node.approvers.length > 0" class="node-approvers">
                  审批人: {{ getApproverNames(node.approvers) }}
                </span>
              </div>
            </div>
          </el-tab-pane>
        </el-tabs>
      </div>
    </div>

    <!-- 设计流程对话框 -->
    <el-dialog v-model="showDesignerDialog" title="流程设计器" width="90%" top="5vh">
      <div class="designer-container">
        <el-row :gutter="20">
          <!-- 左侧工具栏 -->
          <el-col :span="4">
            <div class="toolbar">
              <h4>节点类型</h4>
              <div class="node-types">
                <div class="node-item" draggable="true" @dragstart="dragStart('SingleApproval')">
                  <el-icon><User /></el-icon>
                  <span>单人审批</span>
                </div>
                <div class="node-item" draggable="true" @dragstart="dragStart('MultiApprovalAnd')">
                  <el-icon><DocumentCopy /></el-icon>
                  <span>多人会签</span>
                </div>
                <div class="node-item" draggable="true" @dragstart="dragStart('MultiApprovalOr')">
                  <el-icon><UserFilled /></el-icon>
                  <span>多人或签</span>
                </div>
              </div>
            </div>
          </el-col>

          <!-- 中间画布 -->
          <el-col :span="12">
            <div class="canvas-container" @drop="handleDrop" @dragover.prevent>
              <svg ref="canvas" class="canvas">
                <defs>
                  <marker id="arrowhead-designer" markerWidth="10" markerHeight="10" refX="9" refY="3" orient="auto">
                    <polygon points="0 0, 10 3, 0 6" fill="#666" />
                  </marker>
                </defs>
                <line v-for="line in getLines()" :key="`${line.from}-${line.to}`" :x1="line.x1" :y1="line.y1" :x2="line.x2" :y2="line.y2" stroke="#666" stroke-width="2" marker-end="url(#arrowhead-designer)" />
              </svg>
              <div class="nodes-container">
                <div
                  v-for="node in designerNodes"
                  :key="node.id"
                  class="node"
                  :style="{ left: node.x + 'px', top: node.y + 'px' }"
                  @click="selectNode(node)"
                  @mousedown="startNodeDrag($event, node)"
                  :class="{ selected: selectedNode?.id === node.id }"
                >
                  <div class="node-content">
                    <div class="node-type">{{ getNodeTypeLabel(node.nodeType) }}</div>
                    <div class="node-name">{{ node.name }}</div>
                  </div>
                </div>
              </div>
            </div>
          </el-col>

          <!-- 右侧配置面板 -->
          <el-col :span="8">
            <div class="config-panel">
              <h4>流程信息</h4>
              <el-form :model="definitionForm" label-width="80px" size="small">
                <el-form-item label="流程名称" required>
                  <el-input v-model="definitionForm.name" placeholder="输入流程名称" />
                </el-form-item>
                <el-form-item label="流程描述">
                  <el-input v-model="definitionForm.description" type="textarea" />
                </el-form-item>
                <el-form-item label="流程分类">
                  <el-input v-model="definitionForm.category" placeholder="输入分类" />
                </el-form-item>
              </el-form>

              <h4 v-if="selectedNode">节点配置</h4>
              <el-form v-if="selectedNode" :model="selectedNode" label-width="80px" size="small">
                <el-form-item label="节点名称">
                  <el-input v-model="selectedNode.name" />
                </el-form-item>
                <el-form-item label="节点类型">
                  <el-select v-model="selectedNode.nodeType" disabled>
                    <el-option label="单人审批" value="SingleApproval" />
                    <el-option label="多人会签" value="MultiApprovalAnd" />
                    <el-option label="多人或签" value="MultiApprovalOr" />
                  </el-select>
                </el-form-item>
                <el-form-item v-if="selectedNode.nodeType !== 'Start' && selectedNode.nodeType !== 'End'" label="审批人">
                  <el-select v-model="selectedNode.approvers" multiple filterable placeholder="选择审批人">
                    <el-option v-for="u in users" :key="u.id" :label="u.username" :value="u.id" />
                  </el-select>
                </el-form-item>
                <el-form-item v-if="selectedNode.nodeType !== 'Start' && selectedNode.nodeType !== 'End'" label="超时(分钟)">
                  <el-input-number v-model="selectedNode.timeoutMinutes" :min="0" />
                </el-form-item>
                <el-form-item v-if="selectedNode.nodeType !== 'End'" label="下一节点">
                  <el-select v-model="selectedNode.nextNodes" multiple placeholder="选择下一节点">
                    <el-option v-for="n in designerNodes.filter(nd => nd.id !== selectedNode.id)" :key="n.id" :label="n.name" :value="n.id" />
                  </el-select>
                </el-form-item>

                <el-divider />
                <div style="margin-bottom: 10px;">
                  <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 10px;">
                    <span style="font-weight: bold; font-size: 12px;">表单字段</span>
                    <el-button type="primary" size="small" @click="addFormField">添加字段</el-button>
                  </div>
                  <div v-if="selectedNode.formFields && selectedNode.formFields.length > 0">
                    <div v-for="(field, idx) in selectedNode.formFields" :key="idx" class="field-item">
                      <div style="flex: 1;">
                        <div><strong>{{ field.label }}</strong></div>
                        <div style="color: #666; font-size: 11px;">类型: {{ field.fieldType }} {{ field.required ? '(必填)' : '(可选)' }}</div>
                      </div>
                      <div style="display: flex; gap: 5px;">
                        <el-button type="primary" link size="small" @click="editFormField(idx)">编辑</el-button>
                        <el-button type="danger" link size="small" @click="deleteFormField(idx)">删除</el-button>
                      </div>
                    </div>
                  </div>
                  <div v-else style="color: #999; font-size: 12px; padding: 10px; text-align: center;">暂无表单字段</div>
                </div>

                <el-button type="danger" size="small" @click="deleteNode">删除节点</el-button>
              </el-form>
              <div v-else-if="!selectedNode" class="empty-state">
                <p>选择节点进行配置</p>
              </div>
            </div>
          </el-col>
        </el-row>
      </div>
      <template #footer>
        <el-button @click="showDesignerDialog = false">取消</el-button>
        <el-button type="primary" @click="saveDefinition">保存</el-button>
      </template>
    </el-dialog>

    <!-- 表单字段编辑对话框 -->
    <el-dialog v-model="showFieldDialog" :title="editingFieldIndex !== null ? '编辑表单字段' : '添加表单字段'" width="600px">
      <el-form :model="currentField" label-width="100px">
        <el-form-item label="字段标签" required>
          <el-input v-model="currentField.label" placeholder="如：审批意见" />
        </el-form-item>
        <el-form-item label="字段Key" required>
          <el-input v-model="currentField.key" placeholder="如：approvalOpinion" />
        </el-form-item>
        <el-form-item label="字段类型" required>
          <el-select v-model="currentField.fieldType">
            <el-option label="单行文本" value="text" />
            <el-option label="多行文本" value="textarea" />
            <el-option label="下拉选择" value="select" />
            <el-option label="数字" value="number" />
            <el-option label="复选框" value="checkbox" />
            <el-option label="项目选择" value="project" />
            <el-option label="项目文件选择" value="projectFile" />
          </el-select>
        </el-form-item>

        <template v-if="currentField.fieldType === 'project'">
          <el-form-item label="实体类型">
            <el-select v-model="currentField.entityType" placeholder="选择实体类型">
              <el-option label="项目" value="Project" />
              <el-option label="文件夹" value="Folder" />
              <el-option label="配件" value="Part" />
            </el-select>
          </el-form-item>
        </template>

        <template v-if="currentField.fieldType === 'projectFile'">
          <el-form-item label="关联项目字段">
            <el-select v-model="currentField.entitySourceKey" placeholder="选择关联的项目字段">
              <el-option v-for="f in availableProjectFields" :key="f.key" :label="`${f.label} (${f.key})`" :value="f.key" />
            </el-select>
          </el-form-item>
          <el-form-item label="允许的文件类型">
            <el-input v-model="currentField.allowedFileTypesText" type="textarea" rows="2" placeholder="如：.pdf,.docx,.dwg" />
          </el-form-item>
        </template>

        <el-form-item v-if="currentField.fieldType !== 'project' && currentField.fieldType !== 'projectFile'" label="占位符">
          <el-input v-model="currentField.placeholder" placeholder="输入框提示文本" />
        </el-form-item>

        <el-form-item v-if="currentField.fieldType === 'select'" label="选项" required>
          <el-input v-model="currentField.optionsText" type="textarea" rows="3" placeholder="每行一个选项" />
        </el-form-item>

        <el-form-item label="必填">
          <el-checkbox v-model="currentField.required" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showFieldDialog = false">取消</el-button>
        <el-button type="primary" @click="saveFormField">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { CircleCheck, User, DocumentCopy, UserFilled, CircleCloseFilled } from '@element-plus/icons-vue'
import { getWorkflowDefinitions, getWorkflowDefinition, createWorkflowDefinition, deleteWorkflowDefinition } from '../services/workflowApi'
import { getUsers } from '../api/users'

// 流程定义列表
const definitions = ref([])
const searchText = ref('')
const selectedDefinition = ref(null)
const activeTab = ref('nodes')
const showDesignerDialog = ref(false)
const showFieldDialog = ref(false)

// 画布相关
const designerNodes = ref([])
const selectedNode = ref(null)
const draggedNodeType = ref(null)
const nodeCounter = ref(0)
const draggingNode = ref(null)
const dragOffset = ref({ x: 0, y: 0 })
const users = ref([])
const editingFieldIndex = ref(null)

const currentField = ref({
  id: '',
  label: '',
  key: '',
  fieldType: 'text',
  placeholder: '',
  required: false,
  optionsText: '',
  options: [],
  entityType: 'Project',
  entitySourceKey: '',
  allowedFileTypesText: ''
})

const definitionForm = ref({
  name: '',
  description: '',
  category: ''
})

// 可关联的项目字段
const availableProjectFields = computed(() => {
  return designerNodes.value.flatMap(n => n.formFields || []).filter(f => f.fieldType === 'project')
})

// 过滤后的流程定义
const filteredDefinitions = computed(() => {
  if (!searchText.value) return definitions.value
  const keyword = searchText.value.toLowerCase()
  return definitions.value.filter(def =>
    def.name.toLowerCase().includes(keyword) ||
    def.description?.toLowerCase().includes(keyword)
  )
})

// 获取用户名称
const getApproverNames = (approverIds) => {
  if (!Array.isArray(approverIds)) return ''
  return approverIds.map(id => {
    const user = users.value.find(u => u.id === id)
    return user ? user.username : id
  }).join(', ')
}

const getNodeTypeLabel = (type) => {
  const labels = {
    Start: '开始',
    SingleApproval: '单人审批',
    MultiApprovalAnd: '多人会签',
    MultiApprovalOr: '多人或签',
    End: '结束'
  }
  return labels[type] || type
}

const getNodeTagType = (type) => {
  const types = {
    Start: 'success',
    SingleApproval: 'warning',
    MultiApprovalAnd: 'danger',
    MultiApprovalOr: 'danger',
    End: 'info'
  }
  return types[type] || ''
}

onMounted(async () => {
  await loadDefinitions()
  try {
    const res = await getUsers()
    users.value = res.data || []
  } catch (e) {
    console.error('Failed to load users', e)
  }
})

const loadDefinitions = async () => {
  try {
    definitions.value = (await getWorkflowDefinitions()).data || []
  } catch (error) {
    ElMessage.error('加载流程列表失败: ' + error.message)
  }
}

const selectDefinition = async (def) => {
  try {
    const fullDef = await getWorkflowDefinition(def.id)
    selectedDefinition.value = fullDef.data
  } catch (error) {
    ElMessage.error('加载流程详情失败: ' + error.message)
  }
}

// 打开设计器
const openDesigner = () => {
  definitionForm.value = {
    name: '',
    description: '',
    category: ''
  }
  // 自动创建开始节点
  const startNode = {
    id: `node_0`,
    nodeType: 'Start',
    name: '开始',
    x: 100,
    y: 50,
    approvers: [],
    timeoutMinutes: 0,
    nextNodes: [],
    formFields: []
  }
  designerNodes.value = [startNode]
  nodeCounter.value = 1
  selectedNode.value = null
  showDesignerDialog.value = true
}

const dragStart = (nodeType) => {
  draggedNodeType.value = nodeType
}

const handleDrop = (e) => {
  e.preventDefault()
  if (!draggedNodeType.value) return

  const rect = e.currentTarget.getBoundingClientRect()
  const x = e.clientX - rect.left
  const y = e.clientY - rect.top

  const newNode = {
    id: `node_${nodeCounter.value++}`,
    nodeType: draggedNodeType.value,
    name: getNodeTypeLabel(draggedNodeType.value),
    x,
    y,
    approvers: [],
    timeoutMinutes: 0,
    nextNodes: [],
    formFields: []
  }

  designerNodes.value.push(newNode)
  draggedNodeType.value = null
}

const selectNode = (node) => {
  selectedNode.value = node
}

const startNodeDrag = (e, node) => {
  e.stopPropagation()
  draggingNode.value = node
  dragOffset.value = {
    x: e.clientX - node.x,
    y: e.clientY - node.y
  }
  document.addEventListener('mousemove', moveNode)
  document.addEventListener('mouseup', endNodeDrag)
}

const moveNode = (e) => {
  if (draggingNode.value) {
    draggingNode.value.x = e.clientX - dragOffset.value.x
    draggingNode.value.y = e.clientY - dragOffset.value.y
  }
}

const endNodeDrag = () => {
  draggingNode.value = null
  document.removeEventListener('mousemove', moveNode)
  document.removeEventListener('mouseup', endNodeDrag)
}

const deleteNode = () => {
  if (!selectedNode.value) return
  designerNodes.value = designerNodes.value.filter(n => n.id !== selectedNode.value.id)
  selectedNode.value = null
}

const getLines = () => {
  const lines = []
  designerNodes.value.forEach(node => {
    if (node.nextNodes && node.nextNodes.length > 0) {
      node.nextNodes.forEach(nextNodeId => {
        const nextNode = designerNodes.value.find(n => n.id === nextNodeId)
        if (nextNode) {
          lines.push({
            from: node.id,
            to: nextNode.id,
            x1: node.x + 60,
            y1: node.y + 40,
            x2: nextNode.x + 60,
            y2: nextNode.y
          })
        }
      })
    }
  })
  return lines
}

const saveDefinition = async () => {
  if (!definitionForm.value.name) {
    ElMessage.warning('请输入流程名称')
    return
  }
  if (designerNodes.value.length === 0) {
    ElMessage.warning('请先添加节点')
    return
  }

  try {
    // 构建节点列表，检查是否需要自动添加结束节点
    const nodes = designerNodes.value.map(n => ({
      id: n.id,
      nodeType: n.nodeType,
      name: n.name,
      approvers: Array.isArray(n.approvers) ? n.approvers : [],
      timeoutMinutes: n.timeoutMinutes || 0,
      nextNodes: n.nextNodes || [],
      formFields: n.formFields || []
    }))

    // 如果最后一个节点不是结束节点，自动添加结束节点
    const lastNode = nodes[nodes.length - 1]
    if (lastNode && lastNode.nodeType !== 'End') {
      const endNodeId = `node_${nodeCounter.value++}`
      // 将最后一个节点的下一节点指向结束节点
      if (!lastNode.nextNodes) lastNode.nextNodes = []
      if (!lastNode.nextNodes.includes(endNodeId)) {
        lastNode.nextNodes.push(endNodeId)
      }
      nodes.push({
        id: endNodeId,
        nodeType: 'End',
        name: '结束',
        approvers: [],
        timeoutMinutes: 0,
        nextNodes: [],
        formFields: []
      })
    }

    const definition = {
      name: definitionForm.value.name,
      description: definitionForm.value.description,
      category: definitionForm.value.category || '默认',
      entityType: '',
      version: 1,
      isActive: true,
      startConfig: {},
      nodes
    }

    await createWorkflowDefinition(definition)
    ElMessage.success('流程定义保存成功')
    showDesignerDialog.value = false
    await loadDefinitions()
  } catch (error) {
    ElMessage.error('保存失败: ' + error.message)
  }
}

const handleDelete = async () => {
  if (!selectedDefinition.value) return

  try {
    await ElMessageBox.confirm('确定要删除此流程定义吗？', '提示', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })
    await deleteWorkflowDefinition(selectedDefinition.value.id)
    ElMessage.success('删除成功')
    selectedDefinition.value = null
    await loadDefinitions()
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error('删除失败: ' + error.message)
    }
  }
}

const handleExport = () => {
  if (!selectedDefinition.value) return

  const xml = `<?xml version="1.0" encoding="UTF-8"?>
<workflow>
  <name>${selectedDefinition.value.name}</name>
  <description>${selectedDefinition.value.description || ''}</description>
  <category>${selectedDefinition.value.category || ''}</category>
  <nodes>
${selectedDefinition.value.nodes.map(n => `    <node id="${n.id}" type="${n.nodeType}" name="${n.name}">
      <approvers>${Array.isArray(n.approvers) ? n.approvers.join(',') : n.approvers || ''}</approvers>
      <timeout>${n.timeoutMinutes || 0}</timeout>
      <nextNodes>${(n.nextNodes || []).join(',')}</nextNodes>
    </node>`).join('\n')}
  </nodes>
</workflow>`

  const blob = new Blob([xml], { type: 'application/xml' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = `${selectedDefinition.value.name}.xml`
  a.click()
  URL.revokeObjectURL(url)
  ElMessage.success('流程已导出')
}

const addFormField = () => {
  if (!selectedNode.value) return
  editingFieldIndex.value = null
  currentField.value = {
    id: `field_${Date.now()}`,
    label: '',
    key: '',
    fieldType: 'text',
    placeholder: '',
    required: false,
    optionsText: '',
    options: [],
    entityType: 'Project',
    entitySourceKey: '',
    allowedFileTypesText: ''
  }
  showFieldDialog.value = true
}

const editFormField = (idx) => {
  if (!selectedNode.value) return
  editingFieldIndex.value = idx
  const field = selectedNode.value.formFields[idx]
  currentField.value = {
    ...field,
    optionsText: field.options ? field.options.join('\n') : ''
  }
  showFieldDialog.value = true
}

const saveFormField = () => {
  if (!currentField.value.label || !currentField.value.key) {
    ElMessage.warning('请填写字段标签和Key')
    return
  }

  if (currentField.value.fieldType === 'select' && !currentField.value.optionsText) {
    ElMessage.warning('下拉选择字段必须配置选项')
    return
  }

  const field = {
    id: currentField.value.id,
    label: currentField.value.label,
    key: currentField.value.key,
    fieldType: currentField.value.fieldType,
    placeholder: currentField.value.placeholder,
    required: currentField.value.required,
    options: currentField.value.optionsText ? currentField.value.optionsText.split('\n').map(o => o.trim()).filter(o => o) : [],
    entityType: currentField.value.entityType || '',
    entitySourceKey: currentField.value.entitySourceKey || '',
    allowedFileTypes: currentField.value.allowedFileTypesText
      ? currentField.value.allowedFileTypesText.split(',').map(s => s.trim()).filter(s => s)
      : []
  }

  if (editingFieldIndex.value !== null) {
    selectedNode.value.formFields[editingFieldIndex.value] = field
  } else if (selectedNode.value) {
    if (!selectedNode.value.formFields) {
      selectedNode.value.formFields = []
    }
    selectedNode.value.formFields.push(field)
  }

  showFieldDialog.value = false
  editingFieldIndex.value = null
  ElMessage.success('表单字段保存成功')
}

const deleteFormField = (idx) => {
  if (!selectedNode.value) return
  selectedNode.value.formFields.splice(idx, 1)
  ElMessage.success('表单字段已删除')
}
</script>

<style scoped>
.workflow-manager-container {
  display: flex;
  height: calc(100vh - 120px);
  padding: 20px;
  gap: 20px;
  background: #f5f5f5;
}

.left-panel {
  width: 320px;
  background: #fff;
  border-radius: 8px;
  display: flex;
  flex-direction: column;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.08);
}

.panel-header {
  padding: 16px;
  border-bottom: 1px solid #eee;
}

.panel-header h3 {
  margin: 0 0 12px 0;
  font-size: 16px;
  color: #333;
}

.header-actions {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
}

.definition-list {
  flex: 1;
  overflow-y: auto;
  padding: 8px;
}

.definition-item {
  padding: 12px 16px;
  border-radius: 6px;
  cursor: pointer;
  margin-bottom: 4px;
  transition: all 0.2s;
  border: 1px solid transparent;
}

.definition-item:hover {
  background: #f5f5f5;
}

.definition-item.active {
  background: #ecf5ff;
  border-color: #409eff;
}

.def-name {
  font-size: 14px;
  font-weight: 500;
  color: #333;
  margin-bottom: 4px;
}

.def-category {
  font-size: 12px;
  color: #999;
}

.empty-list {
  text-align: center;
  padding: 40px 20px;
  color: #999;
  font-size: 14px;
}

.right-panel {
  flex: 1;
  background: #fff;
  border-radius: 8px;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.08);
  overflow-y: auto;
}

.empty-detail {
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
}

.workflow-detail {
  padding: 24px;
}

.detail-header {
  margin-bottom: 20px;
  padding-bottom: 16px;
  border-bottom: 1px solid #eee;
}

.detail-header h3 {
  margin: 0 0 8px 0;
  font-size: 18px;
  color: #333;
}

.form-desc {
  margin: 0;
  font-size: 14px;
  color: #666;
}

.nodes-preview {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.node-item {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px;
  background: #f5f7fa;
  border-radius: 4px;
}

.node-name {
  font-weight: 500;
}

.node-approvers {
  color: #666;
  font-size: 12px;
}

/* 设计器样式 */
.designer-container {
  height: 600px;
}

.toolbar {
  padding: 10px;
  border: 1px solid #ddd;
  border-radius: 4px;
}

.toolbar h4 {
  margin: 0 0 10px 0;
  font-size: 14px;
}

.node-types {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.node-item {
  padding: 8px;
  border: 1px solid #409eff;
  border-radius: 4px;
  cursor: move;
  display: flex;
  align-items: center;
  gap: 5px;
  font-size: 12px;
  background: #f0f9ff;
}

.canvas-container {
  position: relative;
  width: 100%;
  height: 100%;
  border: 2px dashed #ddd;
  border-radius: 4px;
  background: #fafafa;
  overflow: hidden;
}

.canvas {
  position: absolute;
  width: 100%;
  height: 100%;
}

.nodes-container {
  position: relative;
  width: 100%;
  height: 100%;
}

.node {
  position: absolute;
  width: 120px;
  padding: 10px;
  background: white;
  border: 2px solid #409eff;
  border-radius: 4px;
  cursor: pointer;
}

.node.selected {
  border-color: #f56c6c;
  box-shadow: 0 0 0 2px rgba(245, 108, 108, 0.2);
}

.node-content {
  text-align: center;
  font-size: 12px;
}

.node-type {
  color: #909399;
  font-size: 11px;
}

.node-name {
  font-weight: bold;
  margin-top: 4px;
}

.config-panel {
  padding: 10px;
  border: 1px solid #ddd;
  border-radius: 4px;
  max-height: 600px;
  overflow-y: auto;
}

.config-panel h4 {
  margin: 0 0 10px 0;
  font-size: 14px;
}

.field-item {
  display: flex;
  justify-content: space-between;
  align-items: start;
  padding: 8px;
  background: #f5f7fa;
  margin-bottom: 8px;
  border-radius: 4px;
  font-size: 12px;
}

.empty-state {
  text-align: center;
  color: #909399;
  padding: 20px;
}
</style>
