<template>
  <div class="page-container">
    <!-- Left: Category Tree -->
    <div class="page-sidebar">
      <CategoryTree
        ref="categoryTreeRef"
        :selected-category-id="selectedCategoryId"
        @select="onCategorySelect"
        @open-template-manager="goToTemplateManager"
      />
    </div>

    <!-- Right: Main Content -->
    <div class="page-main">
      <div class="content-tabs">
        <el-tabs v-model="activeTab" @tab-change="onTabChange">
          <!-- Part List Tab -->
          <el-tab-pane label="配件列表" name="parts">
              <PartListPanel
                ref="partListRef"
                :category-path="selectedCategoryPath"
                :category-id="selectedCategoryId"
              />
            </el-tab-pane>

            <!-- Spec Search Tab (only when category has merged spec params) -->
            <el-tab-pane label="规格搜索" name="search" :disabled="!effectiveSpecTemplate">
              <SpecFilterPanel
                :template="effectiveSpecTemplate"
                :category-path="selectedCategoryPath"
              />
            </el-tab-pane>

            <!-- Category Settings Tab -->
            <el-tab-pane label="分类设置" name="settings" :disabled="!selectedCategory">
              <CategorySettingsPanel
                :category="selectedCategory"
                :all-categories="allCategories"
                :templates="templates"
                @select="onCategorySelect"
                @add-child="onAddChildCategory"
                @refresh="refreshCategories"
              />
            </el-tab-pane>
          </el-tabs>
        </div>
      </div>
    </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import CategoryTree from '@/components/part-management/CategoryTree.vue'
import PartListPanel from '@/components/part-management/PartListPanel.vue'
import SpecFilterPanel from '@/components/part-management/SpecFilterPanel.vue'
import CategorySettingsPanel from '@/components/part-management/CategorySettingsPanel.vue'
import { getPartCategories, getPartCategoryById } from '@/api/partCategories'
import { getTemplates } from '@/api/templates'

const router = useRouter()

// Refs
const categoryTreeRef = ref(null)
const partListRef = ref(null)

// State
const activeTab = ref('parts')
const selectedCategoryId = ref(null)
const selectedCategory = ref(null)
const allCategories = ref([])
const templates = ref([])

// Computed
const selectedCategoryPath = computed(() => {
  return selectedCategory.value?.path || null
})

// 获取有效规格参数（子级覆盖父级）
const getEffectiveSpecParams = (categoryId) => {
  const category = allCategories.value.find(c => c.id === categoryId)
  if (!category) return []

  let params = []

  // 递归获取父级参数
  if (category.parentId) {
    params = getEffectiveSpecParams(category.parentId)
  }

  // 当前级参数覆盖父级
  if (category.specParams?.length > 0) {
    const currentKeys = new Set(category.specParams.map(p => p.key))
    params = [
      ...params.filter(p => !currentKeys.has(p.key)),
      ...category.specParams
    ]
  }

  return params
}

// 合并后的规格模板（用于传递给子组件）
const effectiveSpecTemplate = computed(() => {
  if (!selectedCategory.value) return null

  const specParams = getEffectiveSpecParams(selectedCategoryId.value)

  // 如果没有任何规格参数，返回 null
  if (specParams.length === 0) return null

  return {
    id: selectedCategory.value.specTemplateId,
    category: selectedCategory.value.name,
    paramDefs: specParams
  }
})

// Load initial data
const loadData = async () => {
  // Load templates
  try {
    const tplRes = (await getTemplates()).data
    templates.value = tplRes || []
  } catch (error) {
    console.error('加载模板失败', error)
  }

  // Load categories
  await refreshCategories()
}

const refreshCategories = async () => {
  try {
    const res = (await getPartCategories()).data
    allCategories.value = res || []
  } catch (error) {
    console.error('加载分类失败', error)
  }
}

// Category selection handler
const onCategorySelect = async (category) => {
  selectedCategoryId.value = category.id
  selectedCategory.value = category

  // Switch to parts tab
  activeTab.value = 'parts'

  // Refresh parts list
  if (partListRef.value) {
    partListRef.value.loadParts()
  }
}

// Tab change handler
const onTabChange = (tabName) => {
  // Could add analytics or other handling here
}

// Add child category
const onAddChildCategory = (parentId) => {
  if (categoryTreeRef.value) {
    categoryTreeRef.value.showAddDialog(parentId)
  }
}

// Navigate to template manager
const goToTemplateManager = () => {
  router.push('/spec-templates')
}

// onMounted
onMounted(() => {
  loadData()
})
</script>

<style scoped>
.part-management {
  height: calc(100vh - var(--header-height));
}

.content-tabs {
  flex: 1;
  padding: 0 16px;
  display: flex;
  flex-direction: column;
  padding: 0 20px;
  background-color: #fff;
  overflow: hidden;
  display: flex;
  flex-direction: column;
}

.content-tabs :deep(.el-tabs__content) {
  flex: 1;
  overflow: auto;
}

.content-tabs :deep(.el-tab-pane) {
  height: 100%;
}
</style>
