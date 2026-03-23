<template>
  <div class="pending-tasks">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <span>审批任务</span>
          <el-button @click="refreshTasks">刷新</el-button>
        </div>
      </template>

      <el-tabs v-model="activeTab" class="task-tabs" align="center" @tab-change="handleTabChange">
        <!-- 待审批 Tab -->
        <el-tab-pane label="待审批" name="pending">
          <div class="tab-toolbar">
            <el-select v-model="pendingFilter" placeholder="筛选" clearable size="default" style="width: 150px;">
              <el-option label="全部节点" value="" />
              <el-option label="第一级审批" value="第一级审批" />
              <el-option label="第二级审批" value="第二级审批" />
            </el-select>
            <span class="task-count">共 {{ filteredPendingTasks.length }} 条</span>
          </div>
          <el-table :data="paginatedPendingTasks" stripe style="width: 100%;" v-loading="loading">
            <el-table-column prop="instanceName" label="流程名称" min-width="150" show-overflow-tooltip />
            <el-table-column prop="nodeName" label="审批节点" min-width="120" />
            <el-table-column label="发起人" min-width="100">
              <template #default="{ row }">
                {{ row.startedByName || row.startedBy }}
              </template>
            </el-table-column>
            <el-table-column label="审批人" min-width="100">
              <template #default="{ row }">
                {{ row.assigneeName || row.assigneeId }}
              </template>
            </el-table-column>
            <el-table-column label="创建时间" min-width="160">
              <template #default="{ row }">
                {{ formatDate(row.createdAt) }}
              </template>
            </el-table-column>
            <el-table-column label="截止时间" min-width="160">
              <template #default="{ row }">
                {{ formatDate(row.dueDate) }}
              </template>
            </el-table-column>
            <el-table-column label="操作" width="180" fixed="right">
              <template #default="{ row }">
                <el-button link type="primary" @click="viewDetail(row)">查看</el-button>
                <el-button link type="success" @click="openApprovalDialog(row)">通过</el-button>
                <el-button link type="danger" @click="openRejectDialog(row)">拒绝</el-button>
              </template>
            </el-table-column>
            <template #empty>
              <div style="padding: 40px; color: #999;">
                暂无待审批任务
              </div>
            </template>
          </el-table>
          <div class="pagination-container">
            <el-pagination
              v-model:current-page="pendingCurrentPage"
              v-model:page-size="pendingPageSize"
              :page-sizes="[5, 10, 20, 50]"
              :total="filteredPendingTasks.length"
              layout="total, sizes, prev, pager, next, jumper"
              background
            />
          </div>
        </el-tab-pane>

        <!-- 历史记录 Tab -->
        <el-tab-pane label="历史记录" name="history">
          <div class="tab-toolbar">
            <el-select v-model="historyFilter" placeholder="筛选状态" clearable size="default" style="width: 150px;">
              <el-option label="全部" value="" />
              <el-option label="已通过" value="Approved" />
              <el-option label="已拒绝" value="Rejected" />
            </el-select>
            <span class="task-count">共 {{ filteredHistoryTasks.length }} 条</span>
          </div>
          <el-table :data="paginatedHistoryTasks" stripe style="width: 100%;" v-loading="historyLoading">
            <el-table-column prop="instanceName" label="流程名称" min-width="150" show-overflow-tooltip />
            <el-table-column prop="nodeName" label="审批节点" min-width="120" />
            <el-table-column label="发起人" min-width="100">
              <template #default="{ row }">
                {{ row.startedByName || row.startedBy }}
              </template>
            </el-table-column>
            <el-table-column label="审批人" min-width="100">
              <template #default="{ row }">
                {{ row.assigneeName || row.assigneeId }}
              </template>
            </el-table-column>
            <el-table-column label="状态" width="90">
              <template #default="{ row }">
                <el-tag :type="row.status === 'Approved' ? 'success' : 'danger'">
                  {{ row.status === 'Approved' ? '已通过' : '已拒绝' }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column label="完成时间" min-width="160">
              <template #default="{ row }">
                {{ formatDate(row.completedAt) }}
              </template>
            </el-table-column>
            <el-table-column label="审批意见" min-width="150" show-overflow-tooltip>
              <template #default="{ row }">
                {{ row.comment || '-' }}
              </template>
            </el-table-column>
            <el-table-column label="操作" width="100" fixed="right">
              <template #default="{ row }">
                <el-button link type="primary" @click="viewDetail(row)">查看</el-button>
              </template>
            </el-table-column>
            <template #empty>
              <div style="padding: 40px; color: #999;">
                暂无历史记录
              </div>
            </template>
          </el-table>
          <div class="pagination-container">
            <el-pagination
              v-model:current-page="historyCurrentPage"
              v-model:page-size="historyPageSize"
              :page-sizes="[5, 10, 20, 50]"
              :total="filteredHistoryTasks.length"
              layout="total, sizes, prev, pager, next, jumper"
              background
            />
          </div>
        </el-tab-pane>
      </el-tabs>
    </el-card>

    <!-- 审批对话框 -->
    <el-dialog v-model="showApprovalDialog" title="审批任务" width="800px">
      <!-- 项目信息 -->
      <el-descriptions v-if="selectedTask?.instance" :column="2" border style="margin-bottom: 20px;">
        <el-descriptions-item label="流程名称">{{ selectedTask.instance.name }}</el-descriptions-item>
        <el-descriptions-item label="状态">
          <el-tag :type="getStatusType(selectedTask.instance.status)">{{ selectedTask.instance.status }}</el-tag>
        </el-descriptions-item>
        <el-descriptions-item label="发起人">{{ selectedTask.instance.startedByName }}</el-descriptions-item>
        <el-descriptions-item label="发起时间">{{ formatDate(selectedTask.instance.startedAt) }}</el-descriptions-item>
      </el-descriptions>

      <el-form :model="approvalForm" label-width="100px">
        <!-- 审批表单字段 -->
        <div v-if="approvalFormFields.length > 0" style="border-bottom: 1px solid #ddd; padding-bottom: 20px; margin-bottom: 20px;">
          <div style="font-weight: bold; margin-bottom: 15px;">审批信息</div>
          <el-form-item
            v-for="field in approvalFormFields"
            :key="field.id"
            :label="field.label"
            :required="field.required"
          >
            <el-input
              v-if="field.fieldType === 'text' || field.fieldType === 'textarea'"
              v-model="approvalForm.formData[field.key]"
              :type="field.fieldType === 'textarea' ? 'textarea' : 'text'"
              :placeholder="field.placeholder"
              :rows="field.fieldType === 'textarea' ? 4 : 1"
            />
            <el-select
              v-else-if="field.fieldType === 'select'"
              v-model="approvalForm.formData[field.key]"
              :placeholder="field.placeholder"
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
              v-model="approvalForm.formData[field.key]"
            />
            <el-checkbox
              v-else-if="field.fieldType === 'checkbox'"
              v-model="approvalForm.formData[field.key]"
            />
          </el-form-item>
        </div>

        <el-form-item label="审批意见">
          <el-input v-model="approvalForm.comment" type="textarea" rows="4" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showApprovalDialog = false">取消</el-button>
        <el-button type="primary" @click="submitApproval">通过</el-button>
      </template>
    </el-dialog>

    <!-- 拒绝对话框 -->
    <el-dialog v-model="showRejectDialog" title="拒绝任务" width="600px">
      <el-form :model="rejectForm" label-width="100px">
        <el-form-item label="拒绝原因">
          <el-input v-model="rejectForm.comment" type="textarea" rows="4" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showRejectDialog = false">取消</el-button>
        <el-button type="danger" @click="submitReject">拒绝</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { getPendingTasks, getHistoryTasks, approveTask, rejectTask } from '../services/workflowApi'

const router = useRouter()
const activeTab = ref('pending')
const pendingTasks = ref([])
const historyTasks = ref([])
const loading = ref(false)
const historyLoading = ref(false)
const selectedTask = ref(null)
const showApprovalDialog = ref(false)
const showRejectDialog = ref(false)
const approvalForm = reactive({ comment: '', formData: {} })
const rejectForm = reactive({ comment: '' })
const approvalFormFields = ref([])

// 分页和筛选状态 - 待审批
const pendingCurrentPage = ref(1)
const pendingPageSize = ref(10)
const pendingFilter = ref('')

// 分页和筛选状态 - 历史记录
const historyCurrentPage = ref(1)
const historyPageSize = ref(10)
const historyFilter = ref('')

// 筛选后的待审批任务
const filteredPendingTasks = computed(() => {
  if (!pendingFilter.value) return pendingTasks.value
  return pendingTasks.value.filter(t => t.nodeName === pendingFilter.value)
})

// 筛选后的历史任务
const filteredHistoryTasks = computed(() => {
  if (!historyFilter.value) return historyTasks.value
  return historyTasks.value.filter(t => t.status === historyFilter.value)
})

// 分页后的待审批任务
const paginatedPendingTasks = computed(() => {
  const start = (pendingCurrentPage.value - 1) * pendingPageSize.value
  const end = start + pendingPageSize.value
  return filteredPendingTasks.value.slice(start, end)
})

// 分页后的历史任务
const paginatedHistoryTasks = computed(() => {
  const start = (historyCurrentPage.value - 1) * historyPageSize.value
  const end = start + historyPageSize.value
  return filteredHistoryTasks.value.slice(start, end)
})

onMounted(() => {
  loadPendingTasks()
})

const loadPendingTasks = async () => {
  loading.value = true
  try {
    pendingTasks.value = (await getPendingTasks()).data || []
    // 重置到第一页
    pendingCurrentPage.value = 1
  } catch (error) {
    ElMessage.error('加载待审批任务失败: ' + error.message)
  } finally {
    loading.value = false
  }
}

const loadHistoryTasks = async () => {
  historyLoading.value = true
  try {
    historyTasks.value = (await getHistoryTasks()).data || []
    // 重置到第一页
    historyCurrentPage.value = 1
  } catch (error) {
    ElMessage.error('加载历史记录失败: ' + error.message)
  } finally {
    historyLoading.value = false
  }
}

const refreshTasks = () => {
  if (activeTab.value === 'pending') {
    loadPendingTasks()
  } else {
    loadHistoryTasks()
  }
}

const handleTabChange = (tabName) => {
  if (tabName === 'pending') {
    loadPendingTasks()
  } else {
    loadHistoryTasks()
  }
}

const viewDetail = (row) => {
  router.push(`/workflows/detail/${row.instanceId}`)
}

const openApprovalDialog = (task) => {
  selectedTask.value = task
  approvalForm.comment = ''
  approvalForm.formData = {}

  // 获取当前节点的表单字段
  const instance = task.instance
  const currentNode = instance?.definition?.nodes?.find(n => n.id === task.nodeId)
  approvalFormFields.value = currentNode?.formFields || []

  showApprovalDialog.value = true
}

const openRejectDialog = (task) => {
  selectedTask.value = task
  rejectForm.comment = ''
  showRejectDialog.value = true
}

const submitApproval = async () => {
  // 验证必填字段
  for (const field of approvalFormFields.value) {
    if (field.required && !approvalForm.formData[field.key]) {
      ElMessage.warning(`请填写${field.label}`)
      return
    }
  }

  try {
    await approveTask(selectedTask.value.id, approvalForm.comment, approvalForm.formData)
    ElMessage.success('审批通过')
    showApprovalDialog.value = false
    loadPendingTasks()
  } catch (error) {
    ElMessage.error('审批失败: ' + error.message)
  }
}

const submitReject = async () => {
  try {
    await rejectTask(selectedTask.value.id, rejectForm.comment)
    ElMessage.success('已拒绝')
    showRejectDialog.value = false
    loadPendingTasks()
  } catch (error) {
    ElMessage.error('拒绝失败: ' + error.message)
  }
}

const formatDate = (date) => {
  if (!date) return '-'
  return new Date(date).toLocaleString('zh-CN')
}

const getStatusType = (status) => {
  const types = { Running: 'info', Completed: 'success', Rejected: 'danger', Cancelled: 'warning' }
  return types[status] || 'info'
}
</script>

<style scoped>
.pending-tasks {
  padding: 20px;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.task-tabs {
  width: 100%;
}

.tab-toolbar {
  display: flex;
  align-items: center;
  gap: 15px;
  margin-bottom: 15px;
}

.task-count {
  color: #909399;
  font-size: 14px;
}

.pagination-container {
  display: flex;
  justify-content: center;
  margin-top: 20px;
}

:deep(.el-tabs__item) {
  font-size: 16px;
}

:deep(.el-tabs__content) {
  padding-top: 20px;
}
</style>
