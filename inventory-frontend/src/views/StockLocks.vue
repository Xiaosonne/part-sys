<template>
  <div class="stock-locks">
    <!-- 统计信息 -->
    <div class="stats-cards">
      <div class="stat-card">
        <div class="stat-icon locked-icon">
          <i class="el-icon-lock"></i>
        </div>
        <div class="stat-content">
          <div class="stat-value">{{ stats.totalLocked }}</div>
          <div class="stat-label">当前锁定总数</div>
        </div>
      </div>
      <div class="stat-card">
        <div class="stat-icon part-icon">
          <i class="el-icon-box"></i>
        </div>
        <div class="stat-content">
          <div class="stat-value">{{ stats.partCount }}</div>
          <div class="stat-label">涉及配件</div>
        </div>
      </div>
      <div class="stat-card">
        <div class="stat-icon project-icon">
          <i class="el-icon-folder-opened"></i>
        </div>
        <div class="stat-content">
          <div class="stat-value">{{ stats.projectCount }}</div>
          <div class="stat-label">涉及项目</div>
        </div>
      </div>
    </div>

    <!-- 筛选 -->
    <div class="filter-bar">
      <el-input
        v-model="keyword"
        placeholder="搜索配件名称、型号..."
        clearable
        style="width: 240px;"
        @keyup.enter="loadData"
      >
        <template #prefix><i class="el-icon-search"></i></template>
      </el-input>
      <el-select v-model="filterProjectId" placeholder="项目筛选" clearable style="width: 180px;" @change="loadData">
        <el-option v-for="proj in projects" :key="proj.id" :label="proj.name" :value="proj.id" />
      </el-select>
      <el-button type="primary" @click="loadData" :loading="loading">
        <i class="el-icon-search"></i> 搜索
      </el-button>
      <el-button @click="resetFilters">重置</el-button>
    </div>

    <!-- 锁定列表 -->
    <div class="panel-card">
      <div class="table-header">
        <span class="table-title">配件锁定列表</span>
        <span class="table-count">共 {{ filteredLocks.length }} 条</span>
      </div>
      <el-table
        :data="filteredLocks"
        stripe
        style="width: 100%;"
        v-loading="loading"
        row-key="partId"
        :expand-row-keys="expandedRows"
        @expand-change="onExpandChange"
      >
        <el-table-column type="expand" width="40">
          <template #default="{ row }">
            <div v-if="row.expanded" class="lock-details">
              <el-table :data="row.locks" stripe size="small" max-height="250">
                <el-table-column prop="projectName" label="项目" min-width="150" />
                <el-table-column prop="selectionPlanName" label="选型单" min-width="120" />
                <el-table-column prop="lockedQty" label="锁库数量" width="100" align="center" />
                <el-table-column prop="operatorName" label="操作人" width="100" />
                <el-table-column label="锁定时间" width="160">
                  <template #default="{ row: lock }">
                    {{ formatDate(lock.lockedAt) }}
                  </template>
                </el-table-column>
                <el-table-column label="操作" width="100" fixed="right">
                  <template #default="{ row: lock }">
                    <el-button size="small" type="danger" @click="handleUnlock(lock, row)">解锁</el-button>
                  </template>
                </el-table-column>
              </el-table>
            </div>
          </template>
        </el-table-column>

        <el-table-column prop="partName" label="配件名称" min-width="180" />
        <el-table-column prop="partModel" label="型号" width="150" />
        <el-table-column prop="category" label="分类" width="150" />
        <el-table-column prop="totalLocked" label="锁定总数" width="100" align="center">
          <template #default="{ row }">
            <el-tag type="warning" size="small">{{ row.totalLocked }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="锁定来源" min-width="200">
          <template #default="{ row }">
            <div v-for="lock in row.locks.slice(0, 2)" :key="lock.transactionId" class="lock-source-item">
              <span class="project-tag">{{ lock.projectName }}</span>
              <span v-if="lock.selectionPlanName" class="plan-tag">{{ lock.selectionPlanName }}</span>
              <span class="qty-tag">x{{ lock.lockedQty }}</span>
            </div>
            <span v-if="row.locks.length > 2" class="more-hint">+{{ row.locks.length - 2 }} 更多</span>
          </template>
        </el-table-column>
      </el-table>

      <el-empty v-if="!loading && filteredLocks.length === 0" description="暂无锁定记录" />
    </div>

    <!-- 解锁确认 -->
    <el-dialog v-model="showUnlockDialog" title="确认解锁" width="400px">
      <div v-if="unlockingLock">
        <p>确定解锁以下锁定？</p>
        <el-descriptions :column="1" border style="margin-top: 12px;">
          <el-descriptions-item label="配件">{{ unlockingLock.partName }}</el-descriptions-item>
          <el-descriptions-item label="项目">{{ unlockingLock.projectName }}</el-descriptions-item>
          <el-descriptions-item label="选型单">{{ unlockingLock.selectionPlanName || '-' }}</el-descriptions-item>
          <el-descriptions-item label="锁定数量">{{ unlockingLock.lockedQty }}</el-descriptions-item>
        </el-descriptions>
        <el-form style="margin-top: 16px;">
          <el-form-item label="解锁数量">
            <el-input-number v-model="unlockQty" :min="1" :max="unlockingLock.lockedQty" />
          </el-form-item>
        </el-form>
      </div>
      <template #footer>
        <el-button @click="showUnlockDialog = false">取消</el-button>
        <el-button type="primary" @click="confirmUnlock" :loading="unlocking">确认解锁</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { getStockLocks } from '@/api/stock'
import { unlock } from '@/api/stock'
import { getProjects } from '@/api/projects'

const loading = ref(false)
const allLocks = ref([])
const projects = ref([])
const keyword = ref('')
const filterProjectId = ref('')
const expandedRows = ref([])

// Stats
const stats = computed(() => {
  const totalLocked = allLocks.value.reduce((sum, p) => sum + p.totalLocked, 0)
  const partCount = allLocks.value.length
  const projectSet = new Set()
  allLocks.value.forEach(p => p.locks.forEach(l => {
    if (l.projectId) projectSet.add(l.projectId)
  }))
  return { totalLocked, partCount, projectCount: projectSet.size }
})

// Filtered
const filteredLocks = computed(() => {
  return allLocks.value.filter(p => {
    const matchKw = !keyword.value ||
      p.partName.toLowerCase().includes(keyword.value.toLowerCase()) ||
      p.partModel.toLowerCase().includes(keyword.value.toLowerCase())
    const matchProject = !filterProjectId.value ||
      p.locks.some(l => l.projectId === filterProjectId.value)
    return matchKw && matchProject
  })
})

onMounted(() => {
  loadData()
  loadProjects()
})

const loadData = async () => {
  loading.value = true
  try {
    const res = await getStockLocks()
    allLocks.value = (res.data || []).map(p => ({ ...p, expanded: false }))
  } catch (error) {
    ElMessage.error('加载锁定状态失败')
  } finally {
    loading.value = false
  }
}

const loadProjects = async () => {
  try {
    const res = await getProjects()
    projects.value = res.data || []
  } catch (error) {
    console.error('加载项目失败', error)
  }
}

const resetFilters = () => {
  keyword.value = ''
  filterProjectId.value = ''
}

const onExpandChange = (row, expanded) => {
  if (expanded) {
    expandedRows.value = [row.partId]
  } else {
    expandedRows.value = []
  }
}

// Unlock dialog
const showUnlockDialog = ref(false)
const unlockingLock = ref(null)
const unlockingPart = ref(null)
const unlockQty = ref(1)
const unlocking = ref(false)

const handleUnlock = (lock, part) => {
  unlockingLock.value = { ...lock, partId: part.partId }
  unlockingPart.value = part
  unlockQty.value = Math.min(1, lock.lockedQty)
  showUnlockDialog.value = true
}

const confirmUnlock = async () => {
  if (!unlockingLock.value) return
  unlocking.value = true
  try {
    await unlock({
      partId: unlockingLock.value.partId,
      quantity: unlockQty.value
    })
    ElMessage.success('解锁成功')
    showUnlockDialog.value = false
    await loadData()
  } catch (error) {
    ElMessage.error(error.response?.data?.message || '解锁失败')
  } finally {
    unlocking.value = false
  }
}

const formatDate = (dateStr) => {
  if (!dateStr) return '-'
  return new Date(dateStr).toLocaleString('zh-CN', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' })
}
</script>

<style scoped>
.stock-locks {
  padding: 16px 20px;
  display: flex;
  flex-direction: column;
  gap: 16px;
  height: 100%;
  overflow: auto;
}

/* Ant Design Style Stats Cards */
.stats-cards {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 16px;
}

.stat-card {
  background: var(--color-bg-card);
  border: 1px solid var(--color-border);
  border-radius: var(--radius);
  padding: 16px 20px;
  display: flex;
  align-items: center;
  gap: 16px;
  box-shadow: var(--shadow-sm);
  transition: box-shadow 0.2s;
}

.stat-card:hover {
  box-shadow: var(--shadow-md);
}

.stat-icon {
  width: 48px;
  height: 48px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 22px;
}

.locked-icon {
  background: #fff7e6;
  color: #fa8c16;
}

.part-icon {
  background: #e6f7ff;
  color: #1890ff;
}

.project-icon {
  background: #f6ffed;
  color: #52c41a;
}

.stat-content {
  display: flex;
  flex-direction: column;
}

.stat-value {
  font-size: 24px;
  font-weight: 600;
  color: var(--color-text-primary);
  line-height: 1;
}

.stat-label {
  font-size: 13px;
  color: var(--color-text-secondary);
  margin-top: 4px;
}

.filter-bar {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px 16px;
  background: var(--color-bg-card);
  border: 1px solid var(--color-border);
  border-radius: var(--radius);
}

.table-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 12px 16px;
  border-bottom: 1px solid var(--color-border);
}

.table-title {
  font-size: 15px;
  font-weight: 600;
  color: var(--color-text-primary);
}

.table-count {
  font-size: 13px;
  color: var(--color-text-secondary);
}

.lock-details {
  padding: 12px 20px;
  background: var(--color-bg-page);
}

.lock-source-item {
  display: flex;
  align-items: center;
  gap: 4px;
  margin-bottom: 4px;
  font-size: 12px;
}

.project-tag {
  background: #e6f7ff;
  color: #1890ff;
  padding: 1px 6px;
  border-radius: 3px;
}

.plan-tag {
  background: #f6ffed;
  color: #52c41a;
  padding: 1px 6px;
  border-radius: 3px;
}

.qty-tag {
  color: #fa8c16;
  font-weight: 600;
}

.more-hint {
  color: var(--color-text-muted);
  font-size: 11px;
}
</style>
