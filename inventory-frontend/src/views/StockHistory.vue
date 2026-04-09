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
      <el-button type="primary" @click="loadData" :loading="loading">搜索</el-button>
      <el-button @click="resetFilters">重置</el-button>
    </div>

    <!-- 流水列表 -->
    <div class="panel-card">
      <el-table
        :data="transactions"
        stripe
        style="width: 100%;"
        v-loading="loading"
        max-height="500"
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
        <el-table-column prop="type" label="类型" width="90" align="center">
          <template #default="{ row }">
            <el-tag :type="typeTagType(row.type)" size="small">{{ typeLabel(row.type) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="sourceTypeName" label="来源" width="100" align="center">
          <template #default="{ row }">
            <span class="source-tag" :class="sourceTypeClass(row.sourceType)">{{ row.sourceTypeName }}</span>
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

      <div class="pagination-wrapper" v-if="total > pageSize">
        <el-pagination
          background
          layout="prev, pager, next, total"
          :total="total"
          :page-size="pageSize"
          :current-page="currentPage"
          @current-change="onPageChange"
        />
      </div>

      <el-empty v-if="!loading && transactions.length === 0" description="暂无流水记录" />
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { getTransactions } from '@/api/stock'

const loading = ref(false)
const transactions = ref([])
const total = ref(0)
const currentPage = ref(1)
const pageSize = ref(50)

const dateRange = ref([])
const filterType = ref('')
const filterSourceType = ref('')
const keyword = ref('')

onMounted(() => {
  loadData()
})

const loadData = async () => {
  loading.value = true
  try {
    const params = {
      page: currentPage.value,
      pageSize: pageSize.value
    }
    if (dateRange.value?.length === 2) {
      params.startDate = dateRange.value[0] + 'T00:00:00'
      params.endDate = dateRange.value[1] + 'T23:59:59'
    }
    if (filterType.value) params.type = filterType.value
    if (filterSourceType.value) params.sourceType = parseInt(filterSourceType.value)
    if (keyword.value) {
      // keyword filtering will be done client-side for simplicity
    }

    const res = await getTransactions(params)
    let data = res.data || []

    // Client-side keyword filter
    if (keyword.value) {
      const kw = keyword.value.toLowerCase()
      data = data.filter(t =>
        t.partName.toLowerCase().includes(kw) ||
        t.partModel.toLowerCase().includes(kw)
      )
    }

    transactions.value = data
    total.value = data.length
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
  loadData()
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
  return map[sourceType] || ''
}

const formatDate = (dateStr) => {
  if (!dateStr) return '-'
  return new Date(dateStr).toLocaleString('zh-CN', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' })
}
</script>

<style scoped>
.stock-history {
  padding: 16px 20px;
  display: flex;
  flex-direction: column;
  gap: 16px;
  height: 100%;
  overflow: auto;
}

.filter-bar {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 8px;
  padding: 12px 16px;
  background: #f5f7fa;
  border-radius: 8px;
}

.text-muted {
  color: #999;
  font-size: 12px;
}

.source-tag {
  font-size: 11px;
  padding: 2px 6px;
  border-radius: 4px;
}

.source-manual { background: #f4f4f5; color: #909399; }
.source-purchase { background: #ecf5ff; color: #409eff; }
.source-sel-lock { background: #fef0f0; color: #f56c6c; }
.source-sel-unlock { background: #f0f9ff; color: #67c23a; }
.source-sel-out { background: #fef9f0; color: #e6a23c; }

.qty-positive { color: #67c23a; font-weight: 600; }
.qty-negative { color: #f56c6c; font-weight: 600; }

.pagination-wrapper {
  display: flex;
  justify-content: center;
  padding: 16px;
  border-top: 1px solid var(--color-border);
}
</style>
