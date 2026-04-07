<template>
  <div class="selection-container">
    <!-- Header -->
    <div class="header">
      <h2>选型中心</h2>
      <el-button type="primary" @click="createNewPlan">新建选型单</el-button>
    </div>

    <!-- Plan List -->
    <div v-if="!currentPlan" class="plan-list">
      <el-table :data="selections" border stripe>
        <el-table-column prop="name" label="名称" width="180" />
        <el-table-column prop="projectId" label="项目ID" width="200" />
        <el-table-column prop="status" label="状态" width="120">
          <template #default="{ row }">
            <el-tag :type="statusType(row.status)">{{ statusText(row.status) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="createdAt" label="创建时间" width="180">
          <template #default="{ row }">
            {{ formatDate(row.createdAt) }}
          </template>
        </el-table-column>
        <el-table-column label="配件项" width="100">
          <template #default="{ row }">
            {{ row.items?.length || 0 }}
          </template>
        </el-table-column>
        <el-table-column label="操作" min-width="280">
          <template #default="{ row }">
            <el-button size="small" @click="openPlan(row)">查看</el-button>
            <el-button size="small" type="warning" @click="submitPlan(row)" :disabled="row.status !== 'Draft'">提交</el-button>
            <el-button size="small" type="danger" @click="cancelPlan(row)" :disabled="row.status !== 'Submitted'">取消</el-button>
            <el-button size="small" type="danger" plain @click="deletePlan(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </div>

    <!-- Plan Detail / Edit -->
    <div v-else class="plan-detail">
      <div class="detail-header">
        <el-button @click="closePlan">返回列表</el-button>
        <h3>{{ isNewPlan ? '新建选型单' : currentPlan.name }}</h3>
        <el-tag :type="statusType(currentPlan.status)">{{ statusText(currentPlan.status) }}</el-tag>
      </div>

      <!-- Plan Info -->
      <el-card class="info-card" v-if="!isNewPlan">
        <el-descriptions :column="2" border>
          <el-descriptions-item label="名称">{{ currentPlan.name }}</el-descriptions-item>
          <el-descriptions-item label="项目">{{ currentPlan.projectId }}</el-descriptions-item>
          <el-descriptions-item label="状态">
            <el-tag :type="statusType(currentPlan.status)">{{ statusText(currentPlan.status) }}</el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="创建时间">{{ formatDate(currentPlan.createdAt) }}</el-descriptions-item>
        </el-descriptions>
      </el-card>

      <!-- Create New Plan Form -->
      <el-card v-if="isNewPlan" class="form-card">
        <el-form :model="newPlanForm" label-width="100px">
          <el-form-item label="名称">
            <el-input v-model="newPlanForm.name" placeholder="选型单名称" />
          </el-form-item>
          <el-form-item label="项目ID">
            <el-select v-model="newPlanForm.projectId" placeholder="选择项目" filterable>
              <el-option v-for="p in projects" :key="p.id" :label="p.name" :value="p.id" />
            </el-select>
          </el-form-item>
          <el-form-item>
            <el-button type="primary" @click="saveNewPlan">创建</el-button>
            <el-button @click="closePlan">取消</el-button>
          </el-form-item>
        </el-form>
      </el-card>

      <!-- Items -->
      <el-card v-if="!isNewPlan" class="items-card">
        <template #header>
          <div class="card-header">
            <span>选型配件</span>
            <el-button type="primary" size="small" @click="showAddItem" :disabled="currentPlan.status !== 'Draft'">
              添加配件
            </el-button>
          </div>
        </template>

        <el-table :data="currentPlan.items" border stripe>
          <el-table-column prop="partName" label="配件名称" width="150" />
          <el-table-column prop="category" label="分类" width="120" />
          <el-table-column prop="requiredQty" label="需求数量" width="100" />
          <el-table-column prop="lockedQty" label="已锁定" width="90">
            <template #default="{ row }">
              <span class="qty-locked">{{ row.lockedQty || 0 }}</span>
            </template>
          </el-table-column>
          <el-table-column prop="outboundQty" label="已出库" width="90">
            <template #default="{ row }">
              <span class="qty-outbound">{{ row.outboundQty || 0 }}</span>
            </template>
          </el-table-column>
          <el-table-column prop="pendingQty" label="待采购" width="90">
            <template #default="{ row }">
              <span class="qty-pending">{{ row.pendingQty || 0 }}</span>
            </template>
          </el-table-column>
          <el-table-column prop="status" label="状态" width="120">
            <template #default="{ row }">
              <el-tag size="small" :type="itemStatusType(row)">{{ itemStatusText(row) }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column label="操作" min-width="200">
            <template #default="{ row, $index }">
              <template v-if="currentPlan.status === 'Draft'">
                <el-button size="small" @click="selectPart(row)">选择配件</el-button>
                <el-button size="small" type="danger" plain @click="removeItem($index)">删除</el-button>
              </template>
              <template v-else-if="currentPlan.status === 'Submitted'">
                <el-button size="small" type="success" @click="showOutbound(row)" :disabled="row.lockedQty <= 0">
                  出库
                </el-button>
              </template>
            </template>
          </el-table-column>
        </el-table>

        <!-- Summary -->
        <div class="items-summary" v-if="currentPlan.items?.length > 0">
          <span>总计: {{ currentPlan.items.length }} 项</span>
          <span>已锁定: {{ totalLocked }}</span>
          <span>已出库: {{ totalOutbound }}</span>
          <span>待采购: {{ totalPending }}</span>
        </div>

        <!-- Submit / Cancel Actions -->
        <div class="actions" v-if="currentPlan.status === 'Draft' && currentPlan.items?.length > 0">
          <el-button type="primary" size="large" @click="submitPlan(currentPlan)">提交选型单</el-button>
        </div>
        <div class="actions" v-if="currentPlan.status === 'Submitted'">
          <el-button type="warning" size="large" @click="cancelPlan(currentPlan)">取消选型单</el-button>
        </div>
      </el-card>
    </div>

    <!-- Add Item Dialog -->
    <el-dialog v-model="showItemDialog" title="添加配件" width="500px">
      <el-form :model="itemForm" label-width="100px">
        <el-form-item label="配件名称">
          <el-input v-model="itemForm.partName" placeholder="配件名称" />
        </el-form-item>
        <el-form-item label="分类">
          <el-input v-model="itemForm.category" placeholder="分类" />
        </el-form-item>
        <el-form-item label="需求数量">
          <el-input-number v-model="itemForm.requiredQty" :min="1" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showItemDialog = false">取消</el-button>
        <el-button type="primary" @click="addItem">添加</el-button>
      </template>
    </el-dialog>

    <!-- Select Part Dialog -->
    <el-dialog v-model="showPartDialog" title="选择配件" width="700px">
      <div class="part-filter">
        <el-input v-model="partFilter" placeholder="搜索配件名称/型号" clearable />
      </div>
      <el-table :data="filteredParts" border stripe max-height="400" @row-click="choosePart">
        <el-table-column prop="name" label="名称" />
        <el-table-column prop="model" label="型号" width="120" />
        <el-table-column prop="brand" label="品牌" width="100" />
        <el-table-column prop="availableQty" label="可用库存" width="100">
          <template #default="{ row }">
            <span :class="{ 'qty-low': row.availableQty < 5 }">{{ row.availableQty }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="totalQty" label="总库存" width="100" />
      </el-table>
    </el-dialog>

    <!-- Outbound Dialog -->
    <el-dialog v-model="showOutboundDialog" title="配件出库" width="400px">
      <el-form :model="outboundForm" label-width="100px">
        <el-form-item label="配件">
          <span>{{ outboundForm.partName }}</span>
        </el-form-item>
        <el-form-item label="可出库数量">
          <span>{{ outboundForm.availableQty }}</span>
        </el-form-item>
        <el-form-item label="出库数量">
          <el-input-number v-model="outboundForm.qty" :min="1" :max="outboundForm.availableQty" />
        </el-form-item>
        <el-form-item label="领用人ID">
          <el-input v-model="outboundForm.recipientId" />
        </el-form-item>
        <el-form-item label="领用人">
          <el-input v-model="outboundForm.recipientName" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showOutboundDialog = false">取消</el-button>
        <el-button type="primary" @click="doOutbound">确认出库</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import {
  getSelections, getSelection, createSelection, updateSelection,
  deleteSelection, submitSelection, outboundSelection, cancelSelection, matchParts
} from '@/api/selections'
import { getProjects } from '@/api/projects'
import { getParts } from '@/api/parts'

const selections = ref([])
const projects = ref([])
const currentPlan = ref(null)
const isNewPlan = ref(false)
const allParts = ref([])

// New plan form
const newPlanForm = ref({ name: '', projectId: '' })

// Item dialog
const showItemDialog = ref(false)
const itemForm = ref({ partName: '', category: '', requiredQty: 1 })

// Part dialog
const showPartDialog = ref(false)
const partFilter = ref('')
const currentItem = ref(null)

// Outbound dialog
const showOutboundDialog = ref(false)
const outboundForm = ref({ itemId: '', partName: '', qty: 1, availableQty: 0, recipientId: '', recipientName: '' })

// Computed
const totalLocked = computed(() => currentPlan.value?.items?.reduce((s, i) => s + (i.lockedQty || 0), 0) || 0)
const totalOutbound = computed(() => currentPlan.value?.items?.reduce((s, i) => s + (i.outboundQty || 0), 0) || 0)
const totalPending = computed(() => currentPlan.value?.items?.reduce((s, i) => s + (i.pendingQty || 0), 0) || 0)

const filteredParts = computed(() => {
  if (!partFilter.value) return allParts.value
  const f = partFilter.value.toLowerCase()
  return allParts.value.filter(p =>
    p.name?.toLowerCase().includes(f) || p.model?.toLowerCase().includes(f)
  )
})

// Lifecycle
onMounted(async () => {
  await loadSelections()
  await loadProjects()
  await loadParts()
})

// Methods
const loadSelections = async () => {
  try {
    const res = await getSelections()
    selections.value = res.data || []
  } catch (e) {
    ElMessage.error('加载选型单失败')
  }
}

const loadProjects = async () => {
  try {
    const res = await getProjects()
    projects.value = res.data || []
  } catch (e) {
    console.error(e)
  }
}

const loadParts = async () => {
  try {
    const res = await getParts()
    allParts.value = res.data || []
  } catch (e) {
    console.error(e)
  }
}

const createNewPlan = () => {
  currentPlan.value = {}
  isNewPlan.value = true
}

const saveNewPlan = async () => {
  if (!newPlanForm.value.name || !newPlanForm.value.projectId) {
    ElMessage.warning('请填写名称和选择项目')
    return
  }
  try {
    const res = await createSelection({
      name: newPlanForm.value.name,
      projectId: newPlanForm.value.projectId,
      items: []
    })
    isNewPlan.value = false
    await openPlan(res.data)
    ElMessage.success('创建成功')
  } catch (e) {
    ElMessage.error('创建失败')
  }
}

const openPlan = async (plan) => {
  try {
    const res = await getSelection(plan.id)
    currentPlan.value = res.data
    isNewPlan.value = false
  } catch (e) {
    ElMessage.error('加载选型单失败')
  }
}

const closePlan = () => {
  currentPlan.value = null
  isNewPlan.value = false
  newPlanForm.value = { name: '', projectId: '' }
}

const submitPlan = async (plan) => {
  try {
    await ElMessageBox.confirm('提交后将锁定库存，是否继续？', '提示', { type: 'warning' })
    const res = await submitSelection(plan.id)
    ElMessage.success(res.data?.message || '提交成功')
    await openPlan(plan)
  } catch (e) {
    if (e !== 'cancel') {
      ElMessage.error(e.response?.data?.message || '提交失败')
    }
  }
}

const cancelPlan = async (plan) => {
  try {
    await ElMessageBox.confirm('取消后将解锁已锁定的库存，是否继续？', '提示', { type: 'warning' })
    await cancelSelection(plan.id)
    ElMessage.success('已取消')
    await openPlan(plan)
  } catch (e) {
    if (e !== 'cancel') {
      ElMessage.error(e.response?.data?.message || '取消失败')
    }
  }
}

const deletePlan = async (plan) => {
  try {
    await ElMessageBox.confirm('确定删除选型单？', '警告', { type: 'warning' })
    await deleteSelection(plan.id)
    ElMessage.success('已删除')
    await loadSelections()
  } catch (e) {
    if (e !== 'cancel') {
      ElMessage.error('删除失败')
    }
  }
}

// Item management
const showAddItem = () => {
  itemForm.value = { partName: '', category: '', requiredQty: 1 }
  showItemDialog.value = true
}

const addItem = () => {
  if (!itemForm.value.partName || !itemForm.value.requiredQty) {
    ElMessage.warning('请填写配件名称和数量')
    return
  }
  if (!currentPlan.value.items) currentPlan.value.items = []
  currentPlan.value.items.push({
    id: 'new_' + Date.now(),
    partName: itemForm.value.partName,
    category: itemForm.value.category,
    requiredQty: itemForm.value.requiredQty,
    lockedQty: 0,
    outboundQty: 0,
    pendingQty: 0,
    selectedPartId: '',
    note: ''
  })
  showItemDialog.value = false
  saveItems()
}

const removeItem = async (index) => {
  currentPlan.value.items.splice(index, 1)
  await saveItems()
}

const saveItems = async () => {
  try {
    await updateSelection(currentPlan.value.id, {
      name: currentPlan.value.name,
      projectId: currentPlan.value.projectId,
      items: currentPlan.value.items
    })
  } catch (e) {
    ElMessage.error('保存失败')
  }
}

// Part selection
const selectPart = async (item) => {
  currentItem.value = item
  // Filter by category
  partFilter.value = ''
  showPartDialog.value = true
}

const choosePart = (part) => {
  if (!currentItem.value) return
  currentItem.value.selectedPartId = part.id
  currentItem.value.partName = part.name
  currentItem.value.category = part.category
  showPartDialog.value = false
  saveItems()
}

// Outbound
const showOutbound = (item) => {
  outboundForm.value = {
    itemId: item.id,
    partName: item.partName,
    qty: 1,
    availableQty: item.lockedQty,
    recipientId: '',
    recipientName: ''
  }
  showOutboundDialog.value = true
}

const doOutbound = async () => {
  if (outboundForm.value.qty <= 0) {
    ElMessage.warning('请输入正确的数量')
    return
  }
  try {
    const res = await outboundSelection(
      currentPlan.value.id,
      outboundForm.value.itemId,
      outboundForm.value.qty,
      outboundForm.value.recipientId,
      outboundForm.value.recipientName
    )
    ElMessage.success(res.data?.message || '出库成功')
    showOutboundDialog.value = false
    await openPlan(currentPlan.value)
  } catch (e) {
    ElMessage.error(e.response?.data?.message || '出库失败')
  }
}

// Status helpers
const statusType = (status) => {
  const map = { Draft: 'info', Submitted: 'warning', Completed: 'success', Cancelled: 'danger' }
  return map[status] || 'info'
}

const statusText = (status) => {
  const map = { Draft: '草稿', Submitted: '已提交', Completed: '已完成', Cancelled: '已取消' }
  return map[status] || status
}

const itemStatusType = (item) => {
  if (item.requiredQty === item.outboundQty) return 'success'
  if (item.pendingQty > 0) return 'warning'
  if (item.outboundQty > 0) return 'primary'
  return 'info'
}

const itemStatusText = (item) => {
  if (item.requiredQty === item.outboundQty) return '已完成'
  if (item.pendingQty > 0) return '部分待采购'
  if (item.outboundQty > 0) return '部分出库'
  if (item.lockedQty > 0) return '已锁定'
  return '待处理'
}

const formatDate = (d) => d ? new Date(d).toLocaleString('zh-CN') : ''
</script>

<style scoped>
.selection-container {
  padding: 20px;
}
.header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
}
.plan-list, .plan-detail {
  background: #fff;
  border-radius: 8px;
  padding: 20px;
}
.detail-header {
  display: flex;
  align-items: center;
  gap: 15px;
  margin-bottom: 20px;
}
.detail-header h3 {
  margin: 0;
  flex: 1;
}
.info-card, .form-card, .items-card {
  margin-bottom: 20px;
}
.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
.items-summary {
  display: flex;
  gap: 20px;
  padding: 15px;
  background: #f5f7fa;
  border-radius: 4px;
  margin-top: 15px;
}
.actions {
  margin-top: 20px;
  text-align: center;
}
.part-filter {
  margin-bottom: 15px;
}
.qty-locked { color: #409eff; font-weight: bold; }
.qty-outbound { color: #67c23a; font-weight: bold; }
.qty-pending { color: #e6a23c; font-weight: bold; }
.qty-low { color: #f56c6c; }
</style>
