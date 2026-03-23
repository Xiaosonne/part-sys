<template>
  <div class="workflow-canvas">
    <el-row :gutter="20">
      <!-- 左侧工具栏 -->
      <el-col :span="4">
        <div class="toolbar">
          <h4>节点类型</h4>
          <div class="node-types">
            <div class="node-item" draggable="true" @dragstart="dragStart('Start')">
              <el-icon><CircleCheck /></el-icon>
              <span>开始</span>
            </div>
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
            <div class="node-item" draggable="true" @dragstart="dragStart('End')">
              <el-icon><CircleCloseFilled /></el-icon>
              <span>结束</span>
            </div>
          </div>
        </div>
      </el-col>

      <!-- 中间画布 -->
      <el-col :span="12">
        <div class="canvas-container" @drop="handleDrop" @dragover.prevent>
          <svg ref="canvas" class="canvas">
            <defs>
              <marker id="arrowhead" markerWidth="10" markerHeight="10" refX="9" refY="3" orient="auto">
                <polygon points="0 0, 10 3, 0 6" fill="#666" />
              </marker>
            </defs>
            <line
              v-for="line in getLines()"
              :key="`${line.from}-${line.to}`"
              :x1="line.x1"
              :y1="line.y1"
              :x2="line.x2"
              :y2="line.y2"
              stroke="#666"
              stroke-width="2"
              marker-end="url(#arrowhead)"
            />
          </svg>
          <div class="nodes-container">
            <div
              v-for="node in modelValue"
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
          <h4>节点配置</h4>
          <div v-if="selectedNode" class="node-config">
            <el-form :model="selectedNode" label-width="80px" size="small">
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
              <el-form-item v-if="selectedNode.nodeType !== 'Start' && selectedNode.nodeType !== 'End'" label="审批人">
                <el-select
                  v-model="selectedNode.approvers"
                  multiple
                  filterable
                  placeholder="选择审批人"
                >
                  <el-option
                    v-for="u in users"
                    :key="u.id"
                    :label="u.username"
                    :value="u.id"
                  />
                </el-select>
              </el-form-item>
              <el-form-item v-if="selectedNode.nodeType !== 'Start' && selectedNode.nodeType !== 'End'" label="超时(分钟)">
                <el-input-number v-model="selectedNode.timeoutMinutes" :min="0" />
              </el-form-item>
              <el-form-item v-if="selectedNode.nodeType !== 'End'" label="下一节点">
                <el-select v-model="selectedNode.nextNodes" multiple placeholder="选择下一节点">
                  <el-option
                    v-for="n in modelValue.filter(n => n.id !== selectedNode.id)"
                    :key="n.id"
                    :label="n.name"
                    :value="n.id"
                  />
                </el-select>
              </el-form-item>

              <!-- 表单字段配置 -->
              <el-divider />
              <div style="margin-bottom: 10px;">
                <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 10px;">
                  <span style="font-weight: bold; font-size: 12px;">表单字段</span>
                  <el-button type="primary" size="small" @click="addFormField">添加字段</el-button>
                </div>
                <div v-if="selectedNode.formFields && selectedNode.formFields.length > 0" style="max-height: 200px; overflow-y: auto;">
                  <div
                    v-for="(field, idx) in selectedNode.formFields"
                    :key="idx"
                    style="padding: 8px; background: #f5f7fa; margin-bottom: 8px; border-radius: 4px; font-size: 12px;"
                  >
                    <div style="display: flex; justify-content: space-between; align-items: start;">
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
                </div>
                <div v-else style="color: #999; font-size: 12px; padding: 10px; text-align: center;">暂无表单字段</div>
              </div>

              <el-button type="danger" size="small" @click="deleteNode">删除节点</el-button>
            </el-form>
          </div>
          <div v-else class="empty-state">
            <p>选择或拖拽节点进行配置</p>
          </div>
        </div>
      </el-col>
    </el-row>

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
              <el-option
                v-for="f in availableProjectFields"
                :key="f.key"
                :label="`${f.label} (${f.key})`"
                :value="f.key"
              />
            </el-select>
          </el-form-item>
          <el-form-item label="允许的文件类型">
            <el-input v-model="currentField.allowedFileTypesText" type="textarea" rows="2" placeholder="如：.pdf,.docx,.dwg" />
          </el-form-item>
        </template>

        <!-- 占位符 -->
        <el-form-item v-if="currentField.fieldType !== 'project' && currentField.fieldType !== 'projectFile'" label="占位符">
          <el-input v-model="currentField.placeholder" placeholder="输入框提示文本" />
        </el-form-item>

        <!-- 下拉选择的选项 -->
        <el-form-item v-if="currentField.fieldType === 'select'" label="选项" required>
          <el-input v-model="currentField.optionsText" type="textarea" rows="3" placeholder="每行一个选项，如：同意\n不同意\n需要修改" />
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
import { ElMessage } from 'element-plus'
import { CircleCheck, User, DocumentCopy, UserFilled, CircleCloseFilled } from '@element-plus/icons-vue'
import { getUsers } from '../api/users'

