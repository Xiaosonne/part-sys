<template>
  <div class="spec-filter-panel">
    <!-- Compact Search Bar -->
    <div class="search-bar" v-if="template">
      <!-- Keyword -->
      <el-input
        v-model="keyword"
        placeholder="搜索名称、型号..."
        clearable
        style="width: 180px;"
        @keyup.enter="doSearchWithCategoryPath"
      />

      <!-- Active Filter Tags -->
      <div class="filter-tags">
        <el-tag
          v-for="filter in activeFilters"
          :key="filter.key"
          closable
          @close="removeFilter(filter.key)"
          @click="editFilter(filter)"
          class="filter-tag"
        >
          {{ filter.label }}: {{ formatFilterValue(filter) }}
        </el-tag>
      </div>

      <!-- Add Filter Button -->
      <el-dropdown @command="handleAddFilter" trigger="click">
        <el-button type="primary" plain size="small">
          <el-icon><Plus /></el-icon> 添加过滤
        </el-button>
        <template #dropdown>
          <el-dropdown-menu>
            <el-dropdown-item
              v-for="param in availableParams"
              :key="param.key"
              :command="param"
            >
              {{ param.label }}
            </el-dropdown-item>
          </el-dropdown-menu>
        </template>
      </el-dropdown>

      <!-- Stock Range -->
      <el-input-number
        v-model="minQty"
        :min="0"
        placeholder="最小"
        size="small"
        style="width: 100px;"
        controls-position="right"
      />
      <span class="range-sep">-</span>
      <el-input-number
        v-model="maxQty"
        :min="0"
        placeholder="最大"
        size="small"
        style="width: 100px;"
        controls-position="right"
      />
      <span class="qty-label">库存</span>

      <!-- Search & Reset -->
      <el-button type="primary" @click="doSearchWithCategoryPath" :loading="searching" size="small">搜索</el-button>
      <el-button @click="resetFilters" size="small">重置</el-button>
    </div>

    <!-- No template message -->
    <el-empty v-if="!template" description="该分类未关联模板，无法进行规格过滤" />

    <!-- Results Card -->
    <el-card class="result-card" v-loading="searching" v-if="template">
      <template #header>
        <div class="card-header">
          <span>搜索结果 {{ results.length > 0 ? `(${results.length} 个)` : '' }}</span>
        </div>
      </template>

      <el-table :data="results" stripe style="width: 100%;" max-height="400">
        <el-table-column prop="name" label="名称" width="150" />
        <el-table-column prop="model" label="型号" width="120" />
        <el-table-column prop="brand" label="品牌" width="100" />
        <el-table-column prop="category" label="分类" width="150" />
        <el-table-column label="标签" width="150">
          <template #default="{row}">
            <el-tag v-for="tag in (row.tags || [])" :key="tag" size="small" style="margin-right: 4px;">{{tag}}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="规格" min-width="200">
          <template #default="{row}">
            <div v-if="row.specs && row.specs.length > 0" class="specs-display">
              <span v-for="spec in row.specs.slice(0, 3)" :key="spec.key" class="spec-item">
                {{ spec.label }}: {{ spec.value }}{{ spec.unit }}
              </span>
              <span v-if="row.specs.length > 3" class="more-specs">+{{ row.specs.length - 3 }}</span>
            </div>
            <span v-else class="no-specs">-</span>
          </template>
        </el-table-column>
        <el-table-column prop="availableQty" label="可用" width="80" />
        <el-table-column prop="totalQty" label="总计" width="80" />
        <el-table-column label="操作" width="100" fixed="right">
          <template #default="{row}">
            <el-button size="small" @click="viewPart(row)">详情</el-button>
          </template>
        </el-table-column>
      </el-table>

      <el-empty v-if="results.length === 0 && !searching" description="请设置过滤条件搜索" />
    </el-card>

    <!-- Filter Editor Modal -->
    <el-dialog v-model="showFilterModal" title="编辑过滤条件" width="500px">
      <el-form v-if="editingParam" :label-width="100">
        <el-form-item label="规格参数">
          <span>{{ editingParam.label }}</span>
        </el-form-item>

        <!-- string: fuzzy search -->
        <el-form-item v-if="editingParam.dataType === 'string'" label="值">
          <el-input
            v-model="filterValue.string"
            :placeholder="`输入${editingParam.label}`"
            clearable
          />
        </el-form-item>

        <!-- number: range query -->
        <el-form-item v-else-if="editingParam.dataType === 'number'" label="范围">
          <div class="number-range-edit">
            <el-input-number
              v-model="filterValue.min"
              :placeholder="editingParam.unit ? `最小${editingParam.unit}` : '最小值'"
              :min="0"
              controls-position="right"
              style="width: 140px;"
            />
            <span class="range-sep">-</span>
            <el-input-number
              v-model="filterValue.max"
              :placeholder="editingParam.unit ? `最大${editingParam.unit}` : '最大值'"
              :min="0"
              controls-position="right"
              style="width: 140px;"
            />
            <span v-if="editingParam.unit" class="unit-label">{{ editingParam.unit }}</span>
          </div>
        </el-form-item>

        <!-- select (options < 5): radio buttons -->
        <el-form-item v-else-if="editingParam.dataType === 'select' && editingParam.options?.length < 5" label="值">
          <el-radio-group v-model="filterValue.selected">
            <el-radio-button
              v-for="opt in editingParam.options"
              :key="opt"
              :value="opt"
            >
              {{ opt }}
            </el-radio-button>
          </el-radio-group>
        </el-form-item>

        <!-- select (options >= 5): dropdown -->
        <el-form-item v-else-if="editingParam.dataType === 'select'" label="值">
          <el-select v-model="filterValue.selected" :placeholder="`选择${editingParam.label}`" clearable style="width: 100%;">
            <el-option
              v-for="opt in editingParam.options"
              :key="opt"
              :label="opt"
              :value="opt"
            />
          </el-select>
        </el-form-item>

        <!-- boolean -->
        <el-form-item v-else-if="editingParam.dataType === 'boolean'" label="值">
          <el-switch v-model="filterValue.bool" />
        </el-form-item>
      </el-form>

      <template #footer>
        <el-button @click="showFilterModal = false">取消</el-button>
        <el-button type="primary" @click="confirmFilter">确定</el-button>
      </template>
    </el-dialog>

    <!-- Detail Dialog -->
    <el-dialog v-model="showDetailDialog" :title="selectedPart?.name" width="800px">
      <el-tabs>
        <el-tab-pane label="基本信息">
          <el-descriptions v-if="selectedPart" :column="2" border>
            <el-descriptions-item label="名称">{{ selectedPart.name }}</el-descriptions-item>
            <el-descriptions-item label="型号">{{ selectedPart.model }}</el-descriptions-item>
            <el-descriptions-item label="品牌">{{ selectedPart.brand }}</el-descriptions-item>
            <el-descriptions-item label="厂商">{{ selectedPart.manufacturer }}</el-descriptions-item>
            <el-descriptions-item label="分类">{{ selectedPart.category }}</el-descriptions-item>
            <el-descriptions-item label="标签">
              <el-tag v-for="tag in (selectedPart.tags || [])" :key="tag" size="small" style="margin-right: 4px;">{{tag}}</el-tag>
            </el-descriptions-item>
            <el-descriptions-item label="描述" :span="2">{{ selectedPart.description }}</el-descriptions-item>
            <el-descriptions-item label="可用数量">{{ selectedPart.availableQty }}</el-descriptions-item>
            <el-descriptions-item label="锁定数量">{{ selectedPart.lockedQty }}</el-descriptions-item>
            <el-descriptions-item label="总计">{{ selectedPart.totalQty }}</el-descriptions-item>
          </el-descriptions>

          <el-divider v-if="selectedPart?.specs?.length > 0">规格参数</el-divider>
          <el-descriptions v-if="selectedPart?.specs?.length > 0" :column="2" border>
            <el-descriptions-item
              v-for="spec in selectedPart.specs"
              :key="spec.key"
              :label="spec.label"
            >
              {{ spec.value }}{{ spec.unit ? ' ' + spec.unit : '' }}
            </el-descriptions-item>
          </el-descriptions>
        </el-tab-pane>

        <el-tab-pane label="文件管理">
          <div v-loading="filesLoading">
            <el-table v-if="partFiles.length > 0" :data="partFiles" stripe style="width: 100%;">
              <el-table-column prop="fileName" label="文件名" min-width="200" />
              <el-table-column label="大小" width="100">
                <template #default="{row}">
                  {{ formatFileSize(row.fileSize) }}
                </template>
              </el-table-column>
              <el-table-column prop="uploadedBy" label="上传人" width="100" />
              <el-table-column label="上传时间" width="180">
                <template #default="{row}">
                  {{ row.uploadedAt ? new Date(row.uploadedAt).toLocaleString() : '-' }}
                </template>
              </el-table-column>
            </el-table>
            <el-empty v-else description="暂无文件" />
          </div>
        </el-tab-pane>
      </el-tabs>

      <template #footer>
        <el-button @click="showDetailDialog = false">关闭</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, computed, watch } from 'vue'
