<template>
  <div class="lock-form">
    <el-form :model="form" label-width="100px">
      <el-form-item label="配件" required>
        <el-select v-model="form.partId" placeholder="选择配件" style="width: 100%;" @change="onPartChange">
          <el-option
            v-for="part in parts"
            :key="part.id"
            :label="part.name + ' - ' + part.model + ' (可用: ' + part.availableQty + ')'"
            :value="part.id"
          />
        </el-select>
      </el-form-item>

      <el-form-item label="锁定数量">
        <el-input-number v-model="form.quantity" :min="1" :max="form.maxQty" />
      </el-form-item>

      <el-form-item label="关联项目">
        <el-select v-model="form.projectId" placeholder="选择项目（可选）" style="width: 100%;" clearable>
          <el-option v-for="proj in projects" :key="proj.id" :label="proj.name" :value="proj.id" />
        </el-select>
      </el-form-item>

      <el-form-item label="备注">
        <el-input v-model="form.note" placeholder="可选" />
      </el-form-item>

      <el-form-item>
        <el-button type="primary" @click="handleSubmit" :loading="submitting">提交锁定</el-button>
      </el-form-item>
    </el-form>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { getParts } from '@/api/parts'
import { getProjects } from '@/api/projects'
import { lock } from '@/api/stock'

const submitting = ref(false)
const parts = ref([])
const projects = ref([])

const form = ref({
  partId: '',
  quantity: 1,
  maxQty: Infinity,
  projectId: '',
  note: ''
})

onMounted(async () => {
  await Promise.all([loadParts(), loadProjects()])
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

const onPartChange = (partId) => {
  const part = parts.value.find(p => p.id === partId)
  form.value.maxQty = part?.availableQty || 0
  form.value.quantity = 1
}

const handleSubmit = async () => {
  if (!form.value.partId) {
    ElMessage.warning('请选择配件')
    return
  }
  submitting.value = true
  try {
    await lock({
      partId: form.value.partId,
      quantity: form.value.quantity,
      projectId: form.value.projectId || null,
      note: form.value.note || null
    })
    ElMessage.success('锁定成功')
    form.value = { partId: '', quantity: 1, maxQty: Infinity, projectId: '', note: '' }
    await loadParts()
  } catch (error) {
    ElMessage.error(error.response?.data?.message || '锁定失败')
  } finally {
    submitting.value = false
  }
}
</script>

<style scoped>
.lock-form {
  padding: 20px;
  width: 100%;
}

.lock-form :deep(.el-form) {
  background: var(--color-bg-card);
  border: 1px solid var(--color-border);
  border-radius: var(--radius);
  padding: 24px;
}
</style>
