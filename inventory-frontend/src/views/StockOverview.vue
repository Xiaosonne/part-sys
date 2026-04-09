<template>
  <div class="stock-overview">
    <!-- 统计卡片 - Ant Design Card Style -->
    <div class="stats-cards">
      <div class="stat-card">
        <div class="stat-icon total-icon">
          <i class="el-icon-box"></i>
        </div>
        <div class="stat-content">
          <div class="stat-value">{{ stats.total }}</div>
          <div class="stat-label">总库存</div>
        </div>
      </div>
      <div class="stat-card">
        <div class="stat-icon locked-icon">
          <i class="el-icon-lock"></i>
        </div>
        <div class="stat-content">
          <div class="stat-value">{{ stats.locked }}</div>
          <div class="stat-label">已锁定</div>
        </div>
      </div>
      <div class="stat-card">
        <div class="stat-icon available-icon">
          <i class="el-icon-check"></i>
        </div>
        <div class="stat-content">
          <div class="stat-value">{{ stats.available }}</div>
          <div class="stat-label">可用库存</div>
        </div>
      </div>
      <div class="stat-card">
        <div class="stat-icon pending-icon">
          <i class="el-icon-shopping-cart-2"></i>
        </div>
        <div class="stat-content">
          <div class="stat-value">{{ stats.pending }}</div>
          <div class="stat-label">待采购</div>
        </div>
      </div>
    </div>

    <!-- 搜索过滤 -->
    <div class="search-bar">
      <el-input
        v-model="keyword"
        placeholder="搜索配件名称、型号..."
        clearable
        style="width: 240px;"
        @keyup.enter="loadData"
      >
        <template #prefix><i class="el-icon-search"></i></template>
      </el-input>
      <el-select v-model="filterCategory" placeholder="分类筛选" clearable style="width: 180px;">
        <el-option v-for="cat in categories" :key="cat" :label="cat" :value="cat" />
      </el-select>
      <el-button type="primary" @click="loadData" :loading="loading">
        <i class="el-icon-search"></i> 搜索
      </el-button>
      <el-button @click="resetFilters">重置</el-button>
    </div>

    <!-- 配件列表 -->
    <div class="panel-card">
      <div class="table-header">
        <span class="table-title">配件库存列表</span>
        <span class="table-count">共 {{ filteredParts.length }} 条</span>
      </div>
      <el-table
        :data="filteredParts"
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
              <div class="lock-details-title">锁定明细</div>
              <el-table :data="row.locks" stripe size="small" max-height="200" v-loading="row.loadingLocks">
                <el-table-column prop="projectName" label="项目" min-width="150" />
                <el-table-column prop="selectionPlanName" label="选型单" min-width="120" />
                <el-table-column prop="lockedQty" label="锁库数量" width="100" align="center" />
                <el-table-column prop="operatorName" label="操作人" width="100" />
                <el-table-column label="锁定时间" width="160">
                  <template #default="{ row: lock }">
                    {{ formatDate(lock.lockedAt) }}
                  </template>
                </el-table-column>
              </el-table>
              <el-empty v-if="!row.loadingLocks && row.locks?.length === 0" description="无锁定明细" :image-size="60" />
            </div>
          </template>
        </el-table-column>

        <el-table-column prop="partName" label="配件名称" min-width="180" />
        <el-table-column prop="partModel" label="型号" width="150" />
        <el-table-column prop="category" label="分类" width="150" />
        <el-table-column prop="totalQty" label="总数" width="80" align="center" />
        <el-table-column prop="lockedQty" label="已锁定" width="80" align="center">
          <template #default="{ row }">
            <span v-if="row.lockedQty > 0" class="locked-qty">{{ row.lockedQty }}</span>
            <span v-else>-</span>
          </template>
        </el-table-column>
        <el-table-column prop="availableQty" label="可用" width="80" align="center">
          <template #default="{ row }">
            <el-tag v-if="row.availableQty === 0" type="danger" size="small">缺货</el-tag>
            <span v-else>{{ row.availableQty }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="pendingPurchaseQty" label="待采购" width="90" align="center">
          <template #default="{ row }">
            <el-tag v-if="row.pendingPurchaseQty > 0" type="warning" size="small">{{ row.pendingPurchaseQty }}</el-tag>
            <span v-else>-</span>
          </template>
        </el-table-column>
        <el-table-column label="更新时间" width="160">
          <template #default="{ row }">
            {{ formatDate(row.updatedAt) }}
          </template>
        </el-table-column>
      </el-table>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { getStockOverview, getStockLocksByPartId } from '@/api/stock'

const loading = ref(false)
const allParts = ref([])
const keyword = ref('')
const filterCategory = ref('')
const expandedRows = ref([])

// Stats
const stats = computed(() => ({
  total: allParts.value.reduce((sum, p) => sum + p.totalQty, 0),
  locked: allParts.value.reduce((sum, p) => sum + p.lockedQty, 0),
  available: allParts.value.reduce((sum, p) => sum + p.availableQty, 0),
  pending: allParts.value.reduce((sum, p) => sum + p.pendingPurchaseQty, 0)
}))

// Categories for filter dropdown
const categories = computed(() => {
  const cats = new Set(allParts.value.map(p => p.category).filter(Boolean))
  return Array.from(cats).sort()
})

// Filtered parts
const filteredParts = computed(() => {
  return allParts.value.filter(p => {
    const matchKeyword = !keyword.value ||
      p.partName.toLowerCase().includes(keyword.value.toLowerCase()) ||
      p.partModel.toLowerCase().includes(keyword.value.toLowerCase())
    const matchCategory = !filterCategory.value || p.category === filterCategory.value
    return matchKeyword && matchCategory
  })
})

onMounted(() => {
  loadData()
})

const loadData = async () => {
  loading.value = true
  try {
    const res = await getStockOverview()
    allParts.value = (res.data || []).map(p => ({
      ...p,
      expanded: false,
      locks: [],
      loadingLocks: false
    }))
  } catch (error) {
    ElMessage.error('加载库存总览失败')
  } finally {
    loading.value = false
  }
}

const resetFilters = () => {
  keyword.value = ''
  filterCategory.value = ''
}

const onExpandChange = async (row, expanded) => {
  if (expanded && !row.locks.length && !row.loadingLocks) {
    row.loadingLocks = true
    try {
      const res = await getStockLocksByPartId(row.partId)
      row.locks = res.data?.locks || []
    } catch (error) {
      console.error('加载锁定明细失败', error)
      row.locks = []
    } finally {
      row.loadingLocks = false
    }
  }
  // Update expanded state
  if (expanded) {
    expandedRows.value = [row.partId]
  } else {
    expandedRows.value = []
  }
}

const formatDate = (dateStr) => {
  if (!dateStr) return '-'
  return new Date(dateStr).toLocaleString('zh-CN', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' })
}
</script>

<style scoped>
.stock-overview {
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
  grid-template-columns: repeat(4, 1fr);
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

.total-icon {
  background: #e6f7ff;
  color: #1890ff;
}

.locked-icon {
  background: #fff7e6;
  color: #fa8c16;
}

.available-icon {
  background: #f6ffed;
  color: #52c41a;
}

.pending-icon {
  background: #fff1f0;
  color: #ff4d4f;
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

.search-bar {
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

.lock-details-title {
  font-size: 13px;
  font-weight: 600;
  color: var(--color-text-secondary);
  margin-bottom: 10px;
}

.locked-qty {
  color: #fa8c16;
  font-weight: 600;
}
</style>
