<template>
  <div class="project-detail">
    <el-button @click="$router.back()" style="margin-bottom: 20px;">← Back</el-button>
    <h2>{{ project.name }}</h2>

    <el-container style="height: calc(100vh - 150px);">
      <el-aside width="300px" style="border-right: 1px solid #ddd; overflow-y: auto;">
        <el-tree
          :data="treeData"
          node-key="id"
          :props="{ children: 'children', label: 'label' }"
          @node-click="handleNodeClick"
          :default-expanded-keys="['files']"
        />
      </el-aside>

      <el-main style="overflow-y: auto;">
        <!-- Files View -->
        <div v-if="activeNode === 'files'">
          <h3>File Management</h3>
          <div style="margin-bottom: 20px;">
            <el-breadcrumb separator="/">
              <el-breadcrumb-item @click="currentPath = ''">Root</el-breadcrumb-item>
              <el-breadcrumb-item v-for="(part, idx) in currentPath.split('/').filter(p => p)" :key="idx" @click="currentPath = currentPath.split('/').slice(0, idx + 1).join('/')">
                {{ part }}
              </el-breadcrumb-item>
            </el-breadcrumb>
          </div>

          <div style="margin-bottom: 20px;">
            <el-upload
              drag
              action="#"
              :auto-upload="false"
              @change="handleFileSelect"
              style="margin-bottom: 10px;"
            >
              <el-icon class="el-icon--upload"><upload-filled /></el-icon>
              <div class="el-upload__text">Drop file here or <em>click to upload</em></div>
            </el-upload>
            <el-button type="primary" @click="uploadFile" :loading="uploading">Upload</el-button>
            <el-button @click="showCreateFolderDialog = true">New Folder</el-button>
            <el-button type="warning" @click="handleReinitialize" :loading="reinitializing">Reinitialize Workspace</el-button>
          </div>

          <el-table :data="fileItems" style="margin-top: 20px; height: calc(100% - 150px);" max-height="100%">
            <el-table-column label="Name">
              <template #default="{ row }">
                <div v-if="row.isFolder" style="cursor: pointer; color: #409eff;" @click="enterFolder(row.name)">
                  📁 {{ row.displayName || row.name }}
                </div>
                <div v-else>📄 {{ row.originalName || row.name }}</div>
              </template>
            </el-table-column>
            <el-table-column prop="size" label="Size" width="120" :formatter="formatSize" />
            <el-table-column prop="modified" label="Modified" width="180" :formatter="formatDate" />
            <el-table-column label="Actions" width="180">
              <template #default="{ row }">
                <el-button v-if="!row.isFolder" size="small" @click="downloadFile(row)">Download</el-button>
                <el-button size="small" type="danger" @click="deleteItem(row)">Delete</el-button>
              </template>
            </el-table-column>
          </el-table>
        </div>

        <!-- Selections View -->
        <div v-else-if="activeNode === 'selections'">
          <h3>Selection Plans</h3>
          <el-table :data="selections">
            <el-table-column prop="name" label="Plan Name" />
            <el-table-column prop="status" label="Status" width="100" />
            <el-table-column label="Items" width="100">
              <template #default="{ row }">{{ row.items?.length || 0 }}</template>
            </el-table-column>
          </el-table>
        </div>

        <!-- Selection Detail View -->
        <div v-else-if="activeNode?.startsWith('selection-')">
          <el-button @click="activeNode = 'selections'" style="margin-bottom: 20px;">← Back</el-button>
          <h3>{{ selectedSelection?.name }}</h3>
          <el-table :data="selectionItems">
            <el-table-column prop="partName" label="Part" />
            <el-table-column prop="manufacturer" label="Manufacturer" width="120" />
            <el-table-column prop="model" label="Model" width="120" />
            <el-table-column prop="requiredQty" label="Required" width="100" />
            <el-table-column prop="totalQty" label="Total" width="100" />
            <el-table-column prop="availableQty" label="Available" width="100" />
          </el-table>
        </div>

        <!-- Part Files View -->
        <div v-else-if="activeNode?.startsWith('part-')">
          <h3>{{ selectedPartDetail?.name }}</h3>
          <div style="margin-bottom: 20px; padding: 10px; background: #f5f7fa; border-radius: 4px;">
            <el-row :gutter="20">
              <el-col :span="6">Model: {{ selectedPartDetail?.model }}</el-col>
              <el-col :span="6">Manufacturer: {{ selectedPartDetail?.manufacturer }}</el-col>
              <el-col :span="6">Brand: {{ selectedPartDetail?.brand }}</el-col>
              <el-col :span="6">Category: {{ selectedPartDetail?.category }}</el-col>
            </el-row>
          </div>

          <div style="margin-bottom: 20px;">
            <el-breadcrumb separator="/">
              <el-breadcrumb-item @click="partCurrentPath = ''; loadPartFiles()">Root</el-breadcrumb-item>
              <el-breadcrumb-item v-for="(part, idx) in partCurrentPath.split('/').filter(p => p)" :key="idx" @click="partCurrentPath = partCurrentPath.split('/').slice(0, idx + 1).join('/'); loadPartFiles()">
                {{ part }}
              </el-breadcrumb-item>
            </el-breadcrumb>
          </div>

          <el-table :data="partFileItems" style="margin-top: 20px; height: calc(100% - 200px);" max-height="100%">
            <el-table-column label="Name">
              <template #default="{ row }">
                <div v-if="row.isFolder" style="cursor: pointer; color: #409eff;" @click="partCurrentPath = row.path; loadPartFiles()">
                  📁 {{ row.name }}
                </div>
                <div v-else>📄 {{ row.originalName || row.name }}</div>
              </template>
            </el-table-column>
            <el-table-column prop="size" label="Size" width="120" :formatter="formatSize" />
            <el-table-column prop="modified" label="Modified" width="180" :formatter="formatDate" />
            <el-table-column label="Actions" width="180">
              <template #default="{ row }">
                <el-button v-if="!row.isFolder" size="small" @click="downloadFile(row)">Download</el-button>
              </template>
            </el-table-column>
          </el-table>
        </div>

        <!-- Create Folder Dialog -->
        <el-dialog v-model="showCreateFolderDialog" title="Create Folder" width="400px">
          <el-input v-model="newFolderName" placeholder="Folder name" />
          <template #footer>
            <el-button @click="showCreateFolderDialog = false">Cancel</el-button>
            <el-button type="primary" @click="createFolder">Create</el-button>
          </template>
        </el-dialog>
      </el-main>
    </el-container>
  </div>