const props = defineProps({
  modelValue: {
    type: Array,
    default: () => []
  },
  modelModifiers: {
    default: () => ({})
  }
})

const emit = defineEmits(['update:modelValue', 'change'])

const nodes = computed({
  get: () => props.modelValue,
  set: (val) => emit('update:modelValue', val)
})

const selectedNode = ref(null)
const draggedNodeType = ref(null)
const nodeCounter = ref(0)
const showFieldDialog = ref(false)
const editingFieldIndex = ref(null)
const users = ref([])
const draggingNode = ref(null)
const dragOffset = ref({ x: 0, y: 0 })

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

// 获取可关联的项目字段
const availableProjectFields = computed(() => {
  return nodes.value.flatMap(n => n.formFields || []).filter(f => f.fieldType === 'project')
})

onMounted(async () => {
  nodeCounter.value = nodes.value.length
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

  nodes.value = [...nodes.value, newNode]
  draggedNodeType.value = null
  emit('change')
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
    const updated = nodes.value.map(n => {
      if (n.id === draggingNode.value.id) {
        return { ...n, x: e.clientX - dragOffset.value.x, y: e.clientY - dragOffset.value.y }
      }
      return n
    })
    nodes.value = updated
  }
}

const endNodeDrag = () => {
  draggingNode.value = null
  document.removeEventListener('mousemove', moveNode)
  document.removeEventListener('mouseup', endNodeDrag)
  emit('change')
}

const deleteNode = () => {
  if (!selectedNode.value) return
  nodes.value = nodes.value.filter(n => n.id !== selectedNode.value.id)
  selectedNode.value = null
  emit('change')
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
  emit('change')
}

const deleteFormField = (idx) => {
  if (!selectedNode.value) return
  selectedNode.value.formFields.splice(idx, 1)
  emit('change')
}

// 暴露方法供父组件调用
defineExpose({
  getNodes: () => nodes.value,
  setNodes: (newNodes) => { nodes.value = newNodes },
  resetNodes: () => {
    nodes.value = []
    nodeCounter.value = 0
  }
})
</script>

<style scoped>
.workflow-canvas {
  padding: 10px;
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
  transition: all 0.3s;
}

.node-item:hover {
  background: #e0f2fe;
}

.canvas-container {
  position: relative;
  width: 100%;
  height: 500px;
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
  transition: all 0.3s;
}

.node:hover {
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.1);
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
  max-height: 500px;
  overflow-y: auto;
}

.config-panel h4 {
  margin: 0 0 10px 0;
  font-size: 14px;
}

.node-config {
  font-size: 12px;
}

.empty-state {
  text-align: center;
  color: #909399;
  padding: 20px;
}
</style>
