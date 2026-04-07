<template>
  <div>
    <el-button type="primary" @click="showDialog = true" style="margin-bottom: 20px;">Add Project</el-button>
    <el-table :data="projects" :tree-props="{ children: 'children' }" @expand-change="handleExpand">
      <el-table-column type="expand">
        <template #default="{ row }">
          <div v-if="row.type !== 'folder'" style="padding: 20px;">
            <div v-if="projectDetails.has(row.id)">
              <div v-if="projectDetails.get(row.id).length === 0" style="color: #909399;">暂无选型计划</div>
              <div v-for="selection in projectDetails.get(row.id)" :key="selection.id" style="margin-bottom: 20px;">
                <div style="display: flex; align-items: center; gap: 10px; margin: 10px 0;">
                  <h4 style="margin: 0;">{{ selection.name }}</h4>
                  <el-tag size="small" :type="statusType(selection.status)">{{ statusText(selection.status) }}</el-tag>
                </div>
                <el-table :data="selection.items" size="small" style="width: 100%;">
                  <el-table-column prop="partName" label="配件名称" />
                  <el-table-column prop="requiredQty" label="需求数量" width="90" align="center" />
                  <el-table-column prop="lockedQty" label="已锁定" width="80" align="center" />
                  <el-table-column prop="outboundQty" label="已出库" width="80" align="center" />
                  <el-table-column prop="pendingQty" label="待采购" width="80" align="center" />
                </el-table>
              </div>
            </div>
          </div>
        </template>
      </el-table-column>
      <el-table-column prop="name" label="Name" />
      <el-table-column prop="type" label="Type" />
      <el-table-column prop="status" label="Status" />
      <el-table-column prop="createdAt" label="Created" />
      <el-table-column label="Actions" width="200">
        <template #default="{ row }">
          <el-button v-if="row.type === 'project'" size="small" @click="viewDetail(row)">Detail</el-button>
          <el-button size="small" @click="editProject(row)">Edit</el-button>
          <el-button size="small" type="danger" @click="deleteProject(row.id)">Delete</el-button>
        </template>
      </el-table-column>
    </el-table>

    <el-dialog v-model="showDialog" :title="editingId ? 'Edit Project' : 'Add Project'">
      <el-form :model="form">
        <el-form-item label="Name">
          <el-input v-model="form.name" />
        </el-form-item>
        <el-form-item label="Type">
          <el-select v-model="form.type">
            <el-option label="Folder" value="folder" />
            <el-option label="Project" value="project" />
          </el-select>
        </el-form-item>
        <el-form-item label="Parent">
          <el-select v-model="form.parentId" clearable>
            <el-option v-for="p in projects" :key="p.id" :label="p.name" :value="p.id" />
          </el-select>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showDialog = false">Cancel</el-button>
        <el-button type="primary" @click="saveProject">Save</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { getProjects, createProject, updateProject, deleteProject as deleteProjectApi } from '@/api/projects'
import { getSelections } from '@/api/selections'

const router = useRouter()
const projects = ref([])
const showDialog = ref(false)
const editingId = ref(null)
const form = ref({ name: '', type: 'project', parentId: null })
const projectDetails = ref(new Map())

onMounted(async () => {
  await loadProjects()
})

const loadProjects = async () => {
  try {
    const res = await getProjects()
    projects.value = res.data
  } catch (error) {
    ElMessage.error('Failed to load projects')
  }
}

const handleExpand = async (row, expandedRows) => {
  if (row.type === 'folder') return
  if (expandedRows.some(r => r.id === row.id) && !projectDetails.value.has(row.id)) {
    await loadProjectDetails(row.id)
  }
}

const loadProjectDetails = async (projectId) => {
  try {
    const res = await getSelections(projectId)
    const selections = res.data.map(selection => ({
      id: selection.id,
      name: selection.name,
      status: selection.status,
      items: selection.items.map(item => ({
        partName: item.partName || `配件ID: ${item.selectedPartId} (已删除)`,
        requiredQty: item.requiredQty,
        lockedQty: item.lockedQty || 0,
        outboundQty: item.outboundQty || 0,
        pendingQty: item.pendingQty || 0
      }))
    }))
    projectDetails.value.set(projectId, selections)
  } catch (error) {
    ElMessage.error('Failed to load project details')
  }
}

const viewDetail = (row) => {
  router.push({ name: 'ProjectDetail', params: { id: row.id } })
}

const editProject = (row) => {
  editingId.value = row.id
  form.value = { ...row }
  showDialog.value = true
}

const saveProject = async () => {
  try {
    if (editingId.value) {
      await updateProject(editingId.value, form.value)
      ElMessage.success('Project updated')
    } else {
      await createProject(form.value)
      ElMessage.success('Project created')
    }
    showDialog.value = false
    editingId.value = null
    form.value = { name: '', type: 'project', parentId: null }
    await loadProjects()
  } catch (error) {
    ElMessage.error(error.response?.data?.message || 'Failed to save project')
  }
}

const deleteProject = async (id) => {
  try {
    await ElMessageBox.confirm('Delete this project?', 'Warning', { type: 'warning' })
    await deleteProjectApi(id)
    ElMessage.success('Project deleted')
    await loadProjects()
  } catch (error) {
    if (error.message !== 'cancel') {
      ElMessage.error('Failed to delete project')
    }
  }
}

const statusType = (status) => {
  const map = { Draft: 'info', Submitted: 'warning', Completed: 'success', Cancelled: 'danger', 0: 'info', 1: 'warning', 2: 'success', 3: 'danger' }
  return map[status] || 'info'
}

const statusText = (status) => {
  const map = { Draft: '草稿', Submitted: '已提交', Completed: '已完成', Cancelled: '已取消', 0: '草稿', 1: '已提交', 2: '已完成', 3: '已取消' }
  return map[status] || status
}
</script>
