<template>
  <div class="category-tree">
    <el-tree
      ref="treeRef"
      :data="categoryTree"
      :props="{ label: 'name', children: 'children' }"
      node-key="id"
      :current-node-key="selectedCategoryId"
      highlight-current
      default-expand-all
      :expand-on-click-node="false"
      @node-click="handleNodeClick"
    >
      <template #default="{ node, data }">
        <span class="tree-node">
          <span class="node-label">{{ node.label }}</span>
          <span class="node-badges">
            <el-tag v-if="data.specTemplateId" size="small" type="success">🏷️</el-tag>
          </span>
        </span>
      </template>
    </el-tree>

    <div class="tree-footer">
      <el-button size="small" type="primary" link @click="showAddRootDialog">
        <el-icon><Plus /></el-icon> 新增分类
      </el-button>
      <el-button size="small" link @click="$emit('open-template-manager')">
        <el-icon><Setting /></el-icon> 模板管理
      </el-button>
    </div>

    <!-- Context Menu -->
    <el-dialog v-model="showCategoryDialog" :title="editingCategory ? '编辑分类' : '新增分类'" width="450px">
      <el-form :model="categoryForm" label-width="80px">
        <el-form-item label="分类名称" required>
          <el-input v-model="categoryForm.name" placeholder="输入分类名称" />
        </el-form-item>
        <el-form-item label="关联模板">
          <el-select v-model="categoryForm.specTemplateId" clearable placeholder="选择模板（可选）">
            <el-option
              v-for="tpl in templates"
              :key="tpl.id"
              :label="tpl.category"
              :value="tpl.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="排序">
          <el-input-number v-model="categoryForm.sortOrder" :min="0" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showCategoryDialog = false">取消</el-button>
        <el-button type="primary" @click="saveCategory">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { Setting, Plus } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import {
  getPartCategories,
  getPartCategoryById,
  createPartCategory,
  updatePartCategory,
  deletePartCategory
} from '@/api/partCategories'
import { getTemplates } from '@/api/templates'

const props = defineProps({
  selectedCategoryId: {
    type: String,
    default: null
  }
})

const emit = defineEmits(['select', 'open-template-manager'])

// Data
const categories = ref([])
const templates = ref([])
const treeRef = ref(null)

// Dialog state
const showCategoryDialog = ref(false)
const editingCategory = ref(null)
const categoryForm = ref({
  name: '',
  specTemplateId: '',
  sortOrder: 0
})

// Parent ID for new sub-category
const parentIdForNew = ref(null)

// Build tree structure
const categoryTree = computed(() => {
  const map = {}
  const roots = []

  categories.value.forEach(cat => {
    map[cat.id] = { ...cat, children: [] }
  })

  categories.value.forEach(cat => {
    if (cat.parentId && map[cat.parentId]) {
      map[cat.parentId].children.push(map[cat.id])
    } else {
      roots.push(map[cat.id])
    }
  })

  return roots
})

// Load data
const loadCategories = async () => {
  try {
    const res = (await getPartCategories()).data
    categories.value = res || []
  } catch (error) {
    ElMessage.error('加载分类失败')
  }
}

const loadTemplates = async () => {
  try {
    const res = (await getTemplates()).data
    templates.value = res || []
  } catch (error) {
    console.error('加载模板失败', error)
  }
}

// Node click handler
const handleNodeClick = (data) => {
  emit('select', data)
}

// Show add dialog for new root category (no parent)
const showAddRootDialog = () => {
  editingCategory.value = null
  parentIdForNew.value = null  // null = top level
  categoryForm.value = { name: '', specTemplateId: '', sortOrder: 0 }
  showCategoryDialog.value = true
}

// Show add dialog for new sub-category under selected parent
const showAddDialog = (parentId = null) => {
  editingCategory.value = null
  parentIdForNew.value = parentId
  categoryForm.value = { name: '', specTemplateId: '', sortOrder: 0 }
  showCategoryDialog.value = true
}

// Show edit dialog
const showEditDialog = async (category) => {
  try {
    const res = (await getPartCategoryById(category.id)).data
    editingCategory.value = category
    categoryForm.value = {
      name: res.name,
      specTemplateId: res.specTemplateId || '',
      sortOrder: res.sortOrder || 0
    }
    showCategoryDialog.value = true
  } catch (error) {
    ElMessage.error('加载分类详情失败')
  }
}

// Delete category
const handleDelete = async (category) => {
  try {
    await ElMessageBox.confirm(`确定删除分类 "${category.name}" 吗？`, '警告', { type: 'warning' })
    await deletePartCategory(category.id)
    ElMessage.success('删除成功')
    await loadCategories()
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error('删除失败')
    }
  }
}

// Compute path for new category
const computePath = (name, parentId) => {
  if (!parentId) return name
  const parent = categories.value.find(c => c.id === parentId)
  return parent ? `${parent.path}/${name}` : name
}

// Save category
const saveCategory = async () => {
  if (!categoryForm.value.name) {
    ElMessage.warning('请输入分类名称')
    return
  }

  try {
    const data = {
      name: categoryForm.value.name,
      parentId: editingCategory.value ? editingCategory.value.parentId : parentIdForNew.value,
      specTemplateId: categoryForm.value.specTemplateId || null,
      sortOrder: categoryForm.value.sortOrder || 0,
      path: computePath(categoryForm.value.name, editingCategory.value ? editingCategory.value.parentId : parentIdForNew.value)
    }

    if (editingCategory.value) {
      await updatePartCategory(editingCategory.value.id, data)
      ElMessage.success('分类更新成功')
    } else {
      await createPartCategory(data)
      ElMessage.success('分类创建成功')
    }

    showCategoryDialog.value = false
    await loadCategories()
  } catch (error) {
    ElMessage.error(error.response?.data?.message || '保存失败')
  }
}

// Expose methods for parent to call
defineExpose({
  showAddRootDialog,
  showAddDialog,
  showEditDialog,
  handleDelete,
  loadCategories
})

onMounted(async () => {
  await loadCategories()
  await loadTemplates()
})
</script>

<style scoped>
.category-tree {
  height: 100%;
  display: flex;
  flex-direction: column;
}

.category-tree :deep(.el-tree) {
  flex: 1;
  overflow: auto;
}

.tree-node {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
  padding-right: 8px;
}

.node-label {
  flex: 1;
}

.node-badges {
  display: flex;
  align-items: center;
  gap: 4px;
}

.tree-footer {
  padding: 12px 8px;
  border-top: 1px solid #e4e7ed;
  display: flex;
  justify-content: space-between;
}
</style>
