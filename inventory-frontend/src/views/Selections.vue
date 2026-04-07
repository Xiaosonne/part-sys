<template>
  <div class="selection-container">
    <!-- Header -->
    <div class="header">
      <h2>选型中心</h2>
    </div>

    <!-- Two-column layout -->
    <div class="main-layout">
      <!-- Left: Project-Selection Tree -->
      <div class="left-panel">
        <el-input v-model="treeSearch" placeholder="搜索项目/选型" prefix-icon="Search" clearable style="margin-bottom: 10px;" />

        <!-- Create new selection -->
        <el-popover placement="right" :width="300" trigger="click" v-model:visible="showNewPlanPopover">
          <template #reference>
            <el-button type="primary" size="small" style="width: 100%; margin-bottom: 10px;">+ 新建选型单</el-button>
          </template>
          <el-form :model="newPlanForm" label-width="80px" size="small">
            <el-form-item label="名称">
              <el-input v-model="newPlanForm.name" placeholder="选型单名称" />
            </el-form-item>
            <el-form-item label="所属项目">
              <el-select v-model="newPlanForm.projectId" placeholder="选择项目" filterable style="width: 100%;">
                <el-option v-for="p in projects" :key="p.id" :label="p.name" :value="p.id" />
              </el-select>
            </el-form-item>
            <el-form-item>
              <el-button type="primary" size="small" @click="saveNewPlan" style="width: 100%;">创建</el-button>
            </el-form-item>
          </el-form>
        </el-popover>

        <!-- Tree -->
        <el-scrollbar class="tree-scrollbar">
          <el-tree
            ref="treeRef"
            :data="treeData"
            :props="{ label: 'name', children: 'children' }"
            :expand-on-click-node="true"
            :default-expand-all="true"
            node-key="id"
            :current-node-key="currentNodeKey"
            :filter-node-method="filterTreeNode"
            @node-click="onTreeNodeClick"
            highlight-current
            class="selection-tree"
          >
            <template #default="{ node, data }">
              <span class="tree-node">
                <span v-if="data.type === 'project'" class="node-icon">📁</span>
                <span v-else class="node-icon">📋</span>
                <span class="node-label">{{ node.label }}</span>
                <el-tag v-if="data.type === 'selection'" size="small" :type="statusType(data.status)" style="margin-left: 6px;">
                  {{ statusText(data.status) }}
                </el-tag>
              </span>
            </template>
          </el-tree>
        </el-scrollbar>
      </div>

      <!-- Right: Dynamic Content -->
      <div class="right-panel">
        <!-- Empty state -->
        <el-empty v-if="!currentProject && !currentPlan" description="从左侧选择一个项目或选型单" />

        <!-- Project selected: show selection list for that project -->
        <div v-else-if="currentProject && !currentPlan" class="project-view">
          <div class="view-header">
            <h3>{{ currentProject.name }}</h3>
            <span class="subtitle">选型单列表</span>
          </div>

          <el-table :data="projectSelections" border stripe v-loading="loadingSelections">
            <el-table-column prop="name" label="选型单名称" width="180" />
            <el-table-column prop="status" label="状态" width="100">
              <template #default="{ row }">
                <el-tag :type="statusType(row.status)">{{ statusText(row.status) }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="createdAt" label="创建时间" width="170">
              <template #default="{ row }">
                {{ formatDate(row.createdAt) }}
              </template>
            </el-table-column>
            <el-table-column label="配件项" width="80">
              <template #default="{ row }">
                {{ row.items?.length || 0 }}
              </template>
            </el-table-column>
            <el-table-column label="操作" min-width="200">
              <template #default="{ row }">
                <el-button size="small" type="primary" plain @click="openPlan(row)">查看详情</el-button>
                <el-button size="small" type="danger" plain @click="deletePlan(row)">删除</el-button>
              </template>
            </el-table-column>
          </el-table>

          <el-empty v-if="projectSelections.length === 0" description="该项目暂无选型单" />
        </div>

        <!-- Selection selected: show detail + items + summary -->
        <div v-else-if="currentPlan" class="plan-view">
          <div class="view-header">
            <el-button size="small" @click="backToProject" style="margin-right: 10px;">← 返回</el-button>
            <div>
              <h3>{{ currentPlan.name }}</h3>
              <span class="subtitle">{{ getProjectName(currentPlan.projectId) }}</span>
            </div>
            <el-tag :type="statusType(currentPlan.status)" style="margin-left: auto;">{{ statusText(currentPlan.status) }}</el-tag>
          </div>

          <!-- Summary Stats -->
          <div class="stats-bar">
            <div class="stat-item">
              <div class="stat-value">{{ currentPlan.items?.length || 0 }}</div>
              <div class="stat-label">配件项</div>
            </div>
            <div class="stat-item">
              <div class="stat-value qty-locked">{{ totalLocked }}</div>
              <div class="stat-label">已锁定</div>
            </div>
            <div class="stat-item">
              <div class="stat-value qty-outbound">{{ totalOutbound }}</div>
              <div class="stat-label">已出库</div>
            </div>
            <div class="stat-item">
              <div class="stat-value qty-pending">{{ totalPending }}</div>
              <div class="stat-label">待采购</div>
            </div>
            <div class="stat-item stat-total">
              <div class="stat-value">{{ totalRequired }}</div>
              <div class="stat-label">需求总数</div>
            </div>
          </div>

          <!-- Actions -->
          <div class="plan-actions">
            <el-button type="primary" @click="showAddItem" :disabled="currentPlan.status !== 'Draft' && currentPlan.status !== 0">
              + 添加配件
            </el-button>
            <el-button type="warning" @click="submitPlan(currentPlan)" :disabled="(currentPlan.status !== 'Draft' && currentPlan.status !== 0) || !currentPlan.items?.length">
              提交选型单
            </el-button>
            <el-button type="danger" plain @click="cancelPlan(currentPlan)" :disabled="currentPlan.status !== 'Submitted' && currentPlan.status !== 1">
              取消选型单
            </el-button>
          </div>

          <!-- Items Table -->
          <el-table :data="currentPlan.items" border stripe class="items-table">
            <el-table-column prop="partName" label="配件名称" min-width="150" />
            <el-table-column prop="category" label="分类" width="120" />
            <el-table-column prop="requiredQty" label="需求数量" width="90" align="center" />
            <el-table-column prop="lockedQty" label="已锁定" width="90" align="center">
              <template #default="{ row }">
                <span class="qty-locked">{{ row.lockedQty || 0 }}</span>
              </template>
            </el-table-column>
            <el-table-column prop="outboundQty" label="已出库" width="90" align="center">
              <template #default="{ row }">
                <span class="qty-outbound">{{ row.outboundQty || 0 }}</span>
              </template>
            </el-table-column>
            <el-table-column prop="pendingQty" label="待采购" width="90" align="center">
              <template #default="{ row }">
                <span class="qty-pending">{{ row.pendingQty || 0 }}</span>
              </template>
            </el-table-column>
            <el-table-column label="状态" width="110" align="center">
              <template #default="{ row }">
                <el-tag size="small" :type="itemStatusType(row)">{{ itemStatusText(row) }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column label="操作" width="160" align="center">
              <template #default="{ row, $index }">
                <template v-if="currentPlan.status === 'Draft' || currentPlan.status === 0">
                  <el-button size="small" @click="selectPart(row)">更换</el-button>
                  <el-button size="small" type="danger" plain @click="removeItem($index)">删除</el-button>
                </template>
                <template v-else-if="currentPlan.status === 'Submitted' || currentPlan.status === 1">
                  <el-button size="small" type="success" @click="showOutbound(row)" :disabled="row.lockedQty <= 0">
                    出库
                  </el-button>
                </template>
              </template>
            </el-table-column>
          </el-table>
        </div>
      </div>
    </div>

    <!-- Select Part Dialog (add new OR re-select existing) -->
    <el-dialog v-model="showPartDialog" :title="currentItem ? '重新选择配件' : '添加配件'" width="700px">
      <div class="part-filter">
        <el-input v-model="partFilter" placeholder="搜索配件名称/型号/品牌/分类" clearable style="margin-bottom: 10px;" />
      </div>
      <el-table :data="filteredParts" border stripe max-height="300" @row-click="selectPartRow" highlight-current-row>
        <el-table-column prop="name" label="配件名称" />
        <el-table-column prop="model" label="型号" width="120" />
        <el-table-column prop="brand" label="品牌" width="100" />
        <el-table-column prop="category" label="分类" width="100" />
        <el-table-column prop="availableQty" label="可用库存" width="100" align="center">
          <template #default="{ row }">
            <span :class="{ 'qty-low': row.availableQty < 5 }">{{ row.availableQty }}</span>
          </template>
        </el-table-column>
      </el-table>
      <div v-if="selectedPartForAdd" class="add-form">
        <div class="selected-info">
          已选: <strong>{{ selectedPartForAdd.name }}</strong>
          <span class="muted">{{ selectedPartForAdd.model }} / {{ selectedPartForAdd.brand }} / {{ selectedPartForAdd.category }}</span>
        </div>
        <el-form :model="itemForm" label-width="100px" v-if="!currentItem">
          <el-form-item label="需求数量">
            <el-input-number v-model="itemForm.requiredQty" :min="1" :max="9999" />
          </el-form-item>
        </el-form>
      </div>
      <template #footer>
        <el-button @click="closePartDialog">取消</el-button>
        <el-button type="primary" @click="confirmPartSelection" :disabled="!selectedPartForAdd">
          {{ currentItem ? '确认更换' : '添加' }}
        </el-button>
      </template>
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
import { ref, computed, watch, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import {
  getSelections, getSelection, createSelection, updateSelection,
  deleteSelection, submitSelection, outboundSelection, cancelSelection
} from '@/api/selections'
import { getProjects } from '@/api/projects'
import { getParts } from '@/api/parts'

const route = useRoute()
const router = useRouter()

const selections = ref([])
const projects = ref([])
const allParts = ref([])

// Tree state
const treeRef = ref(null)
const treeSearch = ref('')
const currentNodeKey = ref(null)
const currentProject = ref(null)
const currentPlan = ref(null)
const loadingSelections = ref(false)

// New plan popover
const showNewPlanPopover = ref(false)
const newPlanForm = ref({ name: '', projectId: '' })

// Part dialog
const showPartDialog = ref(false)
const partFilter = ref('')
const selectedPartForAdd = ref(null)
const currentItem = ref(null)
const itemForm = ref({ requiredQty: 1 })

// Outbound dialog
const showOutboundDialog = ref(false)
const outboundForm = ref({ itemId: '', partName: '', qty: 1, availableQty: 0, recipientName: '' })

// Computed: build tree data from projects + selections
const treeData = computed(() => {
  return projects.value.map(project => {
    const projectSelections = selections.value.filter(s => s.projectId === project.id)
    return {
      id: 'proj_' + project.id,
      name: project.name,
      type: 'project',
      children: projectSelections.map(s => ({
        id: 'sel_' + s.id,
        name: s.name,
        type: 'selection',
        status: s.status,
        projectId: s.projectId,
        _selection: s
      }))
    }
  })
})

// Computed: selections for currently selected project
const projectSelections = computed(() => {
  if (!currentProject.value) return []
  return selections.value
    .filter(s => s.projectId === currentProject.value.id)
    .sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt))
})

// Computed: summary stats
const totalLocked = computed(() => currentPlan.value?.items?.reduce((s, i) => s + (i.lockedQty || 0), 0) || 0)
const totalOutbound = computed(() => currentPlan.value?.items?.reduce((s, i) => s + (i.outboundQty || 0), 0) || 0)
const totalPending = computed(() => currentPlan.value?.items?.reduce((s, i) => s + (i.pendingQty || 0), 0) || 0)
const totalRequired = computed(() => currentPlan.value?.items?.reduce((s, i) => s + (i.requiredQty || 0), 0) || 0)

const filteredParts = computed(() => {
  if (!partFilter.value) return allParts.value
  const f = partFilter.value.toLowerCase()
  return allParts.value.filter(p =>
    p.name?.toLowerCase().includes(f) ||
    p.model?.toLowerCase().includes(f) ||
    p.brand?.toLowerCase().includes(f) ||
    p.category?.toLowerCase().includes(f)
  )
})

// Watch tree search
watch(treeSearch, (val) => {
  treeRef.value?.filter(val)
})

// Lifecycle
onMounted(async () => {
  await Promise.all([loadSelections(), loadProjects(), loadParts()])
  // Handle query params from navigation
  const { projectId, selectionId } = route.query
  if (selectionId) {
    const sel = selections.value.find(s => s.id === selectionId)
    if (sel) {
      await openPlan(sel)
    }
  } else if (projectId) {
    const proj = projects.value.find(p => p.id === projectId)
    if (proj) {
      currentProject.value = { id: proj.id, name: proj.name }
      currentPlan.value = null
      currentNodeKey.value = 'proj_' + proj.id
    }
  }
})

// Data loading
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

// Tree helpers
const filterTreeNode = (value, data) => {
  if (!value) return true
  const f = value.toLowerCase()
  return data.name?.toLowerCase().includes(f)
}

const getProjectName = (projectId) => {
  return projects.value.find(p => p.id === projectId)?.name || projectId
}

// Tree click handler
const onTreeNodeClick = async (data) => {
  if (data.type === 'project') {
    currentProject.value = { id: data.id.replace('proj_', ''), name: data.name }
    currentPlan.value = null
    currentNodeKey.value = data.id
  } else {
    // selection node
    currentProject.value = null
    await openPlan(data._selection)
  }
}

// Navigation
const backToProject = () => {
  const projectId = currentPlan.value?.projectId
  currentPlan.value = null
  if (projectId) {
    currentProject.value = projects.value.find(p => p.id === projectId) || null
    if (currentProject.value) {
      currentNodeKey.value = 'proj_' + projectId
    }
  }
}

// Plan operations
const saveNewPlan = async () => {
  if (!newPlanForm.value.name || !newPlanForm.value.projectId) {
    ElMessage.warning('请填写名称和选择项目')
    return
  }
  const planName = newPlanForm.value.name
  const projectId = newPlanForm.value.projectId
  try {
    const res = await createSelection({
      name: planName,
      projectId: projectId,
      items: []
    })
    showNewPlanPopover.value = false
    newPlanForm.value = { name: '', projectId: '' }
    await loadSelections()
    // Navigate to the new plan
    const newId = res.data?.id
    if (newId) {
      const created = selections.value.find(s => s.id === newId)
      if (created) {
        await openPlan(created)
      }
    } else {
      const created = selections.value.find(s => s.name === planName && s.projectId === projectId)
      if (created) {
        await openPlan(created)
      }
    }
    ElMessage.success('创建成功')
  } catch (e) {
    ElMessage.error('创建失败')
  }
}

const openPlan = async (plan) => {
  try {
    const res = await getSelection(plan.id)
    currentPlan.value = res.data
    currentNodeKey.value = 'sel_' + plan.id
    currentProject.value = null
  } catch (e) {
    ElMessage.error('加载选型单失败')
  }
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
    await ElMessageBox.confirm('确定删除该选型单？', '警告', { type: 'warning' })
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
  currentItem.value = null
  selectedPartForAdd.value = null
  itemForm.value = { requiredQty: 1 }
  partFilter.value = ''
  showPartDialog.value = true
}

const selectPartRow = (part) => {
  selectedPartForAdd.value = part
}

const closePartDialog = () => {
  showPartDialog.value = false
  selectedPartForAdd.value = null
  currentItem.value = null
}

const addItem = () => {
  if (!selectedPartForAdd.value) {
    ElMessage.warning('请先选择一个配件')
    return
  }
  if (!itemForm.value.requiredQty || itemForm.value.requiredQty < 1) {
    ElMessage.warning('请输入正确的需求数量')
    return
  }
  if (!currentPlan.value.items) currentPlan.value.items = []
  const part = selectedPartForAdd.value
  currentPlan.value.items.push({
    id: 'new_' + Date.now(),
    selectedPartId: part.id,
    partName: part.name,
    category: part.category || '',
    requiredQty: itemForm.value.requiredQty,
    lockedQty: 0,
    outboundQty: 0,
    pendingQty: 0,
    note: ''
  })
  closePartDialog()
  saveItems()
}

const confirmPartSelection = () => {
  if (!selectedPartForAdd.value) return
  if (currentItem.value) {
    currentItem.value.selectedPartId = selectedPartForAdd.value.id
    currentItem.value.partName = selectedPartForAdd.value.name
    currentItem.value.category = selectedPartForAdd.value.category || ''
    closePartDialog()
    saveItems()
  } else {
    addItem()
  }
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

// Part re-select
const selectPart = async (item) => {
  currentItem.value = item
  selectedPartForAdd.value = null
  itemForm.value = { requiredQty: item.requiredQty || 1 }
  partFilter.value = ''
  showPartDialog.value = true
}

// Outbound
const showOutbound = (item) => {
  outboundForm.value = {
    itemId: item.id,
    partName: item.partName,
    qty: 1,
    availableQty: item.lockedQty,
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
      '', // no recipientId needed
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
  const map = { Draft: 'info', Submitted: 'warning', Completed: 'success', Cancelled: 'danger', 0: 'info', 1: 'warning', 2: 'success', 3: 'danger' }
  return map[status] || 'info'
}

const statusText = (status) => {
  const map = { Draft: '草稿', Submitted: '已提交', Completed: '已完成', Cancelled: '已取消', 0: '草稿', 1: '已提交', 2: '已完成', 3: '已取消' }
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
  height: calc(100vh - 80px);
  display: flex;
  flex-direction: column;
}
.header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 16px;
}
.header h2 {
  margin: 0;
}
.main-layout {
  display: flex;
  gap: 16px;
  flex: 1;
  overflow: hidden;
}
.left-panel {
  width: 280px;
  min-width: 280px;
  background: #fff;
  border-radius: 8px;
  padding: 16px;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}
.right-panel {
  flex: 1;
  background: #fff;
  border-radius: 8px;
  padding: 20px;
  overflow-y: auto;
}
.tree-scrollbar {
  flex: 1;
  overflow-y: auto;
}
.selection-tree {
  background: transparent;
}
.tree-node {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 14px;
}
.node-icon {
  font-size: 14px;
}
.node-label {
  flex: 1;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

/* Views */
.view-header {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 20px;
}
.view-header h3 {
  margin: 0;
}
.subtitle {
  color: #999;
  font-size: 13px;
}

/* Stats bar */
.stats-bar {
  display: flex;
  gap: 24px;
  padding: 16px 20px;
  background: linear-gradient(135deg, #f5f7fa 0%, #e8ecf0 100%);
  border-radius: 8px;
  margin-bottom: 16px;
}
.stat-item {
  text-align: center;
  min-width: 60px;
}
.stat-value {
  font-size: 24px;
  font-weight: bold;
  color: #333;
}
.stat-label {
  font-size: 12px;
  color: #888;
  margin-top: 4px;
}
.stat-total .stat-value {
  color: #409eff;
}
.qty-locked { color: #409eff; }
.qty-outbound { color: #67c23a; }
.qty-pending { color: #e6a23c; }

/* Plan actions */
.plan-actions {
  display: flex;
  gap: 10px;
  margin-bottom: 16px;
}

/* Items table */
.items-table {
  margin-top: 0;
}

/* Part dialog */
.part-filter {
  margin-bottom: 10px;
}
.add-form {
  margin-top: 15px;
  padding: 15px;
  background: #f5f7fa;
  border-radius: 4px;
}
.selected-info {
  margin-bottom: 10px;
  font-size: 14px;
}
.selected-info .muted {
  color: #888;
  margin-left: 8px;
}
.qty-low { color: #f56c6c; }

/* Project view */
.project-view {
  /* use right-panel styles */
}
.plan-view {
  /* use right-panel styles */
}
</style>
