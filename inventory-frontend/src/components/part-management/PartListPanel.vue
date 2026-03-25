<template>
  <div class="part-list-panel">
    <!-- Header -->
    <div class="panel-header">
      <el-input
        v-model="searchKeyword"
        placeholder="搜索配件名称、型号..."
        clearable
        style="width: 300px;"
        @keyup.enter="doSearch"
      >
        <template #append>
          <el-button @click="doSearch">搜索</el-button>
        </template>
      </el-input>
      <el-button type="primary" @click="showAddDialog">
        <el-icon><Plus /></el-icon> 新增配件
      </el-button>
    </div>

    <!-- Table -->
    <el-table :data="filteredParts" stripe style="width: 100%;" v-loading="loading">
      <el-table-column prop="name" label="名称" width="150" />
      <el-table-column prop="model" label="型号" width="120" />
      <el-table-column prop="brand" label="品牌" width="100" />
      <el-table-column label="标签" width="150">
        <template #default="{row}">
          <el-tag v-for="tag in (row.tags || [])" :key="tag" size="small" style="margin-right: 4px;">{{tag}}</el-tag>
        </template>
      </el-table-column>
      <el-table-column prop="availableQty" label="可用" width="80" />
      <el-table-column prop="totalQty" label="总计" width="80" />
      <el-table-column label="操作" width="200" fixed="right">
        <template #default="{ row }">
          <el-button size="small" @click="showEditDialog(row)">编辑</el-button>
          <el-button size="small" type="danger" @click="handleDelete(row)">删除</el-button>
        </template>
      </el-table-column>
    </el-table>

    <!-- Empty state -->
    <el-empty v-if="!loading && filteredParts.length === 0" description="暂无配件" />

    <!-- Add/Edit Dialog -->
    <el-dialog v-model="showDialog" :title="editingPart ? '编辑配件' : '新增配件'" width="800px">
      <el-tabs>
        <el-tab-pane label="基本信息">
          <el-form :model="form" label-width="100px">
            <el-form-item label="名称" required>
              <el-input v-model="form.name" placeholder="配件名称" />
            </el-form-item>
            <el-form-item label="型号">
              <el-input v-model="form.model" placeholder="型号" />
            </el-form-item>
            <el-form-item label="描述">
              <el-input v-model="form.description" type="textarea" rows="2" />
            </el-form-item>
            <el-form-item label="厂商">
              <el-input v-model="form.manufacturer" placeholder="厂商" />
            </el-form-item>
            <el-form-item label="品牌">
              <el-input v-model="form.brand" placeholder="品牌" />
            </el-form-item>
            <el-form-item label="分类" v-if="fixedCategory">
              <el-input :model-value="fixedCategory" disabled />
            </el-form-item>
            <el-form-item label="模板">
              <el-select v-model="selectedTemplateId" @change="onTemplateChange" clearable placeholder="选择模板">
                <el-option v-for="t in templates" :key="t.id" :label="t.category" :value="t.id" />
              </el-select>
            </el-form-item>

            <!-- Spec fields based on template -->
            <template v-if="selectedTemplate">
              <el-divider content-position="left">规格参数</el-divider>
              <el-form-item v-for="param in selectedTemplate.paramDefs" :key="param.key" :label="param.label">
                <!-- string -->
                <el-input v-if="param.dataType === 'string'" v-model="specValues[param.key]" :placeholder="param.unit" />
                <!-- number -->
                <el-input-number v-else-if="param.dataType === 'number'" v-model="specValues[param.key]" :placeholder="param.unit" />
                <!-- boolean -->
                <el-switch v-else-if="param.dataType === 'boolean'" v-model="specValues[param.key]" />
                <!-- select -->
                <el-select v-else-if="param.dataType === 'select'" v-model="specValues[param.key]" clearable :placeholder="param.label">
                  <el-option v-for="opt in param.options" :key="opt" :label="opt" :value="opt" />
                </el-select>
              </el-form-item>
            </template>

            <el-divider content-position="left">标签</el-divider>
            <el-form-item label="标签">
              <el-select v-model="tagsInput" multiple filterable allow-create default-first-option placeholder="添加标签">
                <el-option v-for="tag in allTags" :key="tag" :label="tag" :value="tag" />
              </el-select>
            </el-form-item>

            <el-divider content-position="left">库存</el-divider>
            <el-form-item label="总数量">
              <el-input-number v-model="form.totalQty" :min="0" />
            </el-form-item>
          </el-form>
        </el-tab-pane>

        <el-tab-pane label="文件管理">
          <div class="file-management">
            <div class="file-upload-header">
              <el-button type="primary" :loading="uploading" @click="triggerFileUpload">
                <el-icon><Upload /></el-icon> 上传文件
              </el-button>
              <input
                ref="fileInputRef"
                type="file"
                style="display: none"
                @change="handleFileSelected"
              />
            </div>

            <el-table :data="partFiles" stripe style="width: 100%; margin-top: 12px;" v-loading="filesLoading">
              <el-table-column prop="fileName" label="文件名" min-width="200" />
              <el-table-column label="大小" width="100">
                <template #default="{row}">
                  {{ formatFileSize(row.fileSize) }}
                </template>
              </el-table-column>
              <el-table-column prop="uploadedBy" label="上传人" width="100" />
              <el-table-column label="上传时间" width="160">
                <template #default="{row}">
                  {{ row.uploadedAt ? new Date(row.uploadedAt).toLocaleString() : '-' }}
                </template>
              </el-table-column>
              <el-table-column label="操作" width="120" fixed="right">
                <template #default="{ row }">
                  <el-button size="small" type="primary" link @click="handleFileDownload(row)">下载</el-button>
                  <el-button size="small" type="danger" link @click="handleFileDelete(row)">删除</el-button>
                </template>
              </el-table-column>
            </el-table>

            <el-empty v-if="!filesLoading && partFiles.length === 0" description="暂无文件" />
          </div>
        </el-tab-pane>
      </el-tabs>

      <template #footer>
        <el-button @click="showDialog = false">取消</el-button>
        <el-button type="primary" @click="savePart">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, computed, watch } from 'vue'
