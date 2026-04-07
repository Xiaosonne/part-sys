<template>
  <div class="page-main">
    <div style="padding: 20px 24px;">
      <div class="panel-card">
        <div
          style="padding: 12px 16px; display: flex; align-items: center; justify-content: space-between; border-bottom: 1px solid var(--color-border);">
          <span style="font-size: 14px; font-weight: 600;">流程设计器</span>
          <div style="display: flex; gap: 10px;">
            <el-button type="primary" size="small" @click="saveDefinition">保存流程</el-button>
            <el-button size="small" @click="loadDefinition">加载流程</el-button>
            <el-button size="small" @click="exportXML">导出XML</el-button>
          </div>
        </div>
        <div style="padding: 16px;">
          <el-row :gutter="20">
            <!-- 左侧工具栏 -->
            <el-col :span="4">
              <div class="toolbar">
                <h4>节点类型</h4>
                <div class="node-types">
                  <div class="node-item" draggable="true" @dragstart="dragStart('Start')">
                    <el-icon>
                      <CircleCheck />
                    </el-icon>
                    <span>开始</span>
                  </div>
                  <div class="node-item" draggable="true" @dragstart="dragStart('SingleApproval')">
                    <el-icon>
                      <User />
                    </el-icon>
                    <span>单人审批</span>
                  </div>
                  <div class="node-item" draggable="true" @dragstart="dragStart('MultiApprovalAnd')">
                    <el-icon>
                      <DocumentCopy />
                    </el-icon>
                    <span>多人会签</span>
                  </div>
                  <div class="node-item" draggable="true" @dragstart="dragStart('MultiApprovalOr')">
                    <el-icon>
                      <UserFilled />
                    </el-icon>
                    <span>多人或签</span>
                  </div>
                  <div class="node-item" draggable="true" @dragstart="dragStart('End')">
                    <el-icon>
                      <CircleCloseFilled />
                    </el-icon>
                    <span>结束</span>
                  </div>
                </div>
              </div>
            </el-col>

            <!-- 中间画布 -->
            <el-col :span="16">
              <div class="canvas-container" @drop="handleDrop" @dragover.prevent>
                <svg ref="canvas" class="canvas">
                  <defs>
                    <marker id="arrowhead" markerWidth="10" markerHeight="10" refX="9" refY="3" orient="auto">
                      <polygon points="0 0, 10 3, 0 6" fill="#666" />
                    </marker>
                  </defs>
                  <line v-for="line in getLines()" :key="`${line.from}-${line.to}`" :x1="line.x1" :y1="line.y1"
                    :x2="line.x2" :y2="line.y2" stroke="#666" stroke-width="2" marker-end="url(#arrowhead)" />
                </svg>
                <div class="nodes-container">
                  <div v-for="node in nodes" :key="node.id" class="node"
                    :style="{ left: node.x + 'px', top: node.y + 'px' }" @click="selectNode(node)"
                    @mousedown="startNodeDrag($event, node)" :class="{ selected: selectedNode?.id === node.id }">
                    <div class="node-content">
                      <div class="node-type">{{ getNodeTypeLabel(node.nodeType) }}</div>
                      <div class="node-name">{{ node.name }}</div>
                    </div>
                  </div>
                </div>
              </div>
            </el-col>

            <!-- 右侧配置面板 -->
            <el-col :span="4">
              <div class="config-panel">
                <h4>节点配置</h4>
                <div v-if="selectedNode" class="node-config">
                  <el-form :model="selectedNode" label-width="80px">
                    <el-form-item label="节点名称">
                      <el-input v-model="selectedNode.name" />
                    </el-form-item>
                    <el-form-item label="节点类型">
                      <el-select v-model="selectedNode.nodeType" disabled>
                        <el-option label="开始" value="Start" />
                        <el-option label="单人审批" value="SingleApproval" />
                        <el-option label="多人会签" value="MultiApprovalAnd" />
                        <el-option label="多人或签" value="MultiApprovalOr" />
                        <el-option label="结束" value="End" />
                      </el-select>
                    </el-form-item>
                    <el-form-item v-if="selectedNode.nodeType !== 'Start' && selectedNode.nodeType !== 'End'"
                      label="审批人">
                      <el-select v-model="selectedNode.approvers" multiple filterable placeholder="选择审批人">
                        <el-option v-for="u in users" :key="u.id" :label="u.username" :value="u.id" />
                      </el-select>
                    </el-form-item>
                    <el-form-item v-if="selectedNode.nodeType !== 'Start' && selectedNode.nodeType !== 'End'"
                      label="超时(分钟)">
                      <el-input-number v-model="selectedNode.timeoutMinutes" :min="0" />
                    </el-form-item>
                    <el-form-item v-if="selectedNode.nodeType !== 'End'" label="下一节点">
                      <el-select v-model="selectedNode.nextNodes" multiple placeholder="选择下一节点">
                        <el-option v-for="node in nodes.filter(n => n.id !== selectedNode.id)" :key="node.id"
                          :label="node.name" :value="node.id" />
                      </el-select>
                    </el-form-item>

                    <!-- 表单字段配置 -->
                    <el-divider />
                    <div style="margin-bottom: 10px;">
                      <div
                        style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 10px;">
                        <span style="font-weight: bold; font-size: 12px;">表单字段</span>
                        <el-button type="primary" size="small" @click="addFormField">添加字段</el-button>
                      </div>
                      <div v-if="selectedNode.formFields && selectedNode.formFields.length > 0"
                        style="max-height: 200px; overflow-y: auto;">
                        <div v-for="(field, idx) in selectedNode.formFields" :key="idx"
                          style="padding: 8px; background: #f5f7fa; margin-bottom: 8px; border-radius: 4px; font-size: 12px;">
                          <div style="display: flex; justify-content: space-between; align-items: start;">
                            <div style="flex: 1;">
                              <div><strong>{{ field.label }}</strong></div>
                              <div style="color: #666; font-size: 11px;">类型: {{ field.fieldType }} {{ field.required ?
                                '(必填)' : '(可选)' }}</div>
                            </div>
                            <div style="display: flex; gap: 5px;">
                              <el-button type="primary" link size="small" @click="editFormField(idx)">编辑</el-button>
                              <el-button type="danger" link size="small" @click="deleteFormField(idx)">删除</el-button>
                            </div>
                          </div>
                        </div>
                      </div>
                      <div v-else style="color: #999; font-size: 12px; padding: 10px; text-align: center;">暂无表单字段</div>
                    </div>

                    <el-button type="danger" @click="deleteNode">删除节点</el-button>
                  </el-form>
                </div>
                <div v-else class="empty-state">
                  <p>选择节点进行配置</p>
                </div>
              </div>
            </el-col>
          </el-row>
        </div>
      </div>

      <!-- 流程定义对话框 -->
      <el-dialog v-model="showDefinitionDialog" title="流程定义" width="600px">
        <el-form :model="definitionForm" label-width="100px">
          <el-form-item label="流程名称">
            <el-input v-model="definitionForm.name" />
          </el-form-item>
          <el-form-item label="流程描述">
            <el-input v-model="definitionForm.description" type="textarea" />
          </el-form-item>
          <el-form-item label="流程分类">
            <el-input v-model="definitionForm.category" />
          </el-form-item>
        </el-form>
        <template #footer>
          <el-button @click="showDefinitionDialog = false">取消</el-button>
          <el-button type="primary" @click="submitDefinition">保存</el-button>
        </template>
      </el-dialog>

      <!-- 加载流程对话框 -->
      <el-dialog v-model="showLoadDialog" title="加载流程" width="600px">
        <el-table :data="definitions" stripe @row-click="selectDefinitionToLoad">
          <el-table-column prop="name" label="流程名称" width="200" />
          <el-table-column prop="category" label="分类" width="150" />
          <el-table-column prop="description" label="描述" />
        </el-table>
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

          <!-- 项目选择类型的配置 -->
          <el-form-item v-if="currentField.fieldType === 'project'" label="实体类型">
            <el-select v-model="currentField.entityType" placeholder="选择实体类型">
              <el-option label="项目" value="Project" />
              <el-option label="文件夹" value="Folder" />
              <el-option label="配件" value="Part" />
            </el-select>
          </el-form-item>

          <!-- 项目文件选择类型的配置 -->
          <template v-if="currentField.fieldType === 'projectFile'">
            <el-form-item label="关联项目字段">
              <el-select v-model="currentField.entitySourceKey" placeholder="选择关联的项目字段">
                <el-option v-for="f in availableProjectFields" :key="f.key" :label="`${f.label} (${f.key})`"
                  :value="f.key" />
              </el-select>
            </el-form-item>
            <el-form-item label="允许的文件类型">
              <el-input v-model="currentField.allowedFileTypesText" type="textarea" rows="2"
                placeholder="如：.pdf,.docx,.dwg" />
            </el-form-item>
          </template>

          <!-- 占位符 -->
          <el-form-item v-if="currentField.fieldType !== 'project' && currentField.fieldType !== 'projectFile'"
            label="占位符">
            <el-input v-model="currentField.placeholder" placeholder="输入框提示文本" />
          </el-form-item>

          <!-- 下拉选择的选项 -->
          <el-form-item v-if="currentField.fieldType === 'select'" label="选项" required>
            <el-input v-model="currentField.optionsText" type="textarea" rows="3"
              placeholder="每行一个选项，如：同意&#10;不同意&#10;需要修改" />
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
  </div>
