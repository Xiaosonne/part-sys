<template>
  <div class="page-container">
    <!-- Left: Project/Folder Tree -->
    <div class="page-sidebar">
      <div class="left-panel">
        <el-input v-model="treeSearch" placeholder="搜索项目/文件夹" prefix-icon="Search" clearable
          style="margin-bottom: 10px;" />

        <el-popover placement="right" :width="300" trigger="click" v-model:visible="showNewNodePopover">
          <template #reference>
            <el-button type="primary" size="small" style="width: 100%; margin-bottom: 10px;">+ 新建</el-button>
          </template>
          <el-form :model="newNodeForm" label-width="80px" size="small">
            <el-form-item label="类型">
              <el-select v-model="newNodeForm.type" style="width: 100%;">
                <el-option label="项目" value="project" />
                <el-option label="文件夹" value="folder" />
              </el-select>
            </el-form-item>
            <el-form-item label="名称">
              <el-input v-model="newNodeForm.name" placeholder="名称" />
            </el-form-item>
            <el-form-item label="父级">
              <el-select v-model="newNodeForm.parentId" placeholder="根级" clearable style="width: 100%;">
                <el-option v-for="p in flatProjects" :key="p.id" :label="p.name" :value="p.id" />
              </el-select>
            </el-form-item>
            <el-form-item>
              <el-button type="primary" size="small" @click="saveNewNode" style="width: 100%;">创建</el-button>
            </el-form-item>
          </el-form>
        </el-popover>

        <el-scrollbar class="tree-scrollbar">
          <el-tree ref="treeRef" :data="treeData" :props="{ label: 'name', children: 'children' }"
            :expand-on-click-node="true" node-key="id" :current-node-key="currentNodeKey"
            :filter-node-method="filterTreeNode" @node-click="onTreeNodeClick" highlight-current class="project-tree">
            <template #default="{ node, data }">
              <span class="tree-node">
                <span v-if="data.type === 'folder'" class="node-icon">📁</span>
                <span v-else class="node-icon">📋</span>
                <span class="node-label">{{ node.label }}</span>
              </span>
            </template>
          </el-tree>
        </el-scrollbar>
      </div>
    </div>

    <!-- Right: Dynamic Content -->
    <div class="page-main">
      <el-empty v-if="!selectedNode" description="从左侧选择一个项目或文件夹" style="flex: 1;" />

      <!-- Folder selected -->
      <div v-else-if="selectedNode.type === 'folder'" class="folder-view"
        style="flex: 1; overflow-y: auto; padding: 16px;">
        <div class="view-header">
          <span class="node-icon">📁</span>
          <h3>{{ selectedNode.name }}</h3>
        </div>
        <div class="action-bar">
          <el-button type="danger" plain size="small" @click="deleteNode(selectedNode)">删除文件夹</el-button>
        </div>
      </div>

      <!-- Project selected -->
      <div v-else-if="selectedNode.type === 'project'" class="project-view">

        <!-- ====== 文件管理区（上） ====== -->
        <div class="section files-section">
          <div class="section-title">
            <span>📁 文件管理</span>
            <el-button size="small" type="warning" plain @click="handleReinitialize"
              :loading="reinitializing">重新初始化工作区</el-button>
          </div>

          <div class="file-layout">
            <!-- File tree -->
            <div class="file-tree-panel">
              <div class="file-tree-header">文件夹</div>
              <el-scrollbar style="height: calc(100% - 36px);">
                <el-tree ref="fileTreeRef" :data="fileTreeData" :props="{ label: 'name', children: 'children' }"
                  node-key="path" :expand-on-click-node="true" :default-expanded-keys="defaultExpandedFolders"
                  @node-click="onFileTreeNodeClick" highlight-current class="workspace-tree">
                  <template #default="{ data }">
                    <span class="ftree-node">
                      <span class="ftree-icon">{{ data.isFolder ? '📁' : '📄' }}</span>
                      <span class="ftree-name">{{ data.displayName || data.name }}</span>
                    </span>
                  </template>
                </el-tree>
              </el-scrollbar>
            </div>

            <!-- File content -->
            <div class="file-content-panel">
              <!-- Breadcrumb -->
              <el-breadcrumb separator="/" style="margin-bottom: 12px;">
                <el-breadcrumb-item v-for="(seg, idx) in pathSegments" :key="idx"
                  :class="{ 'breadcrumb-clickable': idx < pathSegments.length - 1 }" @click="navigateToPath(idx)">{{ seg
                    ||
                    '根目录' }}</el-breadcrumb-item>
              </el-breadcrumb>

              <!-- File toolbar -->
              <div class="file-toolbar">
                <el-upload ref="uploadRef" action="#" :auto-upload="false" :show-file-list="false"
                  @change="handleFileSelect" style="display: inline-block;">
                  <el-button size="small" type="primary">上传文件</el-button>
                </el-upload>
                <el-button size="small" @click="showCreateFolderDialog = true">新建文件夹</el-button>
                <span v-if="selectedFile" class="selected-file-name">{{ selectedFile.name || selectedFile }}</span>
                <el-button v-if="selectedFile" size="small" type="primary" :loading="uploading"
                  @click="doUpload">确认上传</el-button>
              </div>

              <!-- File list -->
              <el-table :data="fileItems" border stripe size="small"
                style="margin-top: 10px; height: calc(100% - 90px);" max-height="calc(100% - 90px)">
                <el-table-column label="名称">
                  <template #default="{ row }">
                    <span v-if="row.isFolder" class="file-folder-link" @click="enterFolder(row.name)">📁 {{
                      row.displayName || row.name }}</span>
                    <span v-else>📄 {{ row.originalName || row.name }}</span>
                  </template>
                </el-table-column>
                <el-table-column prop="size" label="大小" width="100" :formatter="formatSize" />
                <el-table-column prop="modified" label="修改时间" width="160" :formatter="formatDate" />
                <el-table-column label="操作" width="120" align="center">
                  <template #default="{ row }">
                    <el-button v-if="!row.isFolder" size="small" @click="downloadFile(row)">下载</el-button>
                    <el-button size="small" type="danger" plain @click="deleteFileItem(row)">删除</el-button>
                  </template>
                </el-table-column>
              </el-table>
            </div>
          </div>
        </div>

        <el-divider />

        <!-- ====== 选型管理区（下） ====== -->
        <div class="section selections-section">
          <div class="section-title">
            <span>📋 选型管理</span>
            <el-button size="small" type="primary" plain @click="goToSelections">在选型中心查看 →</el-button>
          </div>

          <el-table v-if="projectSelections.length > 0" :data="projectSelections" border stripe size="small"
            style="margin-top: 10px;">
            <el-table-column prop="name" label="选型单名称" />
            <el-table-column label="状态" width="90">
              <template #default="{ row }">
                <el-tag size="small" :type="statusType(row.status)">{{ statusText(row.status) }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column label="配件项" width="70" align="center">
              <template #default="{ row }">{{ row.items?.length || 0 }}</template>
            </el-table-column>
            <el-table-column label="操作" width="100" align="center">
              <template #default="{ row }">
                <el-button size="small" type="primary" plain @click="openSelection(row)">查看</el-button>
              </template>
            </el-table-column>
          </el-table>
          <el-empty v-else description="暂无选型单" :image-size="60" />
        </div>
      </div>
    </div>
  </div>

  <!-- Edit Dialog -->
  <el-dialog v-model="showEditDialog" :title="'编辑' + (editForm.type === 'folder' ? '文件夹' : '项目')" width="400px">
    <el-form :model="editForm" label-width="80px" size="small">
      <el-form-item label="名称">
        <el-input v-model="editForm.name" />
      </el-form-item>
    </el-form>
    <template #footer>
      <el-button @click="showEditDialog = false">取消</el-button>
      <el-button type="primary" @click="saveEdit">保存</el-button>
    </template>
  </el-dialog>

  <!-- Create Folder Dialog -->
  <el-dialog v-model="showCreateFolderDialog" title="新建文件夹" width="360px">
    <el-form :model="newFolderForm" label-width="80px" size="small">
      <el-form-item label="文件夹名">
        <el-input v-model="newFolderForm.name" placeholder="文件夹名称" />
      </el-form-item>
    </el-form>
    <template #footer>
      <el-button @click="showCreateFolderDialog = false">取消</el-button>
      <el-button type="primary" @click="doCreateFolder">创建</el-button>
    </template>
  </el-dialog>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { getProjects, createProject, updateProject, deleteProject as deleteProjectApi } from '@/api/projects'
import { getSelections } from '@/api/selections'
import {
  uploadFile as uploadFileApi,
  listFiles,
  createFolder as createFolderApi,
  deleteFile as deleteFileApi,
  deleteFolder as deleteFolderApi,
  reinitializeProjectWorkspace
} from '@/api/files'

const router = useRouter()
const projects = ref([])
const treeRef = ref(null)
const fileTreeRef = ref(null)
const treeSearch = ref('')
const currentNodeKey = ref(null)
const selectedNode = ref(null)
const showNewNodePopover = ref(false)
const showEditDialog = ref(false)
const showCreateFolderDialog = ref(false)
const newNodeForm = ref({ name: '', type: 'project', parentId: null })
const editForm = ref({ id: '', name: '', type: 'project' })
const newFolderForm = ref({ name: '' })
const projectSelections = ref([])

// File management state
const fileItems = ref([])
const fileTreeData = ref([])
const currentPath = ref('')
const selectedFile = ref(null)
const uploading = ref(false)
const reinitializing = ref(false)
const uploadRef = ref(null)
const defaultExpandedFolders = ref([])

// Build flat project list for parent selection
const flatProjects = computed(() => {
  const result = []
  const flatten = (nodes) => {
    for (const n of nodes) {
      if (n.type === 'project') result.push(n)
      if (n.children) flatten(n.children)
    }
  }
  flatten(projects.value)
  return result
})

// Build project tree data
const treeData = computed(() => buildTree(projects.value))

const buildTree = (nodes, parentId = null) => {
  return nodes
    .filter(n => n.parentId === parentId)
    .map(n => ({
      id: n.id,
      name: n.name,
      type: n.type,
      parentId: n.parentId,
      createdAt: n.createdAt,
      children: buildTree(nodes, n.id)
    }))
}

watch(treeSearch, (val) => { treeRef.value?.filter(val) })

const filterTreeNode = (value, data) => {
  if (!value) return true
  return data.name?.toLowerCase().includes(value.toLowerCase())
}

const onTreeNodeClick = async (data) => {
  selectedNode.value = data
  currentNodeKey.value = data.id
  if (data.type === 'project') {
    await Promise.all([loadProjectSelections(data.id), loadFileTree()])
  } else {
    projectSelections.value = []
    fileTreeData.value = []
    fileItems.value = []
    currentPath.value = ''
  }
}

const loadProjectSelections = async (projectId) => {
  try {
    const res = await getSelections(projectId)
    projectSelections.value = res.data || []
  } catch (e) {
    projectSelections.value = []
  }
}

// ====== File Tree ======
const loadFileTree = async () => {
  if (!selectedNode.value || selectedNode.value.type !== 'project') return
  try {
    const res = await listFiles('projects', null, selectedNode.value.id)
    const folders = res.data?.filter(f => f.isFolder) || []
    fileTreeData.value = await buildFileTree(folders)
    defaultExpandedFolders.value = folders.map(f => f.path || '')
  } catch (e) {
    fileTreeData.value = []
  }
}

const buildFileTree = async (folders) => {
  return folders.map(folder => ({
    name: folder.name,
    path: folder.path || '',
    displayName: folder.displayName || folder.name,
    isFolder: true,
    children: []
  }))
}

const onFileTreeNodeClick = async (data) => {
  currentPath.value = data.path || ''
  await loadFiles()
}

const loadFiles = async () => {
  if (!selectedNode.value) return
  try {
    const res = await listFiles('projects', currentPath.value || null, selectedNode.value.id)
    fileItems.value = res.data || []
  } catch (e) {
    fileItems.value = []
  }
}

// ====== File Operations ======
const enterFolder = async (folderName) => {
  currentPath.value = currentPath.value ? `${currentPath.value}/${folderName}` : folderName
  await loadFiles()
}

const navigateToPath = async (segmentIdx) => {
  const segs = currentPath.value.split('/').filter(s => s)
  currentPath.value = segs.slice(0, segmentIdx).join('/')
  await loadFiles()
}

const pathSegments = computed(() => currentPath.value.split('/').filter(s => s))

const handleFileSelect = (file) => {
  selectedFile.value = file.raw
}

const doUpload = async () => {
  if (!selectedFile.value) {
    ElMessage.warning('请先选择文件')
    return
  }
  uploading.value = true
  try {
    const formData = new FormData()
    formData.append('file', selectedFile.value)
    formData.append('bucket', 'projects')
    formData.append('relatedId', selectedNode.value.id)
    formData.append('fileType', 'PROJECT')
    if (currentPath.value) formData.append('path', currentPath.value)
    await uploadFileApi(formData)
    ElMessage.success('上传成功')
    selectedFile.value = null
    uploadRef.value?.clearFiles()
    await loadFiles()
    await loadFileTree()
  } catch (e) {
    ElMessage.error('上传失败')
  } finally {
    uploading.value = false
  }
}

const doCreateFolder = async () => {
  if (!newFolderForm.value.name) {
    ElMessage.warning('请输入文件夹名')
    return
  }
  try {
    const folderPath = currentPath.value
      ? `${currentPath.value}/${newFolderForm.value.name}`
      : newFolderForm.value.name
    await createFolderApi('projects', selectedNode.value.id, folderPath)
    ElMessage.success('文件夹已创建')
    showCreateFolderDialog.value = false
    newFolderForm.value.name = ''
    await loadFiles()
    await loadFileTree()
  } catch (e) {
    ElMessage.error('创建失败')
  }
}

const deleteFileItem = async (item) => {
  try {
    if (item.isFolder) {
      await deleteFolderApi('projects', selectedNode.value.id, item.path || item.name)
      ElMessage.success('文件夹已删除')
    } else {
      await deleteFileApi(item.id)
      ElMessage.success('文件已删除')
    }
    await loadFiles()
    await loadFileTree()
  } catch (e) {
    ElMessage.error('删除失败')
  }
}

const downloadFile = (file) => {
  window.open(`http://localhost:5128/api/files/${file.id}/download`)
}

const handleReinitialize = async () => {
  try {
    await ElMessageBox.confirm(
      '此操作将删除所有文件并重新初始化工作区结构，是否继续？',
      '警告',
      { type: 'warning' }
    )
    reinitializing.value = true
    await reinitializeProjectWorkspace(selectedNode.value.id)
    ElMessage.success('工作区已重新初始化')
    await loadFiles()
    await loadFileTree()
  } catch (e) {
    if (e !== 'cancel') ElMessage.error('操作失败')
  } finally {
    reinitializing.value = false
  }
}

// ====== Project Node Operations ======
onMounted(async () => { await loadProjects() })

const loadProjects = async () => {
  try {
    const res = await getProjects()
    projects.value = res.data || []
  } catch (error) {
    ElMessage.error('加载项目失败')
  }
}

const saveNewNode = async () => {
  if (!newNodeForm.value.name) { ElMessage.warning('请输入名称'); return }
  try {
    await createProject({
      name: newNodeForm.value.name,
      type: newNodeForm.value.type,
      parentId: newNodeForm.value.parentId || null
    })
    showNewNodePopover.value = false
    newNodeForm.value = { name: '', type: 'project', parentId: null }
    await loadProjects()
    ElMessage.success('创建成功')
  } catch (e) { ElMessage.error('创建失败') }
}

const editNode = (node) => {
  editForm.value = { id: node.id, name: node.name, type: node.type }
  showEditDialog.value = true
}

const saveEdit = async () => {
  if (!editForm.value.name) { ElMessage.warning('请输入名称'); return }
  try {
    await updateProject(editForm.value.id, { name: editForm.value.name })
    showEditDialog.value = false
    await loadProjects()
    ElMessage.success('保存成功')
  } catch (e) { ElMessage.error('保存失败') }
}

const deleteNode = async (node) => {
  try {
    await ElMessageBox.confirm(
      `确定删除"${node.name}"？${node.type === 'folder' ? '子级也会被删除。' : ''}`,
      '警告',
      { type: 'warning' }
    )
    await deleteProjectApi(node.id)
    if (selectedNode.value?.id === node.id) {
      selectedNode.value = null
      currentNodeKey.value = null
    }
    await loadProjects()
    ElMessage.success('已删除')
  } catch (e) {
    if (e !== 'cancel') ElMessage.error('删除失败')
  }
}

const goToSelections = () => {
  router.push({ path: '/selections', query: { projectId: selectedNode.value.id } })
}

const openSelection = (selection) => {
  router.push({ path: '/selections', query: { selectionId: selection.id } })
}

// ====== Helpers ======
const statusType = (status) => {
  const map = { Draft: 'info', Submitted: 'warning', Completed: 'success', Cancelled: 'danger', 0: 'info', 1: 'warning', 2: 'success', 3: 'danger' }
  return map[status] || 'info'
}

const statusText = (status) => {
  const map = { Draft: '草稿', Submitted: '已提交', Completed: '已完成', Cancelled: '已取消', 0: '草稿', 1: '已提交', 2: '已完成', 3: '已取消' }
  return map[status] || status
}

const formatSize = (row) => {
  const s = row.size
  if (!s) return '-'
  if (s < 1024) return s + ' B'
  if (s < 1024 * 1024) return (s / 1024).toFixed(1) + ' KB'
  return (s / 1024 / 1024).toFixed(1) + ' MB'
}

const formatDate = (row) => {
  return row.modified ? new Date(row.modified).toLocaleString('zh-CN') : '-'
}
</script>

<style scoped>
.projects-container {
  height: calc(100vh - var(--header-height));
}

.left-panel {
  height: 100%;
  background: var(--color-bg-sidebar);
  padding: 12px;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}


.tree-scrollbar {
  flex: 1;
  overflow-y: auto;
}

.project-tree,
.workspace-tree {
  background: transparent;
}

.tree-node,
.ftree-node {
  display: flex;
  align-items: center;
  gap: 6px; 
  font-size: 14px;
}

.node-icon,
.ftree-icon {
  font-size: 14px;
}

.node-label,
.ftree-name {
  flex: 1;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.view-header {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 16px;
}

.view-header h3 {
  margin: 0;
}

.section {
  margin-bottom: 0;
}

.section-title {
  display: flex;
  align-items: center;
  justify-content: space-between;
  font-size: 15px;
  font-weight: 600;
  color: #333;
  margin-bottom: 12px;
}

.files-section {
  flex: 1;
  overflow: hidden;
}

.file-layout {
  display: flex;
  gap: 12px;
  height: 320px;
}

.file-tree-panel {
  width: 200px;
  min-width: 200px;
  border: 1px solid #eee;
  border-radius: 6px;
  padding: 8px;
  overflow: hidden;
  display: flex;
  flex-direction: column;
}

.file-tree-header {
  font-size: 12px;
  color: #999;
  margin-bottom: 6px;
  padding-bottom: 6px;
  border-bottom: 1px solid #f0f0f0;
}

.file-content-panel {
  flex: 1;
  overflow: hidden;
  display: flex;
  flex-direction: column;
}

.file-toolbar {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}

.selected-file-name {
  font-size: 13px;
  color: #666;
  max-width: 200px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.file-folder-link {
  cursor: pointer;
  color: #409eff;
}

.file-folder-link:hover {
  text-decoration: underline;
}

.breadcrumb-clickable {
  cursor: pointer;
}

.breadcrumb-clickable:hover {
  color: #409eff;
}

.action-bar {
  display: flex;
  gap: 10px;
}
</style>
