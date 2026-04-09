<template>
  <div class="stock-history">
    <!-- 筛选栏 -->
    <div class="filter-bar">
      <el-date-picker
        v-model="dateRange"
        type="daterange"
        range-separator="至"
        start-placeholder="开始日期"
        end-placeholder="结束日期"
        value-format="YYYY-MM-DD"
        style="width: 260px;"
        @change="loadData"
      />
      <el-select v-model="filterType" placeholder="类型" clearable style="width: 100px;" @change="loadData">
        <el-option label="入库" value="INBOUND" />
        <el-option label="出库" value="OUTBOUND" />
        <el-option label="锁定" value="LOCK" />
        <el-option label="解锁" value="UNLOCK" />
        <el-option label="归还" value="RETURN" />
      </el-select>
      <el-select v-model="filterSourceType" placeholder="来源" clearable style="width: 130px;" @change="loadData">
        <el-option label="手动" value="0" />
        <el-option label="采购入库" value="1" />
        <el-option label="选型锁定" value="2" />
        <el-option label="选型解锁" value="3" />
        <el-option label="选型出库" value="4" />
      </el-select>
      <el-input
        v-model="keyword"
        placeholder="配件名称搜索"
        clearable
        style="width: 160px;"
        @keyup.enter="loadData"
      />
      <el-button type="primary" @click="loadData" :loading="loading">
        <i class="el-icon-search"></i> 搜索
      </el-button>
      <el-button @click="resetFilters">重置</el-button>
    </div>

    <!-- 流水列表 -->
    <div class="panel-card">
      <div class="table-header">
        <span class="table-title">库存流水</span>
        <span class="table-count">共 {{ filteredTransactions.length }} 条</span>
      </div>
      <el-table
        :data="pagedTransactions"
        stripe
        style="width: 100%;"
        v-loading="loading"
      >
        <el-table-column label="时间" width="160">
          <template #default="{ row }">
            {{ formatDate(row.createdAt) }}
          </template>
        </el-table-column>
        <el-table-column prop="partName" label="配件" min-width="180">
          <template #default="{ row }">
            <div>{{ row.partName }}</div>
            <div class="text-muted">{{ row.partModel }}</div>
          </template>
        </el-table-column>
        <el-table-column label="入库类型" width="140" align="center">
          <template #default="{ row }">
            <el-tag :type="typeTagType(row.type)" size="small" class="type-tag">{{ typeLabel(row.type) }}</el-tag>
            <el-tag
              size="small"
              :class="'source-tag ' + sourceTypeClass(row.sourceType)"
              style="margin-left: 4px;"
            >{{ row.sourceTypeName }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="quantity" label="数量" width="80" align="center">
          <template #default="{ row }">
            <span :class="row.quantity > 0 ? 'qty-positive' : 'qty-negative'">
              {{ row.quantity > 0 ? '+' : '' }}{{ row.quantity }}
            </span>
          </template>
        </el-table-column>
        <el-table-column prop="operatorName" label="操作人" width="100" />
        <el-table-column prop="projectName" label="关联项目" min-width="140">
          <template #default="{ row }">
            <span v-if="row.projectName">{{ row.projectName }}</span>
            <span v-else class="text-muted">-</span>
          </template>
        </el-table-column>
        <el-table-column prop="selectionPlanName" label="选型单" min-width="120">
          <template #default="{ row }">
            <span v-if="row.selectionPlanName">{{ row.selectionPlanName }}</span>
            <span v-else class="text-muted">-</span>
          </template>
        </el-table-column>
        <el-table-column prop="usage" label="用途" width="90">
          <template #default="{ row }">
            <span v-if="row.usage">{{ row.usage }}</span>
            <span v-else class="text-muted">-</span>
          </template>
        </el-table-column>
        <el-table-column prop="note" label="备注" min-width="150" show-overflow-tooltip />
      </el-table>

      <div class="pagination-wrapper" v-if="filteredTransactions.length > 0">
        <el-pagination
          v-model:current-page="currentPage"
          v-model:page-size="pageSize"
          :page-sizes="[20, 50, 100, 200]"
          :total="filteredTransactions.length"
          layout="total, sizes, prev, pager, next"
          background
        />
      </div>

      <el-empty v-if="!loading && allTransactions.length === 0" description="暂无流水记录" />
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { getTransactions } from '@/api/stock'

const loading = ref(false)
const allTransactions = ref([])
const currentPage = ref(1)
const pageSize = ref(20)

const dateRange = ref([])
const filterType = ref('')
const filterSourceType = ref('')
const keyword = ref('')

// Filtered
const filteredTransactions = computed(() => {
  return allTransactions.value.filter(t => {
    if (keyword.value) {
      const kw = keyword.value.toLowerCase()
      if (!t.partName.toLowerCase().includes(kw) && !t.partModel?.toLowerCase().includes(kw)) {
        return false
      }
    }
    return true
  })
})

// Paged
const pagedTransactions = computed(() => {
  const start = (currentPage.value - 1) * pageSize.value
  return filteredTransactions.value.slice(start, start + pageSize.value)
})

onMounted(() => {
  loadData()
})

const loadData = async () => {
  loading.value = true
  try {
    const params = {}
    if (dateRange.value?.length === 2) {
      params.startDate = dateRange.value[0] + 'T00:00:00'
      params.endDate = dateRange.value[1] + 'T23:59:59'
    }
    if (filterType.value) params.type = filterType.value
    if (filterSourceType.value) params.sourceType = parseInt(filterSourceType.value)

    const res = await getTransactions(params)
    allTransactions.value = res.data || []
    currentPage.value = 1
  } catch (error) {
    ElMessage.error('加载流水记录失败')
  } finally {
    loading.value = false
  }
}

const resetFilters = () => {
  dateRange.value = []
  filterType.value = ''
  filterSourceType.value = ''
  keyword.value = ''
  currentPage.value = 1
  loadData()
}

const onPageChange = (page) => {
  currentPage.value = page
}

const typeLabel = (type) => {
  const map = { INBOUND: '入库', OUTBOUND: '出库', LOCK: '锁定', UNLOCK: '解锁', RETURN: '归还' }
  return map[type] || type
}

const typeTagType = (type) => {
  const map = { INBOUND: 'success', OUTBOUND: 'warning', LOCK: 'danger', UNLOCK: 'info', RETURN: '' }
  return map[type] || ''
}

const sourceTypeClass = (sourceType) => {
  const map = { 0: 'source-manual', 1: 'source-purchase', 2: 'source-sel-lock', 3: 'source-sel-unlock', 4: 'source-sel-out' }
  return map[Number(sourceType)] || ''
}

const formatDate = (dateStr) => {
  if (!dateStr) return '-'
  return new Date(dateStr).toLocaleString('zh-CN', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' })
}
</script>

<style scoped>
.stock-history {
  padding: 16px;
  display: flex;
  flex-direction: column;
  gap: 12px;
  height: 100%;
  overflow: auto;
}

.filter-bar {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
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

.panel-card {
  background: var(--color-bg-card);
  border: 1px solid var(--color-border);
  border-radius: var(--radius);
  overflow: auto;
}

.text-muted {
  color: var(--color-text-muted);
  font-size: 12px;
}

/* 来源类型标签 */
.source-tag {
  font-size: 12px;
  padding: 3px 10px;
  border-radius: 4px;
  font-weight: 500;
}

.source-manual { background: #f5f5f5; color: #666666; border: 1px solid #d9d9d9; }
.source-purchase { background: #e6f7ff; color: #1890ff; border: 1px solid #91d5ff; }
.source-sel-lock { background: #fff1f0; color: #ff4d4f; border: 1px solid #ffccc7; }
.source-sel-unlock { background: #f6ffed; color: #52c41a; border: 1px solid #b7eb8f; }
.source-sel-out { background: #fff7e6; color: #fa8c16; border: 1px solid #ffd591; }

/* 类型标签 */
.type-tag {
  font-weight: 600;
  padding: 3px 8px;
}

.qty-positive { color: #52c41a; font-weight: 600; font-size: 14px; }
.qty-negative { color: #ff4d4f; font-weight: 600; font-size: 14px; }

.pagination-wrapper {
  display: flex;
  justify-content: flex-end;
  padding: 12px 16px;
  border-top: 1px solid var(--color-border);
}
</style>
