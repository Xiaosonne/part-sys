<template>
  <div class="workflow-container">
    <!-- 顶部工具栏 -->
    <div class="toolbar">
      <el-button type="primary" @click="openStartDialog">
        <el-icon><Plus /></el-icon>
        发起新流程
      </el-button>
      <el-button @click="loadInstances">
        <el-icon><Refresh /></el-icon>
        刷新
      </el-button>
    </div>

    <!-- 流程表格 -->
    <div class="table-container">
      <el-table :data="instances" stripe v-loading="loading" @row-click="handleRowClick" style="width: 100%; max-width: 100%;">
        <el-table-column prop="name" label="流程名称" min-width="150" show-overflow-tooltip />
        <el-table-column label="状态" width="90">
          <template #default="{ row }">
            <el-tag :type="getStatusType(row.status)">{{ row.status }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="发起人" min-width="100" show-overflow-tooltip>
          <template #default="{ row }">
            {{ row.startedByName || row.startedBy }}
          </template>
        </el-table-column>
        <el-table-column label="当前节点" min-width="120" show-overflow-tooltip>
          <template #default="{ row }">
            {{ row.currentNodeName || '-' }}
          </template>
        </el-table-column>
        <el-table-column label="发起时间" min-width="160">
          <template #default="{ row }">
            {{ formatDate(row.startedAt) }}
          </template>
        </el-table-column>
        <el-table-column label="完成时间" min-width="160">
          <template #default="{ row }">
            {{ formatDate(row.completedAt) }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="140" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" @click.stop="openDetail(row)">详情</el-button>
            <el-button link type="danger" v-if="row.status === 'Running'" @click.stop="cancelInstance(row)">取消</el-button>
          </template>
        </el-table-column>
        <template #empty>
          <el-empty description="暂无流程记录" />
        </template>
      </el-table>
      <!-- 分页 -->
      <div class="pagination">
        <el-pagination
          v-model:current-page="currentPage"
          v-model:page-size="pageSize"
          :page-sizes="[10, 20, 50, 100]"
          :total="total"
          layout="total, sizes, prev, pager, next, jumper"
          @size-change="handleSizeChange"
          @current-change="handleCurrentChange"
        />
      </div>
    </div>

    <!-- 发起新流程对话框 -->
    <el-dialog v-model="showStartDialog" title="发起新流程" width="700px" destroy-on-close>
      <div class="start-dialog-content">
        <!-- 左侧：流程定义列表 -->
        <div class="definition-panel">
          <h4>选择流程模板</h4>
          <el-input v-model="searchText" placeholder="搜索流程..." clearable size="small" />
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
          </div>
        </div>

        <!-- 右侧：启动表单 -->
        <div class="form-panel" v-if="selectedDefinition">
          <h4>填写信息</h4>
          <el-form :model="startForm" label-width="100px">
            <el-form-item label="流程名称" required>
              <el-input v-model="startForm.name" :placeholder="`${selectedDefinition.name} - 实例`" />
            </el-form-item>

            <!-- 项目选择字段 -->
            <el-form-item
              v-for="field in projectFields"
              :key="field.key"
              :label="field.label"
              :required="field.required"
            >
              <el-select
                v-model="startForm.extraData[field.key]"
                :placeholder="`选择${field.label}`"
                @change="(val) => onProjectChange(field.key, val)"
              >
                <el-option
                  v-for="project in projectList"
                  :key="project.id"
                  :label="project.name"
                  :value="project.id"
                />
              </el-select>
            </el-form-item>

            <!-- 项目文件选择字段 -->
            <el-form-item
              v-for="field in projectFileFields"
              :key="field.key"
              :label="field.label"
              :required="field.required"
            >
              <div v-if="!getProjectFileSource(field)" style="color: #999;">
                请先选择关联的项目
              </div>
              <div v-else-if="!projectFilesMap[field.key] || projectFilesMap[field.key].length === 0" style="color: #999;">
                该项目暂无文件
              </div>
              <div v-else class="file-tree-container">
                <el-tree
                  :ref="`fileTree_${field.key}`"
                  :data="projectFilesMap[field.key]"
                  node-key="id"
                  show-checkbox
                  :props="{ children: 'children', label: 'name' }"
                  default-expand-all
                  @check-change="(node, checked) => onFileChange(field.key, node, checked)"
                />
              </div>
            </el-form-item>

            <!-- 普通表单字段 -->
            <el-form-item
              v-for="field in normalFields"
              :key="field.key"
              :label="field.label"
              :required="field.required"
            >
              <el-input
                v-if="field.fieldType === 'text'"
                v-model="startForm.extraData[field.key]"
                :placeholder="field.placeholder"
              />
              <el-input
                v-else-if="field.fieldType === 'textarea'"
                v-model="startForm.extraData[field.key]"
                type="textarea"
                :rows="3"
                :placeholder="field.placeholder"
              />
              <el-select
                v-else-if="field.fieldType === 'select'"
                v-model="startForm.extraData[field.key]"
                :placeholder="field.placeholder || '请选择'"
              >
                <el-option
                  v-for="option in field.options"
                  :key="option"
                  :label="option"
                  :value="option"
                />
              </el-select>
              <el-input-number
                v-else-if="field.fieldType === 'number'"
                v-model="startForm.extraData[field.key]"
              />
              <el-checkbox
                v-else-if="field.fieldType === 'checkbox'"
                v-model="startForm.extraData[field.key]"
              >
                {{ field.placeholder || '是' }}
              </el-checkbox>
            </el-form-item>
          </el-form>
        </div>
        <div v-else class="no-selection">
          <el-empty description="请从左侧选择一个流程模板" />
        </div>
      </div>
      <template #footer>
        <el-button @click="showStartDialog = false">取消</el-button>
        <el-button type="primary" @click="submitWorkflow" :loading="submitting" :disabled="!selectedDefinition">
          启动流程
        </el-button>
      </template>
    </el-dialog>

    <!-- 流程详情抽屉 -->
    <el-drawer v-model="showDetailDrawer" title="流程详情" size="600px" direction="rtl">
      <div v-if="selectedInstance" class="detail-content">
        <el-descriptions :column="2" border>
          <el-descriptions-item label="流程名称">{{ selectedInstance.name }}</el-descriptions-item>
          <el-descriptions-item label="状态">
            <el-tag :type="getStatusType(selectedInstance.status)">{{ selectedInstance.status }}</el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="发起人">{{ selectedInstance.startedByName }}</el-descriptions-item>
          <el-descriptions-item label="发起时间">{{ formatDate(selectedInstance.startedAt) }}</el-descriptions-item>
          <el-descriptions-item label="当前节点" :span="2">{{ selectedInstance.currentNodeName || '-' }}</el-descriptions-item>
        </el-descriptions>

        <!-- 审批历史 -->
        <div class="history-section">
          <h4>审批历史</h4>
          <el-timeline v-if="history.length > 0">
            <el-timeline-item
              v-for="(item, index) in history"
              :key="index"
              :type="getHistoryType(item.action)"
              :timestamp="formatDate(item.createdAt)"
              placement="top"
            >
              <el-card shadow="hover">
                <div class="history-item">
                  <div class="history-node">{{ item.nodeName }}</div>
                  <div class="history-action">
                    <el-tag size="small" :type="getHistoryType(item.action)">{{ item.action }}</el-tag>
                    <span class="history-operator">by {{ item.operatorName || item.operatorId }}</span>
                  </div>
                  <div v-if="item.formData && Object.keys(item.formData).length > 0" class="history-formdata">
                    <div v-for="(value, key) in item.formData" :key="key" class="formdata-item">
                      <span class="formdata-key">{{ key }}:</span>
                      <span class="formdata-value">{{ formatFormDataValue(value) }}</span>
                    </div>
                  </div>
                  <div v-if="item.comment" class="history-comment">
                    意见: {{ item.comment }}
                  </div>
                </div>
              </el-card>
            </el-timeline-item>
          </el-timeline>
          <div v-else class="no-history">暂无审批记录</div>
        </div>
      </div>
    </el-drawer>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Refresh } from '@element-plus/icons-vue'
import { getWorkflowDefinitions, getWorkflowDefinition, startWorkflowInstance, getMyWorkflowInstances, getWorkflowInstance, getWorkflowHistory, cancelWorkflowInstance } from '../services/workflowApi'
import { getProjects } from '../api/projects'
import { listFiles } from '../api/files'

// 流程实例列表
const instances = ref([])
const loading = ref(false)
const selectedInstance = ref(null)
const history = ref([])
const showDetailDrawer = ref(false)

// 分页
const currentPage = ref(1)
const pageSize = ref(20)
const total = ref(0)

// 启动流程对话框
const showStartDialog = ref(false)
const definitions = ref([])
const searchText = ref('')
const selectedDefinition = ref(null)
const projectList = ref([])
const projectFilesMap = ref({})
const projectFileIdsMap = ref({})
const submitting = ref(false)
const startForm = ref({
  name: '',
  extraData: {}
})

// 开始节点的表单字段
const startNodeFormFields = computed(() => {
  if (!selectedDefinition.value) return []
  const startNode = selectedDefinition.value.nodes?.find(n => n.nodeType === 'Start')
  return startNode?.formFields || []
})

// 分类：项目字段
const projectFields = computed(() => {
  return startNodeFormFields.value.filter(f => f.fieldType === 'project')
})

// 分类：项目文件字段
const projectFileFields = computed(() => {
  return startNodeFormFields.value.filter(f => f.fieldType === 'projectFile')
})

// 分类：普通字段
const normalFields = computed(() => {
  return startNodeFormFields.value.filter(f =>
    f.fieldType !== 'project' && f.fieldType !== 'projectFile'
  )
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

// 获取项目字段关联的项目值
const getProjectFileSource = (field) => {
  const sourceKey = field.entitySourceKey
  if (!sourceKey) return null
  return startForm.value.extraData[sourceKey]
}

// 项目选择变化
const onProjectChange = async (fieldKey, projectId) => {
  if (!projectId) {
    for (const field of projectFileFields.value) {
      if (field.entitySourceKey === fieldKey) {
        projectFilesMap.value[field.key] = []
        projectFileIdsMap.value[field.key] = []
      }
    }
    return
  }
  try {
    const files = await buildFileTree('', projectId)
    for (const field of projectFileFields.value) {
      if (field.entitySourceKey === fieldKey) {
        projectFilesMap.value[field.key] = files
        projectFileIdsMap.value[field.key] = []
      }
    }
  } catch (error) {
    console.error('加载文件失败:', error)
  }
}

// 文件选择变化
const onFileChange = (fieldKey, node, checked) => {
  if (!projectFileIdsMap.value[fieldKey]) {
    projectFileIdsMap.value[fieldKey] = []
  }

  const fileId = node.id
  const isFolder = node.isFolder || String(node.id).startsWith('file-')

  if (checked) {
    if (!isFolder && !projectFileIdsMap.value[fieldKey].includes(fileId)) {
      projectFileIdsMap.value[fieldKey].push(fileId)
    }
  } else {
    const idx = projectFileIdsMap.value[fieldKey].indexOf(fileId)
    if (idx > -1) {
      projectFileIdsMap.value[fieldKey].splice(idx, 1)
    }
  }
}

// 构建文件树
const buildFileTree = async (path, projectId) => {
  try {
    const data = (await listFiles('projects', path || null, projectId)).data || []
    const items = []
    for (const item of data) {
      if (item.isFolder) {
        const node = {
          id: `file-${item.path}`,
          name: item.displayName || item.name,
          isFolder: true,
          path: item.path,
          children: await buildFileTree(item.path, projectId)
        }
        items.push(node)
      } else {
        const node = {
          id: item.id,
          name: item.originalName || item.name,
          isFolder: false,
          path: item.path
        }
        items.push(node)
      }
    }
    return items
  } catch (error) {
    console.error('构建文件树失败:', error)
    return []
  }
}

// 加载流程实例
const loadInstances = async () => {
  loading.value = true
  try {
    const data = await getMyWorkflowInstances()
    const allInstances = data.data || []
    total.value = allInstances.length
    // 前端分页
    const start = (currentPage.value - 1) * pageSize.value
    const end = start + pageSize.value
    instances.value = allInstances.slice(start, end)
  } catch (error) {
    ElMessage.error('加载流程记录失败: ' + error.message)
  } finally {
    loading.value = false
  }
}

// 分页处理
const handleSizeChange = () => {
  currentPage.value = 1
  loadInstances()
}

const handleCurrentChange = () => {
  loadInstances()
}

// 打开发起对话框
const openStartDialog = async () => {
  showStartDialog.value = true
  selectedDefinition.value = null
  startForm.value = { name: '', extraData: {} }
  projectFilesMap.value = {}
  projectFileIdsMap.value = {}

  if (definitions.value.length === 0) {
    try {
      definitions.value = (await getWorkflowDefinitions()).data || []
    } catch (error) {
      ElMessage.error('加载流程模板失败')
    }
  }

  if (projectList.value.length === 0) {
    try {
      const result = (await getProjects(null)).data || []
      projectList.value = result.filter(n => n.type === 'project')
    } catch (error) {
      console.error('加载项目失败:', error)
    }
  }
}

// 选择流程定义
const selectDefinition = async (def) => {
  try {
    const fullDef = (await getWorkflowDefinition(def.id)).data
    selectedDefinition.value = fullDef
    startForm.value = {
      name: fullDef.name + ' - 实例',
      extraData: {}
    }
    projectFilesMap.value = {}
    projectFileIdsMap.value = {}
  } catch (error) {
    ElMessage.error('加载流程详情失败: ' + error.message)
  }
}

// 提交流程
const submitWorkflow = async () => {
  if (!startForm.value.name) {
    ElMessage.warning('请输入流程名称')
    return
  }

  // 验证必填字段
  for (const field of startNodeFormFields.value) {
    if (field.required) {
      if (field.fieldType === 'project') {
        if (!startForm.value.extraData[field.key]) {
          ElMessage.warning(`请选择 ${field.label}`)
          return
        }
      } else if (field.fieldType === 'projectFile') {
        const fileIds = projectFileIdsMap.value[field.key] || []
        if (fileIds.length === 0) {
          ElMessage.warning(`请选择 ${field.label}`)
          return
        }
      } else {
        const value = startForm.value.extraData[field.key]
        if (!value && value !== 0 && value !== false) {
          ElMessage.warning(`请填写 ${field.label}`)
          return
        }
      }
    }
  }

  submitting.value = true
  try {
    const extraData = { ...startForm.value.extraData }
    for (const field of projectFileFields.value) {
      extraData[field.key] = projectFileIdsMap.value[field.key] || []
    }

    const firstProjectField = projectFields.value[0]
    const entityId = firstProjectField ? (startForm.value.extraData[firstProjectField.key] || '') : ''

    await startWorkflowInstance(
      selectedDefinition.value.id,
      entityId ? 'Project' : '',
      entityId,
      [],
      startForm.value.name,
      extraData
    )
    ElMessage.success('流程已启动')
    showStartDialog.value = false
    await loadInstances()
  } catch (error) {
    ElMessage.error('启动流程失败: ' + error.message)
  } finally {
    submitting.value = false
  }
}

// 点击表格行
const handleRowClick = (row) => {
  openDetail(row)
}

// 打开详情抽屉
const openDetail = async (row) => {
  selectedInstance.value = row
  history.value = []
  showDetailDrawer.value = true

  try {
    // 加载完整详情
    const detail = (await getWorkflowInstance(row.id)).data
    selectedInstance.value = detail

    // 加载审批历史
    const historyData = (await getWorkflowHistory(row.id)).data || []
    history.value = historyData
  } catch (error) {
    ElMessage.error('加载流程详情失败: ' + error.message)
  }
}

// 取消流程
const cancelInstance = async (row) => {
  try {
    await ElMessageBox.confirm('确定要取消此流程吗？', '提示', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })

    await cancelWorkflowInstance(row.id)
    ElMessage.success('流程已取消')
    await loadInstances()
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error('取消流程失败: ' + error.message)
    }
  }
}

// 格式化日期
const formatDate = (date) => {
  if (!date) return '-'
  return new Date(date).toLocaleString('zh-CN')
}

// 获取状态标签类型
const getStatusType = (status) => {
  const types = { Running: 'info', Completed: 'success', Rejected: 'danger', Cancelled: 'warning' }
  return types[status] || 'info'
}

// 获取历史类型
const getHistoryType = (action) => {
  const types = { Started: 'primary', Approved: 'success', Rejected: 'danger', Returned: 'warning' }
  return types[action] || 'info'
}

// 格式化表单数据值
const formatFormDataValue = (value) => {
  if (Array.isArray(value)) return value.join(', ')
  if (typeof value === 'object') return JSON.stringify(value)
  return String(value)
}

onMounted(() => {
  loadInstances()
})
</script>

<style scoped>
.workflow-container {
  height: calc(100vh - 120px);
  display: flex;
  flex-direction: column;
  padding: 20px;
  gap: 16px;
}

.toolbar {
  display: flex;
  gap: 12px;
}

.table-container {
  flex: 1;
  background: #fff;
  border-radius: 8px;
  padding: 16px;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.pagination {
  margin-top: 16px;
  display: flex;
  justify-content: flex-end;
}

/* 发起对话框 */
.start-dialog-content {
  display: flex;
  gap: 20px;
  min-height: 400px;
}

.definition-panel {
  width: 250px;
  border-right: 1px solid #eee;
  padding-right: 20px;
}

.definition-panel h4,
.form-panel h4 {
  margin: 0 0 12px 0;
  font-size: 14px;
  color: #333;
}

.definition-list {
  margin-top: 12px;
  max-height: 320px;
  overflow-y: auto;
}

.definition-item {
  padding: 10px 12px;
  border-radius: 6px;
  cursor: pointer;
  margin-bottom: 4px;
  border: 1px solid transparent;
  transition: all 0.2s;
}

.definition-item:hover {
  background: #f5f5f5;
}

.definition-item.active {
  background: #ecf5ff;
  border-color: #409eff;
}

.def-name {
  font-size: 13px;
  font-weight: 500;
  color: #333;
  margin-bottom: 2px;
}

.def-category {
  font-size: 11px;
  color: #999;
}

.form-panel {
  flex: 1;
  overflow-y: auto;
}

.no-selection {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
}

.file-tree-container {
  border: 1px solid #ddd;
  border-radius: 4px;
  padding: 8px;
  max-height: 150px;
  overflow-y: auto;
}

/* 详情抽屉 */
.detail-content {
  padding: 0 16px;
}

.history-section {
  margin-top: 24px;
}

.history-section h4 {
  margin: 0 0 16px 0;
  font-size: 14px;
  color: #333;
}

.history-item {
  padding: 4px 0;
}

.history-node {
  font-weight: 500;
  margin-bottom: 4px;
}

.history-action {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 4px;
}

.history-operator {
  font-size: 12px;
  color: #666;
}

.history-formdata {
  font-size: 12px;
  color: #666;
  margin: 4px 0;
  padding: 4px 8px;
  background: #f5f7fa;
  border-radius: 4px;
}

.formdata-key {
  font-weight: 500;
}

.formdata-value {
  margin-left: 4px;
}

.history-comment {
  font-size: 12px;
  color: #666;
  font-style: italic;
  margin-top: 4px;
}

.no-history {
  text-align: center;
  padding: 20px;
  color: #999;
}
</style>
