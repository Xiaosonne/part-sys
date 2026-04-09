<template>
  <div class="outbound-form">
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

      <el-form-item label="出库数量">
        <el-input-number v-model="form.quantity" :min="1" :max="form.maxQty" />
      </el-form-item>

      <el-form-item label="关联项目" required>
        <el-select v-model="form.projectId" placeholder="选择项目" style="width: 100%;" @change="onProjectChange">
          <el-option v-for="proj in projects" :key="proj.id" :label="proj.name" :value="proj.id" />
        </el-select>
      </el-form-item>

      <el-form-item v-if="form.projectId" label="关联选型">
        <el-select v-model="form.selectionPlanId" placeholder="选择选型单（可选）" style="width: 100%;" clearable>
          <el-option
            v-for="plan in selectionPlans"
            :key="plan.id"
            :label="plan.name"
            :value="plan.id"
          />
        </el-select>
      </el-form-item>

      <el-form-item label="用途">
        <el-select v-model="form.usage" placeholder="选择用途" style="width: 300px;">
          <el-option label="生产使用" value="生产使用" />
          <el-option label="测试" value="测试" />
          <el-option label="维修" value="维修" />
          <el-option label="其他" value="其他" />
        </el-select>
      </el-form-item>

      <el-form-item label="备注">
        <el-input v-model="form.note" placeholder="可选" />
      </el-form-item>

      <el-form-item>
        <el-button type="primary" @click="handleSubmit" :loading="submitting">提交出库</el-button>
      </el-form-item>
    </el-form>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { getParts } from '@/api/parts'
import { getProjects } from '@/api/projects'
import { getSelections } from '@/api/selections'
import { outbound } from '@/api/stock'

const loading = ref(false)
const submitting = ref(false)
const parts = ref([])
const projects = ref([])
const selectionPlansByProject = ref({})

const form = ref({
  partId: '',
  quantity: 1,
  maxQty: Infinity,
  projectId: '',
  selectionPlanId: '',
  usage: '',
  note: ''
})

const selectionPlans = computed(() => {
  if (!form.value.projectId) return []
  return selectionPlansByProject.value[form.value.projectId] || []
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

const onProjectChange = async () => {
  form.value.selectionPlanId = ''
  const pid = form.value.projectId
  if (pid && !selectionPlansByProject.value[pid]) {
    try {
      const res = await getSelections(pid)
      selectionPlansByProject.value[pid] = (res.data || []).filter(p =>
        p.status === 'Submitted' || p.status === 1
      )
    } catch (error) {
      console.error('加载选型单失败', error)
    }
  }
}

const handleSubmit = async () => {
  if (!form.value.partId) {
    ElMessage.warning('请选择配件')
    return
  }
  if (!form.value.projectId) {
    ElMessage.warning('请选择关联项目')
    return
  }
  submitting.value = true
  try {
    await outbound({
      partId: form.value.partId,
      quantity: form.value.quantity,
      projectId: form.value.projectId,
      selectionPlanId: form.value.selectionPlanId || null,
      usage: form.value.usage || null,
      note: form.value.note || null
    })
    ElMessage.success('出库成功')
    form.value = { partId: '', quantity: 1, maxQty: Infinity, projectId: '', selectionPlanId: '', usage: '', note: '' }
    await loadParts()
  } catch (error) {
    ElMessage.error(error.response?.data?.message || '出库失败')
  } finally {
    submitting.value = false
  }
}
</script>

<style scoped>
.outbound-form {
  padding: 20px;
  width: 100%;
}

.outbound-form :deep(.el-form) {
  background: var(--color-bg-card);
  border: 1px solid var(--color-border);
  border-radius: var(--radius);
  padding: 24px;
}
</style>