</template>

<script setup>
import { ref, onMounted, watch } from 'vue'
import { useRoute } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { UploadFilled } from '@element-plus/icons-vue'
import { getProject } from '@/api/projects'
import { getSelections } from '@/api/selections'
import { getParts } from '@/api/parts'
import { uploadFile as uploadFileApi, getProjectFiles, deleteFile as deleteFileApi, listFiles, createFolder as createFolderApi, deleteFolder as deleteFolderApi, reinitializeProjectWorkspace } from '@/api/files'

const route = useRoute()
const project = ref({})
const selections = ref([])
const files = ref([])
const fileItems = ref([])
const parts = ref([])
const activeNode = ref('files')
const selectedFile = ref(null)
const uploading = ref(false)
const selectedSelection = ref(null)
const selectionItems = ref([])
const treeData = ref([])
const currentPath = ref('')
const showCreateFolderDialog = ref(false)
const newFolderName = ref('')
const currentPartId = ref(null)
const partFileItems = ref([])
const partCurrentPath = ref('')
const selectedPartDetail = ref(null)
const reinitializing = ref(false)

onMounted(async () => {
  await loadProject()
  await loadSelections()
  await loadFiles()
  await loadParts()
  buildTreeData()
})

watch(currentPath, () => {
  loadFiles()
})

watch(partCurrentPath, () => {
  loadPartFiles()
})

const loadProject = async () => {
  try {
    const res = await getProject(route.params.id)
    project.value = res.data
  } catch (error) {
    ElMessage.error('Failed to load project')
  }
}

const loadSelections = async () => {
  try {
    const res = await getSelections(route.params.id)
    selections.value = res.data
  } catch (error) {
    ElMessage.error('Failed to load selections')
  }
}

const loadFiles = async () => {
  try {
    const res = await listFiles('projects', currentPath.value || null, route.params.id)
    fileItems.value = res.data
  } catch (error) {
    ElMessage.error('Failed to load files')
  }
}

const loadParts = async () => {
  try {
    const res = await getParts()
    parts.value = res.data
  } catch (error) {
    ElMessage.error('Failed to load parts')
  }
}

const loadPartFiles = async () => {
  try {
    const res = await listFiles('parts', partCurrentPath.value || null, currentPartId.value)
    partFileItems.value = res.data
  } catch (error) {
    ElMessage.error('Failed to load part files')
  }
}

const buildTreeData = async () => {
  const fileTree = await buildFileTree('')
  treeData.value = [
    {
      id: 'files',
      label: 'File Management',
      children: fileTree
    },
    {
      id: 'selections',
      label: 'Selection Plans',
      children: selections.value.map(s => ({
        id: `selection-${s.id}`,
        label: s.name,
        data: s,
        children: s.items.map(item => {
          const part = parts.value.find(p => p.id === item.selectedPartId)
          return {
            id: `part-${item.selectedPartId}`,
            label: part?.name || 'Unknown Part',
            partId: item.selectedPartId,
            isPart: true
          }
        })
      }))
    }
  ]
}

