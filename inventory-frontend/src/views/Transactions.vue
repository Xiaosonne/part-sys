<template>
  <div>
    <el-table :data="transactions">
      <el-table-column prop="type" label="Type" />
      <el-table-column prop="partName" label="Part" />
      <el-table-column prop="quantity" label="Quantity" />
      <el-table-column prop="createdAt" label="Time" />
      <el-table-column prop="remark" label="Remark" />
    </el-table>
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
    transactions.value = res.data
  } catch (error) {
    ElMessage.error('Failed to load transactions')
  }
})
</script>
