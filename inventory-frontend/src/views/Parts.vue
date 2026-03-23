<template>
  <div>
    <el-button type="primary" @click="showDialog = true" style="margin-bottom: 20px;">Add Part</el-button>
    <el-table :data="parts">
      <el-table-column prop="name" label="Name" />
      <el-table-column prop="model" label="Model" />
      <el-table-column prop="brand" label="Brand" />
      <el-table-column prop="category" label="Category" />
      <el-table-column label="Tags" width="150">
        <template #default="{row}">
          <el-tag v-for="tag in (row.tags || [])" :key="tag" size="small" style="margin-right: 4px;">{{tag}}</el-tag>
        </template>
      </el-table-column>
      <el-table-column prop="totalQty" label="Total" />
      <el-table-column prop="availableQty" label="Available" />
      <el-table-column prop="lockedQty" label="Locked" />
      <el-table-column label="Actions" width="280">
        <template #default="{ row }">
          <el-button size="small" @click="editPart(row)">Edit</el-button>
          <el-button size="small" @click="showPartSpecs(row)">Specs</el-button>
          <el-button size="small" @click="showPartFiles(row)">Docs</el-button>
          <el-button size="small" type="danger" @click="deletePart(row.id)">Delete</el-button>
        </template>
      </el-table-column>
    </el-table>

    <el-dialog v-model="showDialog" :title="editingId ? 'Edit Part' : 'Add Part'" width="700px">
      <el-form :model="form" label-width="100px">
        <el-form-item label="Name">
          <el-input v-model="form.name" />
        </el-form-item>
        <el-form-item label="Model">
          <el-input v-model="form.model" />
        </el-form-item>
        <el-form-item label="Description">
          <el-input v-model="form.description" type="textarea" rows="2" />
        </el-form-item>
        <el-form-item label="Manufacturer">
          <el-input v-model="form.manufacturer" />
        </el-form-item>
        <el-form-item label="Brand">
          <el-input v-model="form.brand" />
        </el-form-item>
        <el-form-item label="Category">
          <el-select v-model="form.category" filterable allow-create clearable placeholder="Select or enter category">
            <el-option v-for="cat in categories" :key="cat" :label="cat" :value="cat" />
          </el-select>
        </el-form-item>
        <el-form-item label="Template">
          <el-select v-model="selectedTemplateId" @change="onTemplateChange" clearable placeholder="Select template">
            <el-option v-for="t in templates" :key="t.id" :label="t.category" :value="t.id" />
          </el-select>
        </el-form-item>

        <!-- Dynamic Spec Fields -->
        <template v-if="selectedTemplate">
          <el-divider />
          <div style="font-weight: bold; margin-bottom: 10px;">Specifications</div>
          <el-form-item v-for="param in selectedTemplate.paramDefs" :key="param.key" :label="param.label">
            <!-- string -->
            <el-input v-if="param.dataType === 'string'" v-model="specValues[param.key]" :placeholder="param.unit ? param.unit : ''" />
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

        <el-divider />
        <el-form-item label="Tags">
          <el-select v-model="tagsInput" multiple filterable allow-create default-first-option placeholder="Add tags">
            <el-option v-for="tag in allTags" :key="tag" :label="tag" :value="tag" />
          </el-select>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showDialog = false">Cancel</el-button>
        <el-button type="primary" @click="savePart">Save</el-button>
      </template>
    </el-dialog>

    <!-- Part Files Dialog -->
    <el-dialog v-model="showFilesDialog" :title="`Files - ${selectedPart?.name}`" width="800px">
      <div style="margin-bottom: 20px;">
        <el-breadcrumb separator="/">
          <el-breadcrumb-item @click="partCurrentPath = ''; loadPartFiles()">Root</el-breadcrumb-item>
          <el-breadcrumb-item v-for="(part, idx) in partCurrentPath.split('/').filter(p => p)" :key="idx" @click="partCurrentPath = partCurrentPath.split('/').slice(0, idx + 1).join('/'); loadPartFiles()">
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
        <el-button type="primary" @click="uploadPartFile" :loading="uploading">Upload</el-button>
        <el-button @click="showCreateFolderDialog = true">New Folder</el-button>
      </div>

      <el-table :data="partFileItems" style="height: 300px;" max-height="300px">
        <el-table-column label="Name">
          <template #default="{ row }">
            <div v-if="row.isFolder" style="cursor: pointer; color: #409eff;" @click="partCurrentPath = row.path; loadPartFiles()">
              📁 {{ row.name }}
            </div>
            <div v-else>📄 {{ row.originalName || row.name }}</div>
          </template>
        </el-table-column>
        <el-table-column prop="size" label="Size" width="100" :formatter="formatSize" />
        <el-table-column label="Actions" width="120">
          <template #default="{ row }">
            <el-button v-if="!row.isFolder" size="small" @click="downloadPartFile(row)">Download</el-button>
            <el-button size="small" type="danger" @click="deletePartFile(row)">Delete</el-button>
          </template>
        </el-table-column>
      </el-table>

      <el-dialog v-model="showCreateFolderDialog" title="Create Folder" width="400px">
        <el-input v-model="newFolderName" placeholder="Folder name" />
        <template #footer>
          <el-button @click="showCreateFolderDialog = false">Cancel</el-button>
          <el-button type="primary" @click="createPartFolder">Create</el-button>
        </template>
      </el-dialog>
    </el-dialog>

    <!-- Part Specs Dialog -->
    <el-dialog v-model="showSpecsDialog" :title="`Specifications - ${selectedPart?.name}`" width="500px">
      <el-descriptions :column="1" border v-if="selectedPart && selectedPart.specs && selectedPart.specs.length > 0">
        <el-descriptions-item v-for="spec in selectedPart.specs" :key="spec.key" :label="spec.label">
          {{ spec.value }}{{ spec.unit ? ' ' + spec.unit : '' }}
        </el-descriptions-item>
      </el-descriptions>
      <div v-else style="text-align: center; color: #999; padding: 20px;">
        No specifications defined
      </div>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, onMounted, computed } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { UploadFilled } from '@element-plus/icons-vue'