import { Plus, Upload } from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { getParts, searchParts, createPart, updatePart, deletePart as deletePartApi } from '@/api/parts'
import { getTemplates, getTemplate } from '@/api/templates'
import { getPartFiles, deleteFile as deleteFileApi, uploadFile } from '@/api/files'

const props = defineProps({
  categoryPath: {
    type: String,
    default: null
  },
  categoryId: {
    type: String,
    default: null
  }
})

// Data
const parts = ref([])
const templates = ref([])
const loading = ref(false)
const searchKeyword = ref('')

// Dialog state
const showDialog = ref(false)
const editingPart = ref(null)
const selectedTemplateId = ref(null)
const selectedTemplate = ref(null)
const specValues = ref({})
const tagsInput = ref([])

// File management
const partFiles = ref([])
const filesLoading = ref(false)
const uploading = ref(false)
const fileInputRef = ref(null)
let pendingUploadFile = null

const form = ref({
  name: '',
  model: '',
  description: '',
  manufacturer: '',
  brand: '',
  category: '',
  totalQty: 0
})

// All tags for autocomplete
const allTags = computed(() => {
  const tags = new Set(parts.value.flatMap(p => p.tags || []))
  return Array.from(tags)
})

// Filter parts by keyword
const filteredParts = computed(() => {
  if (!searchKeyword.value) return parts.value
  const kw = searchKeyword.value.toLowerCase()
  return parts.value.filter(p =>
    p.name?.toLowerCase().includes(kw) ||
    p.model?.toLowerCase().includes(kw) ||
    p.brand?.toLowerCase().includes(kw)
  )
})

// Watch category changes
watch(() => props.categoryPath, async () => {
  await loadParts()
}, { immediate: true })

// Watch template changes
watch(selectedTemplate, (tpl) => {
  if (tpl && tpl.paramDefs) {
    tpl.paramDefs.forEach(p => {
      if (p.dataType === 'boolean') {
        specValues.value[p.key] = false
      } else if (p.dataType === 'number') {
        specValues.value[p.key] = null
      } else {
        specValues.value[p.key] = ''
      }
    })
  }
})

// Load parts for this category
const loadParts = async () => {
  loading.value = true
  try {
    if (props.categoryPath) {
      const res = (await searchParts({ categoryPath: props.categoryPath })).data
      parts.value = res || []
    } else {
      const res = (await getParts()).data
      parts.value = res || []
    }
  } catch (error) {
    ElMessage.error('加载配件失败')
  } finally {
    loading.value = false
  }
}

// Load templates
const loadTemplates = async () => {
  try {
    const res = (await getTemplates()).data
    templates.value = res || []
  } catch (error) {
    console.error('加载模板失败', error)
  }
}

// Template change handler
const onTemplateChange = async (templateId) => {
  if (!templateId) {
    selectedTemplate.value = null
    return
  }
  try {
    const res = await getTemplate(templateId)
    selectedTemplate.value = res.data
  } catch (error) {
    ElMessage.error('加载模板失败')
  }
}

// Search
const doSearch = async () => {
  if (!searchKeyword.value) {
    await loadParts()
    return
  }
  loading.value = true
  try {
    const res = (await searchParts({
      keyword: searchKeyword.value,
      categoryPath: props.categoryPath
    })).data
    parts.value = res || []
  } catch (error) {
    ElMessage.error('搜索失败')
  } finally {
    loading.value = false
  }
}