const buildFileTree = async (path) => {
  try {
    const res = await listFiles('projects', path || null, route.params.id)
    const items = []
    for (const item of res.data) {
      if (item.isFolder) {
        const node = {
          id: `file-${item.path}`,
          label: item.displayName || item.name,
          isFolder: true,
          path: item.path,
          data: item,
          children: await buildFileTree(item.path)
        }
        items.push(node)
      }
    }
    return items
  } catch (error) {
    return []
  }
}

const handleNodeClick = async (data) => {
  if (data.id === 'files') {
    currentPath.value = ''
    activeNode.value = 'files'
  } else if (data.id === 'selections') {
    activeNode.value = 'selections'
  } else if (data.id.startsWith('selection-')) {
    selectedSelection.value = data.data
    selectionItems.value = data.data.items.map(item => {
      const part = parts.value.find(p => p.id === item.selectedPartId)
      return {
        partName: part?.name || 'Unknown',
        manufacturer: part?.manufacturer || '-',
        model: part?.model || '-',
        requiredQty: item.requiredQty,
        totalQty: part?.totalQty || 0,
        availableQty: part?.availableQty || 0
      }
    })
    activeNode.value = data.id
  } else if (data.id.startsWith('part-') && data.isPart) {
    currentPartId.value = data.partId
    selectedPartDetail.value = parts.value.find(p => p.id === data.partId)
    partCurrentPath.value = ''
    await loadPartFiles()
    activeNode.value = data.id
  } else if (data.id.startsWith('file-') && data.isFolder) {
    currentPath.value = data.path
    activeNode.value = 'files'
  }
}

const handleFileSelect = (file) => {
  selectedFile.value = file.raw
}

const uploadFile = async () => {
  if (!selectedFile.value) {
    ElMessage.warning('Please select a file')
    return
  }

  uploading.value = true
  try {
    const formData = new FormData()
    formData.append('file', selectedFile.value)
    formData.append('bucket', 'projects')
    formData.append('relatedId', route.params.id)
    formData.append('fileType', 'PROJECT')
    if (currentPath.value) {
      formData.append('path', currentPath.value)
    }

    await uploadFileApi(formData)
    ElMessage.success('File uploaded')
    selectedFile.value = null
    await loadFiles()
  } catch (error) {
    ElMessage.error('Upload failed')
  } finally {
    uploading.value = false
  }
}

const downloadFile = (file) => {
  window.open(`http://localhost:5128/api/files/${file.id}/download`)
}

const deleteItem = async (item) => {
  try {
    if (item.isFolder) {
      await deleteFolderApi('projects', route.params.id, item.path)
      ElMessage.success('Folder deleted')
      await buildTreeData()
      await loadFiles()
    } else {
      await deleteFileApi(item.id)
      ElMessage.success('File deleted')
      await loadFiles()
    }
  } catch (error) {
    ElMessage.error('Delete failed')
  }
}

const enterFolder = (folderName) => {
  currentPath.value = currentPath.value ? `${currentPath.value}/${folderName}` : folderName
  loadFiles()
}

const createFolder = async () => {
  if (!newFolderName.value) {
    ElMessage.warning('Please enter folder name')
    return
  }

  try {
    const folderPath = currentPath.value ? `${currentPath.value}/${newFolderName.value}` : newFolderName.value
    await createFolderApi('projects', route.params.id, folderPath)
    ElMessage.success('Folder created')
    newFolderName.value = ''
    showCreateFolderDialog.value = false
    await loadFiles()
    await buildTreeData()
  } catch (error) {
    ElMessage.error('Failed to create folder')
  }
}

const formatSize = (row) => {
  const size = row.size
  if (size < 1024) return size + ' B'
  if (size < 1024 * 1024) return (size / 1024).toFixed(2) + ' KB'
  return (size / (1024 * 1024)).toFixed(2) + ' MB'
}

const formatDate = (row) => {
  return new Date(row.modified).toLocaleDateString()
}

const handleReinitialize = async () => {
  try {
    await ElMessageBox.confirm('This will delete all files and reinitialize the workspace structure. Continue?', 'Warning', {
      confirmButtonText: 'OK',
      cancelButtonText: 'Cancel',
      type: 'warning'
    })

    reinitializing.value = true
    await reinitializeProjectWorkspace(route.params.id)
    ElMessage.success('Workspace reinitialized successfully')
    await loadFiles()
    await buildTreeData()
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error('Failed to reinitialize workspace')
    }
  } finally {
    reinitializing.value = false
  }
}
</script>

<style scoped>
.project-detail {
  padding: 20px;
}

h2 {
  margin-bottom: 20px;
}

h3 {
  margin-bottom: 15px;
}

.el-container {
  border: 1px solid #ddd;
  border-radius: 4px;
}

.el-aside {
  background-color: #f5f7fa;
}
</style>
