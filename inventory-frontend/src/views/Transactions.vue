<template>
  <div class="page-main">
    <div style="padding: 20px 24px;">
      <div class="panel-card">
        <div style="padding: 16px 20px; border-bottom: 1px solid var(--color-border);">
          <span style="font-size: 14px; font-weight: 600; color: var(--color-text-primary);">库存事务记录</span>
        </div>
        <el-table :data="transactions" stripe style="width: 100%;">
          <el-table-column prop="type" label="类型" width="100" />
          <el-table-column prop="partName" label="配件名称" min-width="150" />
          <el-table-column prop="quantity" label="数量" width="80" align="center" />
          <el-table-column prop="createdAt" label="时间" width="180" />
          <el-table-column prop="remark" label="备注" min-width="120" />
        </el-table>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { getTransactions } from '@/api/stock'

const transactions = ref([])

onMounted(async () => {
  try {
    const res = await getTransactions()
    transactions.value = res.data || []
  } catch (error) {
    ElMessage.error('加载事务记录失败')
  }
})
</script>
