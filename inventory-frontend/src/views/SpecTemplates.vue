<template>
  <div class="page-main">
    <div style="padding: 20px 24px;">
      <div class="panel-card">
        <div style="padding: 12px 16px; display: flex; align-items: center; justify-content: space-between; border-bottom: 1px solid var(--color-border);">
          <span style="font-size: 14px; font-weight: 600;">规格模板</span>
          <el-button type="primary" size="small" @click="showDialog = true">添加模板</el-button>
        </div>
        <el-table :data="templates" stripe>
          <el-table-column prop="category" label="分类" min-width="150" />
          <el-table-column label="参数数量" width="100" align="center">
            <template #default="{row}">
              {{ row.paramDefs?.length || 0 }}
            </template>
          </el-table-column>
          <el-table-column label="操作" width="180" fixed="right">
            <template #default="{row}">
              <el-button size="small" @click="editTemplate(row)">编辑</el-button>
              <el-button size="small" type="danger" plain @click="deleteTemplate(row.id)">删除</el-button>
            </template>
          </el-table-column>
        </el-table>
      </div>
    </div>

    <!-- Template Dialog -->
    <el-dialog v-model="showDialog" :title="editingId ? '编辑模板' : '添加模板'" width="700px">
      <el-form :model="form" label-width="80px">
        <el-form-item label="分类" required>
          <el-input v-model="form.category" placeholder="例如: 电机, 轴承" />
        </el-form-item>

        <el-divider />
        <div style="margin-bottom: 10px;">
          <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 10px;">
            <span style="font-weight: 600;">参数定义</span>
            <el-button type="primary" size="small" @click="addParam">添加参数</el-button>
          </div>

          <div v-if="form.paramDefs && form.paramDefs.length > 0" style="max-height: 300px; overflow-y: auto;">
            <div v-for="(param, idx) in form.paramDefs" :key="idx" style="padding: 10px; background: var(--color-bg-hover); margin-bottom: 10px; border-radius: var(--radius);">
              <div style="display: flex; justify-content: space-between; align-items: start; margin-bottom: 8px;">
                <div style="font-weight: 600;">{{ param.label || '未命名参数' }}</div>
                <div style="display: flex; gap: 5px;">
                  <el-button type="primary" link size="small" @click="editParam(idx)">编辑</el-button>
                  <el-button type="danger" link size="small" @click="deleteParam(idx)">删除</el-button>
                </div>
              </div>
              <div style="color: var(--color-text-secondary); font-size: 12px;">
                <span>Key: {{ param.key }}</span>
                <span style="margin-left: 10px;">类型: {{ param.dataType }}</span>
                <span v-if="param.unit" style="margin-left: 10px;">单位: {{ param.unit }}</span>
                <span v-if="param.required" style="margin-left: 10px; color: #f56c6c;">必填</span>
              </div>
            </div>
          </div>
          <div v-else style="text-align: center; color: var(--color-text-muted); padding: 20px;">
            暂无参数定义
          </div>
        </div>
      </el-form>
      <template #footer>
        <el-button @click="showDialog = false">取消</el-button>
        <el-button type="primary" @click="saveTemplate">保存</el-button>
      </template>
    </el-dialog>

    <!-- Parameter Edit Dialog -->
    <el-dialog v-model="showParamDialog" :title="editingParamIndex !== null ? '编辑参数' : '添加参数'" width="500px">
      <el-form :model="currentParam" label-width="80px">
        <el-form-item label="Key" required>
          <el-input v-model="currentParam.key" placeholder="例如: voltage" />
        </el-form-item>
        <el-form-item label="标签" required>
          <el-input v-model="currentParam.label" placeholder="例如: 电压" />
        </el-form-item>
        <el-form-item label="单位">
          <el-input v-model="currentParam.unit" placeholder="例如: V, kW, rpm" />
        </el-form-item>
        <el-form-item label="数据类型" required>
          <el-select v-model="currentParam.dataType" style="width: 100%;">
            <el-option label="字符串" value="string" />
            <el-option label="数字" value="number" />
            <el-option label="布尔" value="boolean" />
            <el-option label="下拉选择" value="select" />
          </el-select>
        </el-form-item>
        <el-form-item v-if="currentParam.dataType === 'select'" label="选项" required>
          <el-input v-model="currentParam.optionsText" type="textarea" :rows="3" placeholder="每行一个选项，例如:&#10;选项1&#10;选项2&#10;选项3" />
        </el-form-item>
        <el-form-item label="必填">
          <el-checkbox v-model="currentParam.required" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showParamDialog = false">取消</el-button>
        <el-button type="primary" @click="saveParam">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { getTemplates, createTemplate, updateTemplate, deleteTemplate as deleteTemplateApi } from '@/api/templates'

