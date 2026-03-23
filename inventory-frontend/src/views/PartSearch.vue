<template>
  <div class="part-search">
    <el-card class="search-card">
      <template #header>
        <div class="card-header">
          <span>部件搜索</span>
          <el-button type="primary" link @click="resetSearch">重置搜索</el-button>
        </div>
      </template>

      <el-form :model="searchForm" label-width="100px">
        <!-- 多级分类选择 -->
        <el-form-item label="分类">
          <el-cascader
            v-model="selectedCategoryPath"
            :options="categoryTree"
            :props="cascaderProps"
            placeholder="选择分类（支持多级）"
            clearable
            filterable
            @change="onCategoryChange"
            style="width: 100%"
          />
        </el-form-item>

        <!-- 关键字搜索 -->
        <el-form-item label="关键字">
          <el-input
            v-model="searchForm.keyword"
            placeholder="搜索名称、型号、描述、厂商、品牌、标签"
            clearable
            @keyup.enter="doSearch"
          />
        </el-form-item>

        <!-- 动态规格过滤 -->
        <template v-if="selectedTemplate">
          <el-divider content-position="left">规格过滤</el-divider>
          <div class="spec-filters">
            <el-form-item
              v-for="param in selectedTemplate.paramDefs"
              :key="param.key"
              :label="param.label"
            >
              <!-- string 类型 -->
              <el-input
                v-if="param.dataType === 'string'"
                v-model="specFilters[param.key]"
                :placeholder="`输入${param.label}`"
                clearable
              />
              <!-- number 类型 -->
              <div v-else-if="param.dataType === 'number'" class="number-filter">
                <el-select v-model="specFiltersOp[param.key]" style="width: 100px;">
                  <el-option label="=" value="eq" />
                  <el-option label=">" value="gt" />
                  <el-option label="<" value="lt" />
                  <el-option label=">=" value="gte" />
                  <el-option label="<=" value="lte" />
                </el-select>
                <el-input-number
                  v-model="specFiltersNum[param.key]"
                  :placeholder="param.unit || param.label"
                  style="width: 150px;"
                />
                <span v-if="param.unit" class="unit-label">{{ param.unit }}</span>
              </div>
              <!-- select 类型 -->
              <el-select
                v-else-if="param.dataType === 'select'"
                v-model="specFilters[param.key]"
                :placeholder="`选择${param.label}`"
                clearable
              >
                <el-option
                  v-for="opt in param.options"
                  :key="opt"
                  :label="opt"
                  :value="opt"
                />
              </el-select>
              <!-- boolean 类型 -->
              <el-switch
                v-else-if="param.dataType === 'boolean'"
                v-model="specFiltersBool[param.key]"
              />
            </el-form-item>
          </div>
        </template>

        <!-- 库存数量过滤 -->
        <el-divider content-position="left">库存过滤</el-divider>
        <el-form-item label="可用数量">
          <div class="qty-filter">
            <el-input-number
              v-model="searchForm.minAvailableQty"
              :min="0"
              placeholder="最小"
              style="width: 120px;"
            />
            <span class="separator">-</span>
            <el-input-number
              v-model="searchForm.maxAvailableQty"
              :min="0"
              placeholder="最大"
              style="width: 120px;"
            />
          </div>
        </el-form-item>

        <!-- 搜索按钮 -->
        <el-form-item>
          <el-button type="primary" @click="doSearch" :loading="searching">
            搜索
          </el-button>
          <el-button @click="resetSearch">重置</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <!-- 搜索结果 -->
    <el-card class="result-card" v-loading="searching">
      <template #header>
        <div class="card-header">
          <span>搜索结果 {{ totalCount > 0 ? `(${totalCount} 个)` : '' }}</span>
        </div>
      </template>

      <el-table :data="results" stripe style="width: 100%" max-height="500">
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
        <el-table-column label="操作" width="120" fixed="right">
          <template #default="{row}">
            <el-button size="small" @click="viewPart(row)">详情</el-button>
          </template>
        </el-table-column>
      </el-table>

      <div v-if="results.length === 0 && !searching" class="empty-result">
        <el-empty description="请输入搜索条件" />
      </div>
    </el-card>

    <!-- 部件详情对话框 -->
    <el-dialog v-model="showDetailDialog" :title="selectedPart?.name" width="700px">
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

      <template #footer>
        <el-button @click="showDetailDialog = false">关闭</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted, watch } from 'vue'
import { ElMessage } from 'element-plus'
import { getPartCategories, getPartCategoryById } from '@/api/partCategories'
import { getTemplates } from '@/api/templates'
import { searchParts } from '@/api/parts'

const selectedCategoryPath = ref([])
const categoryTree = ref([])
const cascaderProps = {
  checkStrictly: true,
  emitPath: true,
  value: 'path',
  label: 'name',
  children: 'children'
}

const searchForm = reactive({
  keyword: '',
  minAvailableQty: null,
  maxAvailableQty: null
})

const selectedTemplateId = ref(null)
const selectedTemplate = ref(null)
const specFilters = ref({})
const specFiltersOp = ref({})
const specFiltersNum = ref({})
const specFiltersBool = ref({})

const results = ref([])
const totalCount = ref(0)
const searching = ref(false)
const showDetailDialog = ref(false)
const selectedPart = ref(null)

