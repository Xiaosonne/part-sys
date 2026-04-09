<template>
  <div class="inbound-form">
    <!-- 入库类型切换 -->
    <el-radio-group v-model="inboundType" class="type-switch">
      <el-radio-button label="normal">普通入库</el-radio-button>
      <el-radio-button label="purchase">采购入库</el-radio-button>
      <el-radio-button label="return">归还入库</el-radio-button>
    </el-radio-group>

    <!-- 普通入库 -->
    <div v-if="inboundType === 'normal'" class="form-content">
      <el-form :model="normalForm" label-width="100px">
        <el-form-item label="配件" required>
          <el-select v-model="normalForm.partId" placeholder="选择配件" style="width: 100%;" @change="onPartChange">
            <el-option
              v-for="part in parts"
              :key="part.id"
              :label="part.name + ' - ' + part.model + ' (可用: ' + part.availableQty + ')'"
              :value="part.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="数量">
          <el-input-number v-model="normalForm.quantity" :min="1" :max="Infinity" />
        </el-form-item>
        <el-form-item label="备注">
          <el-input v-model="normalForm.note" placeholder="可选" />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleNormalInbound" :loading="submitting">提交入库</el-button>
        </el-form-item>
      </el-form>
    </div>

    <!-- 采购入库 - 从采购任务选择 -->
    <div v-if="inboundType === 'purchase'" class="form-content">
      <el-form :model="purchaseForm" label-width="100px">
        <el-form-item label="采购任务" required>
          <el-select v-model="purchaseForm.taskId" placeholder="选择采购任务" style="width: 100%;" @change="onTaskChange">
            <el-option
              v-for="task in purchaseTasks"
              :key="task.id"
              :label="task.name + ' (' + task.items.length + '种配件)'"
              :value="task.id"
            />
          </el-select>
        </el-form-item>
      </el-form>

      <!-- 配件信息（当前后端单配件模式） -->
      <div v-if="purchaseForm.taskId && selectedTaskPart" class="purchase-items">
        <div class="items-header">配件明细</div>
        <el-descriptions :column="2" border>
          <el-descriptions-item label="配件名称">{{ selectedTaskPart.partName }}</el-descriptions-item>
          <el-descriptions-item label="型号">{{ selectedTaskPart.partModel }}</el-descriptions-item>
          <el-descriptions-item label="需采购数量">{{ selectedTaskPart.requiredQty }}</el-descriptions-item>
          <el-descriptions-item label="原始锁定">{{ selectedTaskPart.lockedQty }}</el-descriptions-item>
        </el-descriptions>
      </div>

      <div class="form-actions">
        <el-button type="primary" @click="handlePurchaseInbound" :loading="submitting" :disabled="!purchaseForm.taskId">
          确认入库
        </el-button>
      </div>
    </div>

    <!-- 归还入库 -->
    <div v-if="inboundType === 'return'" class="form-content">
      <el-form :model="returnForm" label-width="100px">
        <el-form-item label="关联项目" required>
          <el-select v-model="returnForm.projectId" placeholder="选择项目" style="width: 100%;" @change="onReturnProjectChange">
            <el-option v-for="proj in projects" :key="proj.id" :label="proj.name" :value="proj.id" />
          </el-select>
        </el-form-item>
      </el-form>

      <!-- 锁定配件列表（选择项目后显示） -->
      <div v-if="returnForm.projectId && returnLocks.length > 0" class="return-locks">
        <div class="locks-header">该项目的锁定配件</div>
        <el-table
          :data="returnLocks"
          border
          stripe
          max-height="300"
          @selection-change="onReturnSelectionChange"
        >
          <el-table-column type="selection" width="50" />
          <el-table-column prop="partName" label="配件名称" min-width="150" />
          <el-table-column prop="partModel" label="型号" width="120" />
          <el-table-column prop="selectionPlanName" label="选型单" min-width="120">
            <template #default="{ row }">
              {{ row.selectionPlanName || '-' }}
            </template>
          </el-table-column>
          <el-table-column prop="lockedQty" label="已锁定" width="80" align="center">
            <template #default="{ row }">
              <el-tag type="warning" size="small">{{ row.lockedQty }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column label="归还数量" width="140">
            <template #default="{ row }">
              <el-input-number
                v-model="row.returnQty"
                :min="1"
                :max="row.lockedQty"
                size="small"
              />
            </template>
          </el-table-column>
        </el-table>

        <div class="return-actions">
          <span class="return-tip">选择要归还的配件，设置归还数量后提交</span>
          <el-button type="primary" @click="handleReturnInbound" :loading="submitting" :disabled="selectedReturnItems.length === 0">
            提交归还 ({{ selectedReturnItems.length }} 项)
          </el-button>
        </div>
      </div>

      <el-empty v-else-if="returnForm.projectId && returnLocks.length === 0" description="该项目暂无锁定配件" />

      <el-form v-if="returnForm.projectId" :model="returnForm" label-width="100px" style="margin-top: 16px;">
        <el-form-item label="备注">
          <el-input v-model="returnForm.note" placeholder="可选" />
        </el-form-item>
      </el-form>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { getParts } from '@/api/parts'
import { getProjects } from '@/api/projects'
import { inbound, returnStock, getStockLocks } from '@/api/stock'
import { getPurchaseTasks, receivePurchaseTask } from '@/api/purchaseTasks'

const loading = ref(false)
const submitting = ref(false)
const parts = ref([])
const projects = ref([])
const purchaseTasks = ref([])

const inboundType = ref('normal')

// 普通入库
const normalForm = ref({
  partId: '',
  quantity: 1,
  note: ''
})

// 采购入库
const purchaseForm = ref({
  taskId: '',
  items: []
})

const purchaseItems = computed(() => purchaseForm.value.items || [])

// 当前选中任务的配件信息（单配件模式）
const selectedTaskPart = computed(() => {
  const task = purchaseTasks.value.find(t => t.id === purchaseForm.value.taskId)
  if (!task) return null
  return {
    partName: task.partName,
    partModel: task.partModel || '-',
    requiredQty: task.requiredQty,
    lockedQty: task.lockedQty
  }
})

// 归还入库
const returnForm = ref({
  projectId: '',
  note: ''
})
const returnLocks = ref([])
const selectedReturnItems = ref([])

onMounted(async () => {
  await loadParts()
  await loadProjects()
  await loadPurchaseTasks()
})

const loadParts = async () => {
  try {
    const res = await getParts()
    parts.value = res.data || []
  } catch (error) {
    console.error('加载配件列表失败', error)
  }
}

const loadProjects = async () => {
  try {
    const res = await getProjects()
    projects.value = res.data || []
  } catch (error) {
    console.error('加载项目列表失败', error)
  }
}

const loadPurchaseTasks = async () => {
  try {
    const res = await getPurchaseTasks()
    // 筛选 Pending 和 InProgress 状态的任务
    purchaseTasks.value = (res.data || []).filter(t =>
      t.status === 'Pending' || t.status === 'InProgress'
    ).map(t => ({
      ...t,
      name: `采购: ${t.partName} (${t.requiredQty})`
    }))
  } catch (error) {
    console.error('加载采购任务失败', error)
  }
}

const onPartChange = (partId) => {
  normalForm.value.quantity = 1
}

const onTaskChange = (taskId) => {
  // 重置 items 数组（当前后端单配件）
  purchaseForm.value.items = []
}

// 归还入库 - 项目选择变化
const onReturnProjectChange = async (projectId) => {
  returnLocks.value = []
  selectedReturnItems.value = []
  if (!projectId) return

  try {
    const res = await getStockLocks()
    const allLocks = res.data || []
    // 过滤出该项目的锁定，并展开 locks 数组
    const projectLocks = []
    allLocks.forEach(part => {
      (part.locks || []).forEach(lock => {
        if (lock.projectId === projectId) {
          projectLocks.push({
            partId: part.partId,
            partName: part.partName,
            partModel: part.partModel,
            selectionPlanId: lock.selectionPlanId,
            selectionPlanName: lock.selectionPlanName,
            lockedQty: lock.lockedQty,
            returnQty: 1
          })
        }
      })
    })
    returnLocks.value = projectLocks
  } catch (error) {
    console.error('加载项目锁定失败', error)
  }
}

// 归还入库 - 选择变化
const onReturnSelectionChange = (selection) => {
  selectedReturnItems.value = selection
}

// 提交归还
const handleReturnInbound = async () => {
  if (selectedReturnItems.value.length === 0) {
    ElMessage.warning('请选择要归还的配件')
    return
  }
  submitting.value = true
  try {
    for (const item of selectedReturnItems.value) {
      await returnStock({
        partId: item.partId,
        quantity: item.returnQty,
        projectId: returnForm.value.projectId,
        selectionPlanId: item.selectionPlanId || null,
        note: returnForm.value.note || null
      })
    }
    ElMessage.success('归还成功')
    returnForm.value = { projectId: '', note: '' }
    returnLocks.value = []
    selectedReturnItems.value = []
  } catch (error) {
    ElMessage.error(error.response?.data?.message || '归还失败')
  } finally {
    submitting.value = false
  }
}

const handleNormalInbound = async () => {
  if (!normalForm.value.partId) {
    ElMessage.warning('请选择配件')
    return
  }
  submitting.value = true
  try {
    await inbound({
      partId: normalForm.value.partId,
      quantity: normalForm.value.quantity,
      sourceType: 0, // Manual
      note: normalForm.value.note || null
    })
    ElMessage.success('入库成功')
    normalForm.value = { partId: '', quantity: 1, note: '' }
    await loadParts()
  } catch (error) {
    ElMessage.error(error.response?.data?.message || '入库失败')
  } finally {
    submitting.value = false
  }
}

const handlePurchaseInbound = async () => {
  if (!purchaseForm.value.taskId) {
    ElMessage.warning('请选择采购任务')
    return
  }
  submitting.value = true
  try {
    // 当前后端只支持单个配件入库，多配件需要后端增强
    const task = purchaseTasks.value.find(t => t.id === purchaseForm.value.taskId)
    if (task) {
      await receivePurchaseTask(purchaseForm.value.taskId, {
        remark: `采购入库: ${task.partName} x ${task.requiredQty}`
      })
    }
    ElMessage.success('采购入库成功')
    purchaseForm.value = { taskId: '', items: [] }
    await loadPurchaseTasks()
  } catch (error) {
    ElMessage.error(error.response?.data?.message || '入库失败')
  } finally {
    submitting.value = false
  }
}
</script>

<style scoped>
.inbound-form {
  padding: 20px;
  width: 100%;
}

.type-switch {
  margin-bottom: 20px;
}

.form-content {
  background: var(--color-bg-card);
  border: 1px solid var(--color-border);
  border-radius: var(--radius);
  padding: 24px;
}

.purchase-items {
  margin-top: 20px;
}

.items-header {
  font-size: 14px;
  font-weight: 600;
  color: var(--color-text-primary);
  margin-bottom: 12px;
}

.form-actions {
  margin-top: 20px;
  padding-top: 16px;
  border-top: 1px solid var(--color-border);
}

.return-locks {
  margin-top: 16px;
  border: 1px solid var(--color-border);
  border-radius: var(--radius);
  overflow: hidden;
}

.locks-header {
  font-size: 14px;
  font-weight: 600;
  color: var(--color-text-primary);
  padding: 12px 16px;
  background: var(--color-bg-page);
  border-bottom: 1px solid var(--color-border);
}

.return-actions {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 12px 16px;
  background: var(--color-bg-page);
  border-top: 1px solid var(--color-border);
}

.return-tip {
  font-size: 13px;
  color: var(--color-text-secondary);
}
</style>