import { getParts, createPart, updatePart, deletePart as deletePartApi } from '@/api/parts'
import { getTemplates, getTemplate } from '@/api/templates'
import { uploadFile as uploadFileApi, deleteFile as deleteFileApi, listFiles, createFolder as createFolderApi } from '@/api/files'

const parts = ref([])
const templates = ref([])
const showDialog = ref(false)
const editingId = ref(null)
const form = ref({ name: '', model: '', description: '', manufacturer: '', brand: '', category: '' })
const showFilesDialog = ref(false)
const selectedPart = ref(null)
const partFileItems = ref([])
const partCurrentPath = ref('')
const selectedFile = ref(null)
const uploading = ref(false)
const showCreateFolderDialog = ref(false)
const newFolderName = ref('')
const showSpecsDialog = ref(false)

// Template and spec related
const selectedTemplateId = ref(null)
const selectedTemplate = ref(null)
const specValues = ref({})
const tagsInput = ref([])

// Computed
const categories = computed(() => {
  const cats = new Set(parts.value.map(p => p.category).filter(c => c))
  return Array.from(cats)
})

const allTags = computed(() => {
  const tags = new Set(parts.value.flatMap(p => p.tags || []))
  return Array.from(tags)
})

onMounted(async () => {
  await loadParts()
  await loadTemplates()
})

const loadParts = async () => {
  try {
    const res = (await getParts()).data
    parts.value = res || []
  } catch (error) {
    ElMessage.error('Failed to load parts')
  }
}

const loadTemplates = async () => {
  try {
    const res = (await getTemplates()).data
    templates.value = res || []
  } catch (error) {
    console.error('Failed to load templates', error)
  }
}

const onTemplateChange = async (templateId) => {
  if (!templateId) {
    selectedTemplate.value = null
    return
  }
  try {
    const res = await getTemplate(templateId)
    selectedTemplate.value = res.data
    // Initialize spec values
    specValues.value = {}
    res.data.paramDefs.forEach(p => {
      if (p.dataType === 'boolean') {
        specValues.value[p.key] = false
      } else if (p.dataType === 'number') {
        specValues.value[p.key] = null
      } else {
        specValues.value[p.key] = ''
      }
    })
  } catch (error) {
    ElMessage.error('Failed to load template')
  }
}