// 加载分类树
const loadCategories = async () => {
  try {
    const res = await getPartCategories()
    const categories = res.data || []

    // 构建树形结构
    const treeMap = {}
    const roots = []

    categories.forEach(cat => {
      treeMap[cat.path] = { ...cat, children: [] }
    })

    categories.forEach(cat => {
      if (cat.parentId) {
        const parent = categories.find(c => c.id === cat.parentId)
        if (parent && treeMap[parent.path]) {
          treeMap[parent.path].children.push(treeMap[cat.path])
        }
      } else {
        roots.push(treeMap[cat.path])
      }
    })

    // 如果没有分类，创建默认分类
    if (roots.length === 0) {
      // 使用部件中已有的分类
      const res2 = (await searchParts({})).data
      const existingCategories = [...new Set(res2.map(p => p.category).filter(c => c))]
      roots.push(...buildSimpleTree(existingCategories))
    }

    categoryTree.value = roots
  } catch (error) {
    console.error('Failed to load categories', error)
  }
}

// 从已有分类构建简单的树结构
const buildSimpleTree = (categories) => {
  const roots = []
  const pathMap = {}

  categories.forEach(cat => {
    const parts = cat.split('/')
    let currentPath = ''
    let parent = null

    parts.forEach((part, idx) => {
      currentPath = idx === 0 ? part : `${currentPath}/${part}`

      if (!pathMap[currentPath]) {
        const node = {
          id: currentPath,
          name: part,
          path: currentPath,
          children: []
        }
        pathMap[currentPath] = node

        if (parent) {
          parent.children.push(node)
        } else {
          roots.push(node)
        }
      }
      parent = pathMap[currentPath]
    })
  })

  return roots
}

// 加载模板列表
const loadTemplates = async () => {
  try {
    const res = await getTemplates()
    // 模板加载完成
  } catch (error) {
    console.error('Failed to load templates', error)
  }
}

// 分类变化时，加载对应模板
const onCategoryChange = async (path) => {
  if (!path || path.length === 0) {
    selectedTemplate.value = null
    return
  }

  const categoryPath = path[path.length - 1]
  try {
    const res = (await getPartCategoryById(categoryPath)).data
    if (res && res.specTemplateId) {
      selectedTemplateId.value = res.specTemplateId
      const templatesRes = (await getTemplates()).data || []
      selectedTemplate.value = templatesRes.find(t => t.id === res.specTemplateId)
    } else {
      selectedTemplate.value = null
    }
  } catch (error) {
    // 可能是通过路径ID查找，尝试通过路径查找
    try {
      const templatesRes = (await getTemplates()).data || []
      // 尝试匹配分类名称
      const categoryName = categoryPath.split('/').pop()
      selectedTemplate.value = templatesRes.find(t =>
        t.category === categoryName ||
        categoryPath.includes(t.category)
      )
    } catch (e) {
      console.error('Failed to load template for category', e)
    }
  }

  // 清空规格过滤
  clearSpecFilters()
}

const clearSpecFilters = () => {
  specFilters.value = {}
  specFiltersOp.value = {}
  specFiltersNum.value = {}
  specFiltersBool.value = {}
}

// 执行搜索
const doSearch = async () => {
  searching.value = true
  try {
    // 构建规格过滤
    const specFiltersList = []

    // string 类型
    Object.entries(specFilters.value).forEach(([key, value]) => {
      if (value !== '' && value !== null && value !== undefined) {
        specFiltersList.push({ Key: key, Op: 'contains', Value: value })
      }
    })

    // number 类型
    Object.entries(specFiltersNum.value).forEach(([key, value]) => {
      if (value !== '' && value !== null && value !== undefined) {
        const op = specFiltersOp.value[key] || 'eq'
        specFiltersList.push({ Key: key, Op: op, Value: String(value) })
      }
    })

    // select 类型
    Object.entries(specFilters.value).forEach(([key, value]) => {
      if (value !== '' && value !== null && value !== undefined) {
        if (!specFiltersList.some(f => f.Key === key)) {
          specFiltersList.push({ Key: key, Op: 'eq', Value: value })
        }
      }
    })

    const searchParams = {
      keyword: searchForm.keyword || null,
      categoryPath: selectedCategoryPath.value?.length > 0 ? selectedCategoryPath.value[selectedCategoryPath.value.length - 1] : null,
      specFilters: specFiltersList.length > 0 ? specFiltersList : null,
      minAvailableQty: searchForm.minAvailableQty || null,
      maxAvailableQty: searchForm.maxAvailableQty || null
    }

    const res = (await searchParts(searchParams)).data
    results.value = res || []
    totalCount.value = results.value.length
  } catch (error) {
    ElMessage.error('搜索失败: ' + (error.message || '未知错误'))
    results.value = []
    totalCount.value = 0
  } finally {
    searching.value = false
  }
}

// 重置搜索
const resetSearch = () => {
  selectedCategoryPath.value = []
  searchForm.keyword = ''
  searchForm.minAvailableQty = null
  searchForm.maxAvailableQty = null
  selectedTemplate.value = null
  clearSpecFilters()
  results.value = []
  totalCount.value = 0
}

// 查看部件详情
const viewPart = (part) => {
  selectedPart.value = part
  showDetailDialog.value = true
}

onMounted(async () => {
  await loadCategories()
  await loadTemplates()
})
</script>

<style scoped>
.part-search {
  padding: 20px;
}

.search-card {
  margin-bottom: 20px;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.spec-filters {
  padding: 0 20px;
}

.spec-filters .el-form-item {
  margin-bottom: 12px;
}

.number-filter {
  display: flex;
  align-items: center;
  gap: 8px;
}

.number-filter .unit-label {
  color: #999;
  font-size: 12px;
}

.separator {
  margin: 0 8px;
  color: #999;
}

.qty-filter {
  display: flex;
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

.empty-result {
  padding: 40px 0;
}
</style>
