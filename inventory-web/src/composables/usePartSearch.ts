import { ref, computed, reactive } from 'vue'
import { ElMessage } from 'element-plus'
import { getAccessoryCategories, getAccessorySpecTemplates, searchAccessories } from '/@/api/accessory'

/**
 * Shared part search composable with category + spec parameter filtering.
 *
 * @param {Object} options
 * @param {Function} options.searchApi - API function to call (defaults to searchAccessories)
 */
export function usePartSearch(options = {}) {
  const searchApi = options.searchApi || searchAccessories

  // ── Data ──────────────────────────────────────────────────────────────────
  const categories = ref<any[]>([])
  const templates = ref<any[]>([])

  // ── Search inputs ─────────────────────────────────────────────────────────
  const keyword = ref('')
  const categoryPath = ref<string | null>(null)
  const minQty = ref<number | null>(null)
  const maxQty = ref<number | null>(null)
  const searching = ref(false)
  const results = ref<any[]>([])

  // ── Template (provided by parent or computed from category) ───────────────
  const template = ref<any>(null)  // { id, category, paramDefs: [...] }

  // ── Active spec filters (shown as tags) ──────────────────────────────────
  const activeFilters = ref<any[]>([])

  // ── Filter editor modal ───────────────────────────────────────────────────
  const showFilterModal = ref(false)
  const editingParam = ref<any>(null)          // param being edited
  const editingFilterKey = ref<string | null>(null)       // null = new filter, else editing existing
  const filterValue = reactive({
    string: '',
    min: null as number | null,
    max: null as number | null,
    selected: null as any,
    bool: false
  })

  // ── Effective spec params (inherited from ancestor categories) ───────────
  const getEffectiveSpecParams = (categoryId: string) => {
    const cat = categories.value.find(c => c.id === categoryId)
    if (!cat) return []
    let params = []
    if (cat.parentId) {
      params = getEffectiveSpecParams(cat.parentId)
    }
    if (cat.specParams?.length > 0) {
      const currentKeys = new Set(cat.specParams.map((p: any) => p.key))
      params = [
        ...params.filter((p: any) => !currentKeys.has(p.key)),
        ...cat.specParams
      ]
    }
    return params
  }

  /**
   * Update the template based on selected category ID.
   * Called by parent when category selection changes.
   */
  const updateTemplateFromCategory = (categoryId: string | null) => {
    if (!categoryId) {
      template.value = null
      activeFilters.value = []
      return
    }
    const specParams = getEffectiveSpecParams(categoryId)
    const cat = categories.value.find(c => c.id === categoryId)
    if (specParams.length > 0) {
      template.value = {
        id: cat?.specTemplateId,
        category: cat?.name,
        paramDefs: specParams
      }
    } else {
      template.value = null
    }
    activeFilters.value = []
  }

  // ── Available params for "add filter" dropdown ───────────────────────────
  const availableParams = computed(() => {
    if (!template.value?.paramDefs) return []
    const activeKeys = new Set(activeFilters.value.map(f => f.key))
    return template.value.paramDefs.filter((p: any) => !activeKeys.has(p.key))
  })

  // ── Load categories + templates ───────────────────────────────────────────
  const loadCategories = async (forceReload = false) => {
    if (categories.value.length > 0 && !forceReload) return  // cache
    try {
      const [catRes, tplRes] = await Promise.all([
        getAccessoryCategories(),
        getAccessorySpecTemplates()
      ])
      categories.value = (catRes as any) || []
      templates.value = (tplRes as any) || []
    } catch (e) {
      console.error('加载分类/模板失败', e)
    }
  }

  // ── Category tree (flat list → tree structure for cascader) ───────────────
  const categoryTree = computed(() => {
    const map: any = {}
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

  // ── Build spec filters list for API ──────────────────────────────────────
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

  // ── Execute search ────────────────────────────────────────────────────────
  const doSearch = async (extraParams = {}) => {
    searching.value = true
    try {
      const specFiltersList = buildSpecFiltersList()
      const params = {
        keyword: keyword.value || null,
        categoryPath: categoryPath.value,
        specFilters: specFiltersList.length > 0 ? specFiltersList : null,
        minAvailableQty: minQty.value || null,
        maxAvailableQty: maxQty.value || null,
        ...extraParams
      }
      const res = (await searchApi(params as any)) as any
      results.value = res || []
    } catch (error) {
      ElMessage.error('搜索配件失败')
      results.value = []
    } finally {
      searching.value = false
    }
  }

  // ── Reset all filters ─────────────────────────────────────────────────────
  const resetFilters = () => {
    keyword.value = ''
    categoryPath.value = null
    minQty.value = null
    maxQty.value = null
    activeFilters.value = []
    template.value = null
    results.value = []
  }

  // ── Format filter value for display in tag ────────────────────────────────
  const formatFilterValue = (filter: any) => {
    switch (filter.type) {
      case 'string':
        return filter.value || ''
      case 'number-range':
        if (filter.value.min !== null && filter.value.max !== null) {
          return `${filter.value.min}-${filter.value.max}${filter.unit || ''}`
        } else if (filter.value.min !== null) {
          return `≥${filter.value.min}${filter.unit || ''}`
        } else if (filter.value.max !== null) {
          return `≤${filter.value.max}${filter.unit || ''}`
        }
        return ''
      case 'select':
        return filter.value || ''
      case 'boolean':
        return filter.value ? '是' : '否'
      default:
        return filter.value || ''
    }
  }

  // ── Filter CRUD ───────────────────────────────────────────────────────────
  const handleAddFilter = (param: any) => {
    editingFilterKey.value = null
    editingParam.value = param
    resetFilterValue()
    showFilterModal.value = true
  }

  const removeFilter = (key: string) => {
    activeFilters.value = activeFilters.value.filter(f => f.key !== key)
  }

  const resetFilterValue = () => {
    filterValue.string = ''
    filterValue.min = null
    filterValue.max = null
    filterValue.selected = null
    filterValue.bool = false
  }

  const confirmFilter = () => {
    if (!editingParam.value) return
    const param = editingParam.value
    let value = null
    let isValid = false

    switch (param.dataType) {
      case 'string':
        if (filterValue.string && filterValue.string.trim()) {
          value = filterValue.string.trim()
          isValid = true
        }
        break
      case 'number':
        if (filterValue.min !== null || filterValue.max !== null) {
          value = { min: filterValue.min, max: filterValue.max }
          isValid = true
        }
        break
      case 'select':
        if (filterValue.selected) {
          value = filterValue.selected
          isValid = true
        }
        break
      case 'boolean':
        value = filterValue.bool
        isValid = true
        break
    }

    if (!isValid) {
      ElMessage.warning('请设置有效的过滤值')
      return
    }

    if (editingFilterKey.value) {
      activeFilters.value = activeFilters.value.filter(f => f.key !== editingFilterKey.value)
    }

    activeFilters.value.push({
      key: param.key,
      label: param.label,
      type: param.dataType === 'number' ? 'number-range' : param.dataType,
      value: param.dataType === 'number'
        ? { min: filterValue.min, max: filterValue.max }
        : param.dataType === 'select'
          ? filterValue.selected
          : param.dataType === 'boolean'
            ? filterValue.bool
            : filterValue.string,
      unit: param.unit,
      options: param.options
    })

    showFilterModal.value = false
  }

  return {
    // Data
    categories,
    templates,
    categoryTree,

    // Search inputs
    keyword,
    categoryPath,
    minQty,
    maxQty,

    // Results
    searching,
    results,

    // Template
    template,
    updateTemplateFromCategory,

    // Filters
    activeFilters,
    availableParams,
    showFilterModal,
    editingParam,
    editingFilterKey,
    filterValue,

    // Methods
    loadCategories,
    buildSpecFiltersList,
    doSearch,
    resetFilters,
    formatFilterValue,
    handleAddFilter,
    removeFilter,
    confirmFilter,
    resetFilterValue
  }
}