const editPart = (row) => {
  editingId.value = row.id
  form.value = { ...row }
  tagsInput.value = row.tags || []

  // Load template if part has one
  if (row.specTemplateId) {
    selectedTemplateId.value = row.specTemplateId
    onTemplateChange(row.specTemplateId).then(() => {
      // Populate existing spec values
      if (row.specs) {
        row.specs.forEach(s => {
          if (specValues.value.hasOwnProperty(s.key)) {
            specValues.value[s.key] = s.value
          }
        })
      }
    })
  } else {
    selectedTemplateId.value = null
    selectedTemplate.value = null
    specValues.value = {}
  }

  showDialog.value = true
}

const savePart = async () => {
  try {
    // Build specs array from specValues
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
      tags: tagsInput.value,
      specTemplateId: selectedTemplateId.value || null,
      specs
    }

    if (editingId.value) {
      await updatePart(editingId.value, partData)
      ElMessage.success('Part updated')
    } else {
      await createPart(partData)
      ElMessage.success('Part created')
    }
    showDialog.value = false
    editingId.value = null
    selectedTemplateId.value = null
    selectedTemplate.value = null
    specValues.value = {}
    tagsInput.value = []
    form.value = { name: '', model: '', description: '', manufacturer: '', brand: '', category: '' }
    await loadParts()
  } catch (error) {
    ElMessage.error(error.response?.data?.message || 'Failed to save part')
  }
}

const showPartSpecs = (part) => {
  selectedPart.value = part
  showSpecsDialog.value = true
}

const deletePart = async (id) => {
  try {
    await ElMessageBox.confirm('Delete this part?', 'Warning', { type: 'warning' })
    await deletePartApi(id)
    ElMessage.success('Part deleted')
    await loadParts()
  } catch (error) {
    if (error.message !== 'cancel') {
      ElMessage.error('Failed to delete part')
    }
  }
}

const showPartFiles = async (part) => {
  selectedPart.value = part
  partCurrentPath.value = ''
  await loadPartFiles()
  showFilesDialog.value = true
}

const loadPartFiles = async () => {
  try {
    const res = await listFiles('parts', partCurrentPath.value || null, selectedPart.value.id)
    partFileItems.value = res.data
  } catch (error) {
    ElMessage.error('Failed to load part files')
  }
}

const handleFileSelect = (file) => {
  selectedFile.value = file.raw
}

const uploadPartFile = async () => {
  if (!selectedFile.value) {
    ElMessage.warning('Please select a file')
    return
  }

  uploading.value = true
  try {
    const formData = new FormData()
    formData.append('file', selectedFile.value)
    formData.append('bucket', 'parts')
    formData.append('relatedId', selectedPart.value.id)
    formData.append('fileType', 'PART')
    if (partCurrentPath.value) {
      formData.append('path', partCurrentPath.value)
    }

    await uploadFileApi(formData)
    ElMessage.success('File uploaded')
    selectedFile.value = null
    await loadPartFiles()
  } catch (error) {
    ElMessage.error('Upload failed')
  } finally {
    uploading.value = false
  }
}

const downloadPartFile = (file) => {
  window.open(`http://localhost:5128/api/files/${file.id}/download`)
}

const deletePartFile = async (item) => {
  try {
    if (!item.isFolder) {
      await deleteFileApi(item.id)
      ElMessage.success('File deleted')
      await loadPartFiles()
    }
  } catch (error) {
    ElMessage.error('Delete failed')
  }
}

const createPartFolder = async () => {
  if (!newFolderName.value) {
    ElMessage.warning('Please enter folder name')
    return
  }

  try {
    const folderPath = partCurrentPath.value ? `${partCurrentPath.value}/${newFolderName.value}` : newFolderName.value
    await createFolderApi('parts', selectedPart.value.id, folderPath)
    ElMessage.success('Folder created')
    newFolderName.value = ''
    showCreateFolderDialog.value = false
    await loadPartFiles()
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
</script>
