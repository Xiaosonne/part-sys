<template>
  <div class="page-main">
    <div style="padding: 20px 24px; flex: 1;">
      <!-- Filter Bar -->
      <div class="panel-card" style="margin-bottom: 16px;">
        <div style="padding: 12px 16px; display: flex; align-items: center; gap: 16px; flex-wrap: wrap;">
          <span style="font-size: 13px; font-weight: 600; color: var(--color-text-secondary);">状态筛选:</span>
          <el-radio-group v-model="statusFilter" size="small">
            <el-radio-button label="">全部</el-radio-button>
            <el-radio-button label="Pending">待采购</el-radio-button>
            <el-radio-button label="InProgress">采购中</el-radio-button>
            <el-radio-button label="Received">已到货</el-radio-button>
            <el-radio-button label="Cancelled">已取消</el-radio-button>
          </el-radio-group>
        </div>
      </div>

      <!-- Task Table -->
      <div class="panel-card">
        <el-table :data="filteredTasks" stripe v-loading="loading" style="width: 100%;">
          <el-table-column prop="partName" label="配件名称" min-width="150" />
          <el-table-column prop="selectionPlanId" label="选型单" width="200" />
          <el-table-column prop="lockedQty" label="锁定数量" width="90" align="center" />
          <el-table-column prop="requiredQty" label="需采购数量" width="100" align="center">
            <template #default="{ row }">
              <span class="qty-pending">{{ row.requiredQty }}</span>
            </template>
          </el-table-column>
          <el-table-column prop="status" label="状态" width="110">
            <template #default="{ row }">
              <el-tag size="small" :type="statusType(row.status)">{{ statusText(row.status) }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column prop="createdAt" label="创建时间" width="170" />
          <el-table-column prop="remark" label="备注" min-width="120" show-overflow-tooltip />
          <el-table-column label="操作" width="220" fixed="right">
            <template #default="{ row }">
              <el-button size="small" type="primary" plain @click="startTask(row)" :disabled="row.status !== 'Pending'">开始采购</el-button>
              <el-button size="small" type="success" plain @click="receiveTask(row)" :disabled="row.status !== 'InProgress'">确认到货</el-button>
              <el-button size="small" type="danger" plain @click="cancelTask(row)" :disabled="row.status === 'Received' || row.status === 'Cancelled'">取消</el-button>
            </template>
          </el-table-column>
        </el-table>
        <el-empty v-if="!loading && filteredTasks.length === 0" description="暂无采购任务" />
      </div>
    </div>

    <!-- Remark Dialog -->
    <el-dialog v-model="showRemarkDialog" title="填写备注" width="400px">
      <el-form :model="remarkForm" label-width="80px">
        <el-form-item label="备注">
          <el-input v-model="remarkForm.remark" type="textarea" :rows="3" placeholder="可选" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showRemarkDialog = false">取消</el-button>
        <el-button type="primary" @click="confirmAction">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import request from '@/api/request'

const tasks = ref([])
const loading = ref(false)
const statusFilter = ref('')

// Remark dialog
const showRemarkDialog = ref(false)
const remarkForm = ref({ remark: '', taskId: '', action: '' })

// Computed
const filteredTasks = computed(() => {
  if (!statusFilter.value) return tasks.value
  return tasks.value.filter(t => t.status === statusFilter.value)
})

// Status helpers
const statusType = (status) => {
  const map = {
    'Pending': 'warning',
    'InProgress': 'primary',
    'Received': 'success',
    'Cancelled': 'info'
  }
  return map[status] || 'info'
}

const statusText = (status) => {
  const map = {
    'Pending': '待采购',
    'InProgress': '采购中',
    'Received': '已到货',
    'Cancelled': '已取消'
  }
  return map[status] || status
}

const formatDate = (d) => d ? new Date(d).toLocaleString('zh-CN') : ''

// Lifecycle
onMounted(async () => {
  await loadTasks()
})

// Methods
const loadTasks = async () => {
  loading.value = true
  try {
    const res = await request.get('/api/purchase-tasks')
    tasks.value = res.data || []
  } catch (e) {
    ElMessage.error('加载采购任务失败')
  } finally {
    loading.value = false
  }
}

const startTask = async (task) => {
  try {
    await ElMessageBox.confirm(`确认开始采购 "${task.partName}" x${task.requiredQty}？`, '开始采购', { type: 'info' })
    await request.post(`/api/purchase-tasks/${task.id}/start`)
    ElMessage.success('已开始采购')
    await loadTasks()
  } catch (e) {
    if (e !== 'cancel') {
      ElMessage.error(e.response?.data?.message || '操作失败')
    }
  }
}

const receiveTask = async (task) => {
  try {
    await ElMessageBox.confirm(`确认 "${task.partName}" x${task.requiredQty} 已到货？`, '确认到货', { type: 'success' })
    await request.post(`/api/purchase-tasks/${task.id}/receive`)
    ElMessage.success('已标记为已到货')
    await loadTasks()
  } catch (e) {
    if (e !== 'cancel') {
      ElMessage.error(e.response?.data?.message || '操作失败')
    }
  }
}

const cancelTask = async (task) => {
  try {
    await ElMessageBox.confirm(`确认取消采购任务 "${task.partName}"？`, '取消任务', { type: 'warning' })
    await request.post(`/api/purchase-tasks/${task.id}/cancel`)
    ElMessage.success('已取消')
    await loadTasks()
  } catch (e) {
    if (e !== 'cancel') {
      ElMessage.error(e.response?.data?.message || '操作失败')
    }
  }
}
</script>

<style scoped>
.purchase-tasks-container {
  padding: 20px;
}
.header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
}
.header h2 {
  margin: 0;
}
.task-list {
  background: #fff;
  border-radius: 8px;
  padding: 20px;
}
.qty-pending {
  color: #e6a23c;
  font-weight: bold;
}
</style>
