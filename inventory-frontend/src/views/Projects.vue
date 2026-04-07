<template>
  <div class="projects-container">
    <!-- Header -->
    <div class="header">
      <h2>项目管理</h2>
    </div>

    <!-- Two-column layout -->
    <div class="main-layout">
      <!-- Left: Project/Folder Tree -->
      <div class="left-panel">
        <el-input
          v-model="treeSearch"
          placeholder="搜索项目/文件夹"
          prefix-icon="Search"
          clearable
          style="margin-bottom: 10px;"
        />

        <!-- Create new project/folder -->
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

        <!-- Tree -->
        <el-scrollbar class="tree-scrollbar">
          <el-tree
            ref="treeRef"
            :data="treeData"
            :props="{ label: 'name', children: 'children' }"
            :expand-on-click-node="true"
            :default-expand-all="true"
            node-key="id"
            :current-node-key="currentNodeKey"
            :filter-node-method="filterTreeNode"
            @node-click="onTreeNodeClick"
            highlight-current
            class="project-tree"
          >
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

      <!-- Right: Dynamic Content -->
      <div class="right-panel">
        <!-- Empty state -->
        <el-empty v-if="!selectedNode" description="从左侧选择一个项目或文件夹" />

        <!-- Folder selected -->
        <div v-else-if="selectedNode.type === 'folder'" class="folder-view">
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
          <div class="view-header">
            <span class="node-icon">📋</span>
            <div>
              <h3>{{ selectedNode.name }}</h3>
              <span class="subtitle">项目</span>
            </div>
            <div class="header-actions">
              <el-button size="small" @click="editNode(selectedNode)">编辑</el-button>
              <el-button size="small" type="danger" plain @click="deleteNode(selectedNode)">删除</el-button>
            </div>
          </div>

          <!-- Project meta -->
          <div class="meta-card">
            <el-descriptions :column="2" border size="small">
              <el-descriptions-item label="类型">{{ selectedNode.type }}</el-descriptions-item>
              <el-descriptions-item label="创建时间">{{ formatDate(selectedNode.createdAt) }}</el-descriptions-item>
            </el-descriptions>
          </div>

          <!-- Quick actions -->
          <div class="quick-links">
            <el-button type="primary" plain @click="goToFiles(selectedNode)">📁 文件管理</el-button>
            <el-button type="warning" plain @click="goToSelections(selectedNode)">📋 选型管理</el-button>
          </div>

          <!-- Selection plans summary -->
          <div class="selections-summary">
            <div class="section-header">
              <h4>选型单</h4>
              <el-button size="small" type="primary" plain @click="goToSelections(selectedNode)">查看全部 →</el-button>
            </div>
            <el-table
              v-if="projectSelections.length > 0"
              :data="projectSelections"
              border
              stripe
              size="small"
              style="margin-top: 10px;"
            >
              <el-table-column prop="name" label="选型单名称" />
              <el-table-column label="状态" width="90">
                <template #default="{ row }">
                  <el-tag size="small" :type="statusType(row.status)">{{ statusText(row.status) }}</el-tag>
                </template>
              </el-table-column>
              <el-table-column label="配件项" width="70" align="center">
                <template #default="{ row }">
                  {{ row.items?.length || 0 }}
                </template>
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
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { getProjects, createProject, updateProject, deleteProject as deleteProjectApi } from '@/api/projects'
import { getSelections } from '@/api/selections'

const router = useRouter()
const projects = ref([])
const treeRef = ref(null)
const treeSearch = ref('')
const currentNodeKey = ref(null)
const selectedNode = ref(null)
const showNewNodePopover = ref(false)
const showEditDialog = ref(false)
const newNodeForm = ref({ name: '', type: 'project', parentId: null })
const editForm = ref({ id: '', name: '', type: 'project' })
const projectSelections = ref([])

// Build flat list for parent selection
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

// Build tree data
const treeData = computed(() => {
  return buildTree(projects.value)
})

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