</template>

<script setup>
import { ref, reactive, onMounted, computed } from 'vue'
import { ElMessage } from 'element-plus'
import { CircleCheck, User, DocumentCopy, UserFilled, CircleCloseFilled } from '@element-plus/icons-vue'
import { createWorkflowDefinition, getWorkflowDefinitions, getWorkflowDefinition } from '../services/workflowApi'
import { getUsers } from '../api/users'

const nodes = ref([])
const selectedNode = ref(null)
const draggedNodeType = ref(null)
const nodeCounter = ref(0)
const showDefinitionDialog = ref(false)
const showLoadDialog = ref(false)
const showFieldDialog = ref(false)
const definitions = ref([])
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

// 获取可关联的项目字段（用于 projectFile 字段配置）
const availableProjectFields = computed(() => {
  // 只有普通审批节点有项目字段（开始节点不参与审批流程）
  return nodes.value.flatMap(n => n.formFields || []).filter(f => f.fieldType === 'project')
})
const definitionForm = reactive({
  name: '',
  description: '',
  category: 'Project',
  entityType: 'Project',
  startConfig: {
    requireEntity: false,
    entityType: 'Project',
    requireFiles: false,
    minFileCount: 0,
    maxFileCount: 0,
    allowedFileTypes: [],
    formFields: []
  }
})