import { ElMessage } from 'element-plus'
import { Plus } from '@element-plus/icons-vue'
import { searchParts } from '@/api/parts'
import { getPartFiles, deleteFile } from '@/api/files'
import { usePartSearch } from '@/composables/usePartSearch'

const props = defineProps({
  template: {
    type: Object,
    default: null
  },
  categoryPath: {
    type: String,
    default: null
  }
})

// Use shared composable (skip its internal template management — we use props.template)
const {
  keyword,
  minQty,
  maxQty,
  searching,
  results,
  activeFilters,
  availableParams: composableAvailableParams,
  showFilterModal,
  editingParam,
  editingFilterKey,
  filterValue,
  doSearch,
  resetFilters,
  formatFilterValue,
  handleAddFilter,
  removeFilter,
  confirmFilter,
  resetFilterValue
} = usePartSearch()

// SpecFilterPanel receives template from parent props (not computed from category)
const availableParams = computed(() => {
  if (!props.template?.paramDefs) return []
  const activeKeys = new Set(activeFilters.value.map(f => f.key))
  return props.template.paramDefs.filter(p => !activeKeys.has(p.key))
})

// Override doSearch to use props.categoryPath
const doSearchWithCategoryPath = async () => {
  searching.value = true
  try {
    const res = await searchParts({
      keyword: keyword.value || null,
      categoryPath: props.categoryPath,
      specFilters: activeFilters.value.length > 0 ? buildSpecFiltersList() : null,
      minAvailableQty: minQty.value || null,
      maxAvailableQty: maxQty.value || null
    })
    results.value = res.data || []
  } catch (error) {
    ElMessage.error('搜索失败: ' + (error.message || '未知错误'))
    results.value = []
  } finally {
    searching.value = false
  }
}