watch(treeSearch, (val) => {
  treeRef.value?.filter(val)
})

const filterTreeNode = (value, data) => {
  if (!value) return true
  return data.name?.toLowerCase().includes(value.toLowerCase())
}

const onTreeNodeClick = async (data) => {
  selectedNode.value = data
  currentNodeKey.value = data.id
  if (data.type === 'project') {
    await loadProjectSelections(data.id)
  } else {
    projectSelections.value = []
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

onMounted(async () => {
  await loadProjects()
})

const loadProjects = async () => {
  try {
    const res = await getProjects()
    projects.value = res.data || []
  } catch (error) {
    ElMessage.error('加载项目失败')
  }
}

const saveNewNode = async () => {
  if (!newNodeForm.value.name) {
    ElMessage.warning('请输入名称')
    return
  }
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
  } catch (e) {
    ElMessage.error('创建失败')
  }
}

const editNode = (node) => {
  editForm.value = { id: node.id, name: node.name, type: node.type }
  showEditDialog.value = true
}

const saveEdit = async () => {
  if (!editForm.value.name) {
    ElMessage.warning('请输入名称')
    return
  }
  try {
    await updateProject(editForm.value.id, { name: editForm.value.name })
    showEditDialog.value = false
    await loadProjects()
    ElMessage.success('保存成功')
  } catch (e) {
    ElMessage.error('保存失败')
  }
}

const deleteNode = async (node) => {
  try {
    await ElMessageBox.confirm(`确定删除"${node.name}"？${node.type === 'folder' ? '子级也会被删除。' : ''}`, '警告', { type: 'warning' })
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

const goToFiles = (node) => {
  router.push({ name: 'ProjectDetail', params: { id: node.id } })
}

const goToSelections = (node) => {
  router.push({ path: '/selections', query: { projectId: node.id } })
}

const openSelection = (selection) => {
  router.push({ path: '/selections', query: { selectionId: selection.id } })
}

const statusType = (status) => {
  const map = { Draft: 'info', Submitted: 'warning', Completed: 'success', Cancelled: 'danger', 0: 'info', 1: 'warning', 2: 'success', 3: 'danger' }
  return map[status] || 'info'
}

const statusText = (status) => {
  const map = { Draft: '草稿', Submitted: '已提交', Completed: '已完成', Cancelled: '已取消', 0: '草稿', 1: '已提交', 2: '已完成', 3: '已取消' }
  return map[status] || status
}

const formatDate = (d) => d ? new Date(d).toLocaleString('zh-CN') : '-'
</script>

<style scoped>
.projects-container {
  padding: 20px;
  height: calc(100vh - 80px);
  display: flex;
  flex-direction: column;
}
.header {
  margin-bottom: 16px;
}
.header h2 {
  margin: 0;
}
.main-layout {
  display: flex;
  gap: 16px;
  flex: 1;
  overflow: hidden;
}
.left-panel {
  width: 280px;
  min-width: 280px;
  background: #fff;
  border-radius: 8px;
  padding: 16px;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}
.right-panel {
  flex: 1;
  background: #fff;
  border-radius: 8px;
  padding: 20px;
  overflow-y: auto;
}
.tree-scrollbar {
  flex: 1;
  overflow-y: auto;
}
.project-tree {
  background: transparent;
}
.tree-node {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 14px;
}
.node-icon {
  font-size: 14px;
}
.node-label {
  flex: 1;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

/* Right panel views */
.view-header {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 20px;
}
.view-header h3 {
  margin: 0;
}
.subtitle {
  color: #999;
  font-size: 13px;
}
.header-actions {
  margin-left: auto;
  display: flex;
  gap: 8px;
}
.meta-card {
  margin-bottom: 16px;
}
.quick-links {
  display: flex;
  gap: 10px;
  margin-bottom: 20px;
}
.section-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 8px;
}
.section-header h4 {
  margin: 0;
}
.action-bar {
  display: flex;
  gap: 10px;
}
</style>