onMounted(async () => {
  try {
    const res = await getUsers()
    users.value = res.data || []
  } catch (e) {
    console.error('Failed to load users', e)
  }
})

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

  nodes.value.push(newNode)
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
  nodes.value = nodes.value.filter(n => n.id !== selectedNode.value.id)
  selectedNode.value = null
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

const getLines = () => {
  const lines = []
  nodes.value.forEach(node => {
    if (node.nextNodes && node.nextNodes.length > 0) {
      node.nextNodes.forEach(nextNodeId => {
        const nextNode = nodes.value.find(n => n.id === nextNodeId)
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

const saveDefinition = () => {
  if (nodes.value.length === 0) {
    ElMessage.warning('请先添加节点')
    return
  }
  showDefinitionDialog.value = true
}

const submitDefinition = async () => {
  try {
    const definition = {
      name: definitionForm.name,
      description: definitionForm.description,
      category: definitionForm.category,
      entityType: definitionForm.entityType,
      version: 1,
      isActive: true,
      startConfig: {
        requireEntity: definitionForm.startConfig.requireEntity,
        entityType: definitionForm.startConfig.requireEntity ? definitionForm.startConfig.entityType : '',
        requireFiles: definitionForm.startConfig.requireFiles,
        minFileCount: definitionForm.startConfig.minFileCount || 0,
        maxFileCount: definitionForm.startConfig.maxFileCount || 0,
        allowedFileTypes: definitionForm.startConfig.allowedFileTypes || [],
        formFields: definitionForm.startConfig.formFields || []
      },
      nodes: nodes.value.map(n => ({
        id: n.id,
        nodeType: n.nodeType,
        name: n.name,
        approvers: Array.isArray(n.approvers) ? n.approvers : (n.approvers ? n.approvers.split(',').map(a => a.trim()) : []),
        timeoutMinutes: n.timeoutMinutes || 0,
        nextNodes: n.nextNodes || [],
        formFields: n.formFields || []
      }))
    }

    await createWorkflowDefinition(definition)
    ElMessage.success('流程定义保存成功')
    showDefinitionDialog.value = false
  } catch (error) {
    ElMessage.error('保存失败: ' + error.message)
  }
}

const loadDefinition = async () => {
  try {
    definitions.value = (await getWorkflowDefinitions()).data || []
    showLoadDialog.value = true
  } catch (error) {
    ElMessage.error('加载流程列表失败: ' + error.message)
  }
}

const selectDefinitionToLoad = async (definition) => {
  try {
    const fullDef = (await getWorkflowDefinition(definition.id)).data
    nodes.value = fullDef.nodes.map((n, idx) => ({
      id: n.id,
      nodeType: n.nodeType,
      name: n.name,
      approvers: n.approvers || [],
      timeoutMinutes: n.timeoutMinutes || 0,
      nextNodes: n.nextNodes || [],
      formFields: n.formFields || [],
      x: 100 + idx * 150,
      y: 100
    }))
    nodeCounter.value = nodes.value.length

    // 加载 startConfig
    if (fullDef.startConfig) {
      definitionForm.startConfig = {
        requireEntity: fullDef.startConfig.requireEntity || false,
        entityType: fullDef.startConfig.entityType || 'Project',
        requireFiles: fullDef.startConfig.requireFiles || false,
        minFileCount: fullDef.startConfig.minFileCount || 0,
        maxFileCount: fullDef.startConfig.maxFileCount || 0,
        allowedFileTypes: fullDef.startConfig.allowedFileTypes || [],
        formFields: fullDef.startConfig.formFields || []
      }
    }

    // 加载基本定义信息
    definitionForm.name = fullDef.name
    definitionForm.description = fullDef.description || ''
    definitionForm.category = fullDef.category || 'Project'
    definitionForm.entityType = fullDef.entityType || 'Project'

    showLoadDialog.value = false
    ElMessage.success('流程加载成功')
  } catch (error) {
    ElMessage.error('加载流程失败: ' + error.message)
  }
}

const exportXML = () => {
  const xml = `<?xml version="1.0" encoding="UTF-8"?>
<workflow>
  <nodes>
${nodes.value.map(n => `    <node id="${n.id}" type="${n.nodeType}" name="${n.name}">
      <approvers>${Array.isArray(n.approvers) ? n.approvers.join(',') : n.approvers}</approvers>
      <timeout>${n.timeoutMinutes}</timeout>
      <nextNodes>${n.nextNodes.join(',')}</nextNodes>
    </node>`).join('\n')}
  </nodes>
</workflow>`

  const blob = new Blob([xml], { type: 'application/xml' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = 'workflow.xml'
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

  // 判断是节点表单字段
  if (editingFieldIndex.value !== null) {
    // 节点表单字段（编辑）
    selectedNode.value.formFields[editingFieldIndex.value] = field
  } else if (selectedNode.value) {
    // 节点表单字段（新增）
    if (!selectedNode.value.formFields) {
      selectedNode.value.formFields = []
    }
    selectedNode.value.formFields.push(field)
  } else {
    ElMessage.warning('请先选中一个节点')
    return
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

// allowedFileTypes 文本转换
const allowedFileTypesText = computed({
  get: () => definitionForm.startConfig.allowedFileTypes.join(','),
  set: (val) => {
    definitionForm.startConfig.allowedFileTypes = val.split(',').map(s => s.trim()).filter(s => s)
  }
})
</script>

<style scoped>
.toolbar {
  padding: 10px;
  border: 1px solid var(--color-border);
  border-radius: var(--radius);
}

.toolbar h4 {
  margin: 0 0 10px 0;
  font-size: 14px;
  font-weight: 600;
}

.node-types {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.node-item {
  padding: 8px;
  border: 1px solid var(--color-primary);
  border-radius: var(--radius);
  cursor: move;
  display: flex;
  align-items: center;
  gap: 5px;
  font-size: 12px;
  background: var(--color-primary-50);
  transition: all 0.3s;
}

.node-item:hover {
  background: var(--color-primary-100);
}

.canvas-container {
  position: relative;
  width: 100%;
  height: 600px;
  border: 2px dashed var(--color-border);
  border-radius: var(--radius);
  background: var(--color-bg-page);
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
  border: 2px solid var(--color-primary);
  border-radius: var(--radius);
  cursor: pointer;
  transition: all 0.3s;
}

.node:hover {
  box-shadow: var(--shadow-md);
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
  color: var(--color-text-muted);
  font-size: 11px;
}

.node-name {
  font-weight: bold;
  margin-top: 4px;
}

.config-panel {
  padding: 10px;
  border: 1px solid var(--color-border);
  border-radius: var(--radius);
  max-height: 600px;
  overflow-y: auto;
}

.config-panel h4 {
  margin: 0 0 10px 0;
  font-size: 14px;
  font-weight: 600;
}

.node-config {
  font-size: 12px;
}

.empty-state {
  text-align: center;
  color: var(--color-text-muted);
  padding: 20px;
}
</style>