// Show add dialog
const showAddDialog = () => {
  editingPart.value = null
  selectedTemplateId.value = null
  selectedTemplate.value = null
  specValues.value = {}
  tagsInput.value = []
  form.value = {
    name: '',
    model: '',
    description: '',
    manufacturer: '',
    brand: '',
    category: props.categoryPath || '',
    totalQty: 0
  }
  showDialog.value = true
}

// Show edit dialog
const showEditDialog = async (part) => {
  editingPart.value = part
  form.value = { ...part }
  tagsInput.value = part.tags || []
  partFiles.value = []

  if (part.specTemplateId) {
    selectedTemplateId.value = part.specTemplateId
    await onTemplateChange(part.specTemplateId)
    // Populate spec values
    if (part.specs) {
      part.specs.forEach(s => {
        if (specValues.value.hasOwnProperty(s.key)) {
          specValues.value[s.key] = s.value
        }
      })
    }
  }

  // Load part files
  await loadPartFiles(part.id)

  showDialog.value = true
}

// Save part
const savePart = async () => {
  if (!form.value.name) {
    ElMessage.warning('请输入配件名称')
    return
  }

  try {
    const specs = []
    if (selectedTemplate.value) {
      selectedTemplate.value.paramDefs.forEach(p => {
        const val = specValues.value[p.key]
        if (val !== '' && val !== null && val !== undefined) {
          specs.push({
            key: p.key,
            label: p.label,
            value: String(val),
            unit: p.unit || ''
          })
        }
      })
    }

    const partData = {
      ...form.value,
      category: props.categoryPath || form.value.category,
      tags: tagsInput.value,
      specTemplateId: selectedTemplateId.value || null,
      specs
    }

    if (editingPart.value) {
      await updatePart(editingPart.value.id, partData)
      ElMessage.success('配件更新成功')
    } else {
      await createPart(partData)
      ElMessage.success('配件创建成功')
    }

    showDialog.value = false
    await loadParts()
  } catch (error) {
    ElMessage.error(error.response?.data?.message || '保存失败')
  }
}

// Delete part
const handleDelete = async (part) => {
  try {
    await ElMessageBox.confirm(`确定删除配件 "${part.name}" 吗？`, '警告', { type: 'warning' })
    await deletePartApi(part.id)
    ElMessage.success('删除成功')
    await loadParts()
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error('删除失败')
    }
  }
}

// Initialize
loadTemplates()

// File management functions
const loadPartFiles = async (partId) => {
  filesLoading.value = true
  try {
    const res = await getPartFiles(partId)
    partFiles.value = res.data || []
  } catch (error) {
    console.error('加载配件文件失败:', error)
    partFiles.value = []
  } finally {
    filesLoading.value = false
  }
}

const triggerFileUpload = () => {
  if (!editingPart.value) {
    ElMessage.warning('请先保存配件后再上传文件')
    return
  }
  fileInputRef.value?.click()
}

const handleFileSelected = async (event) => {
  const file = event.target.files?.[0]
  if (!file) return

  uploading.value = true
  try {
    const formData = new FormData()
    formData.append('file', file)
    formData.append('bucket', 'parts')
    formData.append('relatedId', editingPart.value.id)
    formData.append('fileType', 'PART')
    formData.append('description', '')

    await uploadFile(formData)
    ElMessage.success('文件上传成功')
    await loadPartFiles(editingPart.value.id)
  } catch (error) {
    ElMessage.error('文件上传失败: ' + (error.message || '未知错误'))
  } finally {
    uploading.value = false
    // Reset input
    if (fileInputRef.value) {
      fileInputRef.value.value = ''
    }
  }
}

const handleFileDelete = async (file) => {
  try {
    await ElMessageBox.confirm(`确定删除文件 "${file.fileName}" 吗？`, '警告', { type: 'warning' })
    await deleteFileApi(file.id)
    ElMessage.success('文件删除成功')
    await loadPartFiles(editingPart.value.id)
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error('删除失败')
    }
  }
}

const handleFileDownload = (file) => {
  window.open(`/api/files/${file.id}/download`, '_blank')
}

const formatFileSize = (bytes) => {
  if (!bytes) return '-'
  if (bytes < 1024) return bytes + ' B'
  if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB'
  return (bytes / (1024 * 1024)).toFixed(1) + ' MB'
}

defineExpose({
  loadParts
})
</script>

<style scoped>
.part-list-panel {
  height: 100%;
  display: flex;
  flex-direction: column;
}

.panel-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 16px;
}

.el-table {
  flex: 1;
}

.file-management {
  padding: 8px 0;
}

.file-upload-header {
  display: flex;
  align-items: center;
  gap: 12px;
}
</style>
