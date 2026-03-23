<template>
  <div>
    <el-button type="primary" @click="showDialog = true" style="margin-bottom: 20px;">Add Selection</el-button>
    <el-table :data="selections">
      <el-table-column prop="name" label="Name" />
      <el-table-column prop="projectName" label="Project" />
      <el-table-column prop="status" label="Status" />
      <el-table-column prop="createdAt" label="Created" />
      <el-table-column label="Actions" width="200">
        <template #default="{ row }">
          <el-button size="small" @click="editSelection(row)">Edit</el-button>
          <el-button size="small" @click="submitSelection(row.id)" :disabled="row.status === 'submitted'">Submit</el-button>
          <el-button size="small" type="danger" @click="deleteSelection(row.id)">Delete</el-button>
        </template>
      </el-table-column>
    </el-table>

    <el-dialog v-model="showDialog" :title="editingId ? 'Edit Selection' : 'Add Selection'">
      <el-form :model="form">
        <el-form-item label="Name">
          <el-input v-model="form.name" />
        </el-form-item>
        <el-form-item label="Project">
          <el-select v-model="form.projectId">
            <el-option v-for="p in projects" :key="p.id" :label="p.name" :value="p.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="Items (JSON)">
          <el-input v-model="form.itemsJson" type="textarea" rows="5" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showDialog = false">Cancel</el-button>
        <el-button type="primary" @click="saveSelection">Save</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { getSelections, createSelection, updateSelection, deleteSelection as deleteSelectionApi, submitSelection as submitSelectionApi } from '@/api/selections'
import { getProjects } from '@/api/projects'

const selections = ref([])
const projects = ref([])
const showDialog = ref(false)
const editingId = ref(null)
const form = ref({ name: '', projectId: '', itemsJson: '[]' })

onMounted(async () => {
  await loadSelections()
  await loadProjects()
})

const loadSelections = async () => {
  try {
    const res = await getSelections()
    selections.value = res.data
  } catch (error) {
    ElMessage.error('Failed to load selections')
  }
}

const loadProjects = async () => {
  try {
    const res = await getProjects()
    projects.value = res.data
  } catch (error) {
    ElMessage.error('Failed to load projects')
  }
}

const editSelection = (row) => {
  editingId.value = row.id
  form.value = {
    name: row.name,
    projectId: row.projectId,
    itemsJson: JSON.stringify(row.items || [])
  }
  showDialog.value = true
}

const saveSelection = async () => {
  try {
    const data = {
      name: form.value.name,
      projectId: form.value.projectId,
      items: JSON.parse(form.value.itemsJson)
    }
    if (editingId.value) {
      await updateSelection(editingId.value, data)
      ElMessage.success('Selection updated')
    } else {
      await createSelection(data)
      ElMessage.success('Selection created')
    }
    showDialog.value = false
    editingId.value = null
    form.value = { name: '', projectId: '', itemsJson: '[]' }
    await loadSelections()
  } catch (error) {
    ElMessage.error(error.response?.data?.message || 'Failed to save selection')
  }
}

const submitSelection = async (id) => {
  try {
    await submitSelectionApi(id)
    ElMessage.success('Selection submitted')
    await loadSelections()
  } catch (error) {
    ElMessage.error(error.response?.data?.message || 'Failed to submit selection')
  }
}

const deleteSelection = async (id) => {
  try {
    await ElMessageBox.confirm('Delete this selection?', 'Warning', { type: 'warning' })
    await deleteSelectionApi(id)
    ElMessage.success('Selection deleted')
    await loadSelections()
  } catch (error) {
    if (error.message !== 'cancel') {
      ElMessage.error('Failed to delete selection')
    }
  }
}
</script>
