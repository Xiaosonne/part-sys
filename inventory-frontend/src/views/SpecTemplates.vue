<template>
  <div>
    <el-button type="primary" @click="showDialog = true" style="margin-bottom: 20px;">Add Template</el-button>

    <el-table :data="templates" stripe>
      <el-table-column prop="category" label="Category" />
      <el-table-column label="Parameters" width="120">
        <template #default="{row}">
          {{ row.paramDefs?.length || 0 }}
        </template>
      </el-table-column>
      <el-table-column label="Actions" width="200">
        <template #default="{row}">
          <el-button size="small" @click="editTemplate(row)">Edit</el-button>
          <el-button size="small" type="danger" @click="deleteTemplate(row.id)">Delete</el-button>
        </template>
      </el-table-column>
    </el-table>

    <!-- Template Dialog -->
    <el-dialog v-model="showDialog" :title="editingId ? 'Edit Template' : 'Add Template'" width="700px">
      <el-form :model="form" label-width="100px">
        <el-form-item label="Category" required>
          <el-input v-model="form.category" placeholder="e.g., Motor, Bearing" />
        </el-form-item>

        <el-divider />
        <div style="margin-bottom: 10px;">
          <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 10px;">
            <span style="font-weight: bold;">Parameters</span>
            <el-button type="primary" size="small" @click="addParam">Add Parameter</el-button>
          </div>

          <div v-if="form.paramDefs && form.paramDefs.length > 0" style="max-height: 300px; overflow-y: auto;">
            <div v-for="(param, idx) in form.paramDefs" :key="idx" style="padding: 10px; background: #f5f7fa; margin-bottom: 10px; border-radius: 4px;">
              <div style="display: flex; justify-content: space-between; align-items: start; margin-bottom: 8px;">
                <div style="font-weight: bold;">{{ param.label || 'Unnamed Parameter' }}</div>
                <div style="display: flex; gap: 5px;">
                  <el-button type="primary" link size="small" @click="editParam(idx)">Edit</el-button>
                  <el-button type="danger" link size="small" @click="deleteParam(idx)">Delete</el-button>
                </div>
              </div>
              <div style="color: #666; font-size: 12px;">
                <span>Key: {{ param.key }}</span>
                <span style="margin-left: 10px;">Type: {{ param.dataType }}</span>
                <span v-if="param.unit" style="margin-left: 10px;">Unit: {{ param.unit }}</span>
                <span v-if="param.required" style="margin-left: 10px; color: #f56c6c;">Required</span>
              </div>
            </div>
          </div>
          <div v-else style="text-align: center; color: #999; padding: 20px;">
            No parameters defined yet
          </div>
        </div>
      </el-form>
      <template #footer>
        <el-button @click="showDialog = false">Cancel</el-button>
        <el-button type="primary" @click="saveTemplate">Save</el-button>
      </template>
    </el-dialog>

    <!-- Parameter Edit Dialog -->
    <el-dialog v-model="showParamDialog" :title="editingParamIndex !== null ? 'Edit Parameter' : 'Add Parameter'" width="500px">
      <el-form :model="currentParam" label-width="100px">
        <el-form-item label="Key" required>
          <el-input v-model="currentParam.key" placeholder="e.g., voltage" />
        </el-form-item>
        <el-form-item label="Label" required>
          <el-input v-model="currentParam.label" placeholder="e.g., Voltage" />
        </el-form-item>
        <el-form-item label="Unit">
          <el-input v-model="currentParam.unit" placeholder="e.g., V, kW, rpm" />
        </el-form-item>
        <el-form-item label="Data Type" required>
          <el-select v-model="currentParam.dataType">
            <el-option label="String" value="string" />
            <el-option label="Number" value="number" />
            <el-option label="Boolean" value="boolean" />
            <el-option label="Select" value="select" />
          </el-select>
        </el-form-item>
        <el-form-item v-if="currentParam.dataType === 'select'" label="Options" required>
          <el-input v-model="currentParam.optionsText" type="textarea" rows="3" placeholder="One option per line, e.g.:&#10;Option 1&#10;Option 2&#10;Option 3" />
        </el-form-item>
        <el-form-item label="Required">
          <el-checkbox v-model="currentParam.required" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showParamDialog = false">Cancel</el-button>
        <el-button type="primary" @click="saveParam">Save</el-button>
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