// Build spec filters list for API (same logic as composable)
const buildSpecFiltersList = () => {
  const list = []
  activeFilters.value.forEach(filter => {
    switch (filter.type) {
      case 'string':
        if (filter.value && filter.value.trim()) {
          list.push({ Key: filter.key, Op: 'contains', Value: filter.value.trim() })
        }
        break
      case 'number-range':
        if (filter.value.min !== null) {
          list.push({ Key: filter.key, Op: 'gte', Value: String(filter.value.min) })
        }
        if (filter.value.max !== null) {
          list.push({ Key: filter.key, Op: 'lte', Value: String(filter.value.max) })
        }
        break
      case 'select':
        if (filter.value) {
          list.push({ Key: filter.key, Op: 'eq', Value: filter.value })
        }
        break
      case 'boolean':
        list.push({ Key: filter.key, Op: 'eq', Value: String(filter.value) })
        break
    }
  })
  return list
}

// Detail dialog state
const showDetailDialog = ref(false)
const selectedPart = ref(null)

// Part files
const partFiles = ref([])
const filesLoading = ref(false)

// Edit existing filter (unique to SpecFilterPanel — uses props.template)
const editFilter = (filter) => {
  editingFilterKey.value = filter.key
  editingParam.value = props.template.paramDefs.find(p => p.key === filter.key)
  filterValue.string = filter.value?.string || ''
  filterValue.min = filter.value?.min ?? null
  filterValue.max = filter.value?.max ?? null
  filterValue.selected = filter.value?.selected ?? null
  filterValue.bool = filter.value?.bool ?? false
  showFilterModal.value = true
}

// View part detail
const viewPart = (part) => {
  selectedPart.value = part
  showDetailDialog.value = true
  loadPartFiles(part.id)
}

// Load part files
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

// Format file size
const formatFileSize = (bytes) => {
  if (!bytes) return '-'
  if (bytes < 1024) return bytes + ' B'
  if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB'
  return (bytes / (1024 * 1024)).toFixed(1) + ' MB'
}
</script>

<style scoped>
.spec-filter-panel {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.search-bar {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 8px;
  padding: 12px 16px;
  background: #f5f7fa;
  border-radius: 8px;
}

.filter-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  flex: 1;
  min-width: 200px;
}

.filter-tag {
  cursor: pointer;
}

.range-sep {
  margin: 0 4px;
  color: #999;
}

.qty-label {
  margin-left: 4px;
  color: #666;
  font-size: 12px;
}

.unit-label {
  margin-left: 8px;
  color: #999;
  font-size: 12px;
}

.result-card {
  flex: 1;
  min-height: 0;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.specs-display {
  display: flex;
  flex-wrap: wrap;
  gap: 4px;
}

.spec-item {
  background: #f5f7fa;
  padding: 2px 6px;
  border-radius: 4px;
  font-size: 12px;
  margin-right: 4px;
}

.more-specs {
  color: #409eff;
  font-size: 12px;
}

.no-specs {
  color: #999;
}

.number-range-edit {
  display: flex;
  align-items: center;
  gap: 8px;
}
</style>
