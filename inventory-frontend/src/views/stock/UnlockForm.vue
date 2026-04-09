<template>
  <div class="unlock-form">
    <el-form :model="form" label-width="100px">
      <el-form-item label="配件" required>
        <el-select v-model="form.partId" placeholder="选择配件" style="width: 100%;" @change="onPartChange">
          <el-option
            v-for="part in lockedParts"
            :key="part.id"
            :label="part.name + ' - ' + part.model + ' (已锁定: ' + part.lockedQty + ')'"
            :value="part.id"
          />
        </el-select>
      </el-form-item>

      <el-form-item label="解锁数量">
        <el-input-number v-model="form.quantity" :min="1" :max="form.maxQty" />
      </el-form-item>

      <el-form-item label="备注">
        <el-input v-model="form.note" placeholder="可选" />
      </el-form-item>

      <el-form-item>
        <el-button type="primary" @click="handleSubmit" :loading="submitting">提交解锁</el-button>
      </el-form-item>
    </el-form>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { getParts } from '@/api/parts'
import { unlock } from '@/api/stock'

const submitting = ref(false)
const parts = ref([])

const form = ref({
  partId: '',
  quantity: 1,
  maxQty: Infinity,
  note: ''
})

const lockedParts = computed(() => parts.value.filter(p => p.lockedQty > 0))

onMounted(async () => {
  await loadParts()
})

const loadParts = async () => {
  try {
    const res = await getParts()
    parts.value = res.data || []
  } catch (error) {
    console.error('加载配件列表失败', error)
  }
}

const onPartChange = (partId) => {
  const part = parts.value.find(p => p.id === partId)
  form.value.maxQty = part?.lockedQty || 0
  form.value.quantity = 1
}

const handleSubmit = async () => {
  if (!form.value.partId) {
    ElMessage.warning('请选择配件')
    return
  }
  submitting.value = true
  try {
    await unlock({
      partId: form.value.partId,
      quantity: form.value.quantity,
      note: form.value.note || null
    })
    ElMessage.success('解锁成功')
    form.value = { partId: '', quantity: 1, maxQty: Infinity, note: '' }
    await loadParts()
  } catch (error) {
    ElMessage.error(error.response?.data?.message || '解锁失败')
  } finally {
    submitting.value = false
  }
}
</script>

<style scoped>
.unlock-form {
  padding: 20px;
  width: 100%;
}

.unlock-form :deep(.el-form) {
  background: var(--color-bg-card);
  border: 1px solid var(--color-border);
  border-radius: var(--radius);
  padding: 24px;
}
</style>