const templates = ref([])
const showDialog = ref(false)
const editingId = ref(null)
const form = ref({
  category: '',
  paramDefs: []
})
const showParamDialog = ref(false)
const editingParamIndex = ref(null)
const currentParam = ref({
  key: '',
  label: '',
  unit: '',
  dataType: 'string',
  optionsText: '',
  options: [],
  required: false
})

onMounted(async () => {
  await loadTemplates()
})

const loadTemplates = async () => {
  try {
    const res = (await getTemplates()).data
    templates.value = res || []
  } catch (error) {
    ElMessage.error('Failed to load templates')
  }
}

const editTemplate = (row) => {
  editingId.value = row.id
  form.value = {
    category: row.category,
    paramDefs: row.paramDefs ? [...row.paramDefs.map(p => ({...p, optionsText: p.options ? p.options.join('\n') : ''}))] : []
  }
  showDialog.value = true
}

const saveTemplate = async () => {
  if (!form.value.category) {
    ElMessage.warning('Please enter category name')
    return
  }

  try {
    const templateData = {
      category: form.value.category,
      paramDefs: form.value.paramDefs.map(p => ({
        key: p.key,
        label: p.label,
        unit: p.unit || '',
        dataType: p.dataType,
        options: p.optionsText ? p.optionsText.split('\n').map(o => o.trim()).filter(o => o) : [],
        required: p.required || false
      }))
    }

    if (editingId.value) {
      await updateTemplate(editingId.value, templateData)
      ElMessage.success('Template updated')
    } else {
      await createTemplate(templateData)
      ElMessage.success('Template created')
    }

    showDialog.value = false
    editingId.value = null
    form.value = { category: '', paramDefs: [] }
    await loadTemplates()
  } catch (error) {
    ElMessage.error(error.response?.data?.message || 'Failed to save template')
  }
}

const deleteTemplate = async (id) => {
  try {
    await ElMessageBox.confirm('Delete this template?', 'Warning', { type: 'warning' })
    await deleteTemplateApi(id)
    ElMessage.success('Template deleted')
    await loadTemplates()
  } catch (error) {
    if (error.message !== 'cancel') {
      ElMessage.error('Failed to delete template')
    }
  }
}

const addParam = () => {
  editingParamIndex.value = null
  currentParam.value = {
    key: '',
    label: '',
    unit: '',
    dataType: 'string',
    optionsText: '',
    options: [],
    required: false
  }
  showParamDialog.value = true
}

const editParam = (idx) => {
  editingParamIndex.value = idx
  const param = form.value.paramDefs[idx]
  currentParam.value = {
    ...param,
    optionsText: param.options ? param.options.join('\n') : ''
  }
  showParamDialog.value = true
}

const saveParam = () => {
  if (!currentParam.value.key || !currentParam.value.label) {
    ElMessage.warning('Please enter key and label')
    return
  }

  if (currentParam.value.dataType === 'select' && !currentParam.value.optionsText) {
    ElMessage.warning('Select type requires options')
    return
  }

  const param = {
    key: currentParam.value.key,
    label: currentParam.value.label,
    unit: currentParam.value.unit || '',
    dataType: currentParam.value.dataType,
    options: currentParam.value.optionsText ? currentParam.value.optionsText.split('\n').map(o => o.trim()).filter(o => o) : [],
    required: currentParam.value.required || false
  }

  if (editingParamIndex.value !== null) {
    form.value.paramDefs[editingParamIndex.value] = param
  } else {
    form.value.paramDefs.push(param)
  }

  showParamDialog.value = false
  editingParamIndex.value = null
}

const deleteParam = (idx) => {
  form.value.paramDefs.splice(idx, 1)
}
</script>
