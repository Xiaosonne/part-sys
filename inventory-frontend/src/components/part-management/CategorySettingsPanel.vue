<template>
  <div class="category-settings-panel">
    <!-- No category selected -->
    <el-empty v-if="!category" description="请从左侧选择分类" />

    <!-- Category details -->
    <div v-else class="category-details">
      <el-card>
        <template #header>
          <div class="card-header">
            <span>分类信息</span>
            <el-button type="primary" link @click="showEditDialog = true">编辑</el-button>
          </div>
        </template>

        <el-descriptions :column="2" border>
          <el-descriptions-item label="名称">{{ category.name }}</el-descriptions-item>
          <el-descriptions-item label="路径">{{ category.path }}</el-descriptions-item>
          <el-descriptions-item label="排序">{{ category.sortOrder || 0 }}</el-descriptions-item>
          <el-descriptions-item label="关联模板">
            <template v-if="template">
              <el-tag type="success">{{ template.category }}</el-tag>
            </template>
            <span v-else class="no-template">无</span>
          </el-descriptions-item>
        </el-descriptions>

        <!-- Template preview -->
        <template v-if="template && template.paramDefs">
          <el-divider content-position="left">模板参数</el-divider>
          <el-table :data="template.paramDefs" stripe size="small">
            <el-table-column prop="label" label="参数名" width="120" />
            <el-table-column prop="key" label="Key" width="120" />
            <el-table-column prop="dataType" label="类型" width="100" />
            <el-table-column prop="unit" label="单位" width="80" />
            <el-table-column label="选项">
              <template #default="{row}">
                {{ row.options?.join(', ') || '-' }}
              </template>
            </el-table-column>
          </el-table>
        </template>
      </el-card>

      <!-- Child categories -->
      <el-card class="child-categories">
        <template #header>
          <div class="card-header">
            <span>子分类 ({{ children.length }})</span>
            <el-button type="primary" link @click="$emit('add-child', category.id)">
              <el-icon><Plus /></el-icon> 新增子分类
            </el-button>
          </div>
        </template>

        <el-table v-if="children.length > 0" :data="children" stripe size="small">
          <el-table-column prop="name" label="名称" />
          <el-table-column prop="sortOrder" label="排序" width="80" />
          <el-table-column label="模板" width="120">
            <template #default="{row}">
              <el-tag v-if="row.specTemplateId" size="small" type="success">有</el-tag>
              <span v-else>-</span>
            </template>
          </el-table-column>
          <el-table-column label="操作" width="120">
            <template #default="{row}">
              <el-button size="small" link @click="$emit('select', row)">选择</el-button>
              <el-button size="small" link type="danger" @click="handleDeleteChild(row)">删除</el-button>
            </template>
          </el-table-column>
        </el-table>
        <el-empty v-else description="暂无子分类" />
      </el-card>

      <!-- Parent category -->
      <el-card v-if="parent" class="parent-category">
        <template #header>
          <span>父分类</span>
        </template>
        <div class="parent-info">
          <el-tag>{{ parent.name }}</el-tag>
          <el-button type="primary" link @click="$emit('select', parent)">选择父分类</el-button>
        </div>
      </el-card>
    </div>

    <!-- Edit Dialog -->
    <el-dialog v-model="showEditDialog" title="编辑分类" width="450px">
      <el-form :model="editForm" label-width="80px">
        <el-form-item label="名称" required>
          <el-input v-model="editForm.name" placeholder="输入分类名称" />
        </el-form-item>
        <el-form-item label="关联模板">
          <el-select v-model="editForm.specTemplateId" clearable placeholder="选择模板（可选）">
            <el-option
              v-for="tpl in templates"
              :key="tpl.id"
              :label="tpl.category"
              :value="tpl.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="排序">
          <el-input-number v-model="editForm.sortOrder" :min="0" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showEditDialog = false">取消</el-button>
        <el-button type="primary" @click="saveCategory">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, computed, watch } from 'vue'
import { Plus } from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { updatePartCategory, deletePartCategory } from '@/api/partCategories'
import { getTemplate } from '@/api/templates'

const props = defineProps({
  category: {
    type: Object,
    default: null
  },
  allCategories: {
    type: Array,
    default: () => []
  },
  templates: {
    type: Array,
    default: () => []
  }
})

const emit = defineEmits(['select', 'add-child', 'refresh'])

// Edit dialog
const showEditDialog = ref(false)
const editForm = ref({
  name: '',
  specTemplateId: '',
  sortOrder: 0
})

// Template for this category
const template = ref(null)

// Parent category
const parent = computed(() => {
  if (!props.category?.parentId) return null
  return props.allCategories.find(c => c.id === props.category.parentId)
})

// Child categories
const children = computed(() => {
  if (!props.category) return []
  return props.allCategories.filter(c => c.parentId === props.category.id)
})

// Watch category changes and load template
watch(() => props.category, async (cat) => {
  if (cat?.specTemplateId) {
    try {
      const res = await getTemplate(cat.specTemplateId)
      template.value = res.data
    } catch (error) {
      template.value = null
    }
  } else {
    template.value = null
  }

  // Initialize edit form
  if (cat) {
    editForm.value = {
      name: cat.name,
      specTemplateId: cat.specTemplateId || '',
      sortOrder: cat.sortOrder || 0
    }
  }
}, { immediate: true, deep: true })

// Save category
const saveCategory = async () => {
  if (!editForm.value.name) {
    ElMessage.warning('请输入分类名称')
    return
  }

  try {
    const data = {
      name: editForm.value.name,
      parentId: props.category.parentId,
      specTemplateId: editForm.value.specTemplateId || null,
      sortOrder: editForm.value.sortOrder || 0,
      path: computePath(editForm.value.name, props.category.parentId)
    }

    await updatePartCategory(props.category.id, data)
    ElMessage.success('分类更新成功')
    showEditDialog.value = false
    emit('refresh')
  } catch (error) {
    ElMessage.error(error.response?.data?.message || '保存失败')
  }
}

// Compute path
const computePath = (name, parentId) => {
  if (!parentId) return name
  const parentCat = props.allCategories.find(c => c.id === parentId)
  return parentCat ? `${parentCat.path}/${name}` : name
}

// Delete child category
const handleDeleteChild = async (child) => {
  try {
    await ElMessageBox.confirm(`确定删除分类 "${child.name}" 吗？`, '警告', { type: 'warning' })
    await deletePartCategory(child.id)
    ElMessage.success('删除成功')
    emit('refresh')
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error('删除失败')
    }
  }
}
</script>

<style scoped>
.category-settings-panel {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.category-details {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.no-template {
  color: #999;
}

.parent-info {
  display: flex;
  align-items: center;
  gap: 12px;
}

.child-categories {
  flex: 1;
}
</style>
