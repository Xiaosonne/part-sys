<template>
	<div class="accessory-list-container layout-padding">
		<div class="accessory-list-layout layout-padding-auto">
			<el-card class="accessory-left" shadow="hover">
				<AccessoryCategoryTree
					ref="treeRef"
					:selected-id="selectedCategoryId"
					@select="onCategorySelect"
				/>
			</el-card>

			<el-card class="accessory-right" shadow="hover">
				<div class="search-bar mb15">
					<div class="search-row">
						<el-input
							v-model="state.search.keyword"
							size="default"
							clearable
							placeholder="搜索配件名称、型号..."
							style="width: 180px"
							@keyup.enter="loadAccessories"
						/>

						<!-- Spec Filter Tags -->
						<div class="filter-tags" v-if="state.activeFilters.length > 0">
							<el-tag
								v-for="filter in state.activeFilters"
								:key="filter.key"
								closable
								@close="removeFilter(filter.key)"
								size="small"
							>
								{{ filter.label }}: {{ formatFilterValue(filter) }}
							</el-tag>
						</div>

						<!-- Add Filter Dropdown (only if template has params) -->
						<el-dropdown v-if="currentCategoryTemplate?.paramDefs?.length" @command="handleAddFilter" trigger="click">
							<el-button type="primary" plain size="default">
								<el-icon><ele-Plus /></el-icon>
								添加筛选
							</el-button>
							<template #dropdown>
								<el-dropdown-menu>
									<el-dropdown-item v-for="param in availableParams" :key="param.key" :command="param">
										{{ param.label }}
									</el-dropdown-item>
								</el-dropdown-menu>
							</template>
						</el-dropdown>

						<el-divider direction="vertical" />

						<!-- Stock Range -->
						<span class="range-label">库存</span>
						<el-input-number
							v-model="state.search.minQty"
							:min="0"
							placeholder="最小"
							size="default"
							style="width: 90px"
							controls-position="right"
						/>
						<span class="range-sep">-</span>
						<el-input-number
							v-model="state.search.maxQty"
							:min="0"
							placeholder="最大"
							size="default"
							style="width: 90px"
							controls-position="right"
						/>

						<el-button type="primary" @click="loadAccessories" :loading="state.table.loading" size="default">搜索</el-button>
						<el-button @click="onReset" size="default" v-if="hasActiveFilters">重置</el-button>

						<div class="spacer"></div>

						<el-tooltip :disabled="!!selectedCategory?.path" content="请先在左侧选择分类" placement="bottom">
							<el-button size="default" type="primary" @click="onOpenAdd" :disabled="!selectedCategory?.path">
								<el-icon><ele-Plus /></el-icon>
								新增配件
							</el-button>
						</el-tooltip>
					</div>
				</div>

				<el-table :data="pagedData" v-loading="state.table.loading" style="width: 100%" stripe table-layout="auto">
					<el-table-column prop="name" label="名称" min-width="140" show-overflow-tooltip />
					<el-table-column prop="model" label="型号" min-width="120" show-overflow-tooltip />
					<el-table-column prop="brand" label="品牌" min-width="100" show-overflow-tooltip />
					<el-table-column label="标签" min-width="140">
						<template #default="{ row }">
							<el-tag v-for="tag in row.tags || []" :key="tag" size="small" class="mr6">{{ tag }}</el-tag>
						</template>
					</el-table-column>
					<el-table-column label="规格" min-width="160">
						<template #default="{ row }">
							<span v-if="row.specs && row.specs.length > 0" class="specs-cell">
								<span v-for="spec in row.specs.slice(0, 2)" :key="spec.key" class="spec-item">
									{{ spec.label }}: {{ spec.value }}{{ spec.unit }}
								</span>
								<span v-if="row.specs.length > 2" class="more-specs">+{{ row.specs.length - 2 }}</span>
							</span>
							<span v-else>-</span>
						</template>
					</el-table-column>
					<el-table-column prop="availableQty" label="可用" width="70" />
					<el-table-column prop="totalQty" label="总计" width="70" />
					<el-table-column label="操作" width="120" fixed="right">
						<template #default="{ row }">
							<el-button size="small" text type="primary" @click="onOpenEdit(row)">编辑</el-button>
							<el-button size="small" text type="primary" @click="onDelete(row)">删除</el-button>
						</template>
					</el-table-column>
				</el-table>

				<el-pagination
					class="mt15"
					background
					:pager-count="5"
					:page-sizes="[10, 20, 30, 50]"
					layout="total, sizes, prev, pager, next, jumper"
					v-model:current-page="state.table.pageNum"
					v-model:page-size="state.table.pageSize"
					:total="state.table.total"
					@size-change="onPageSizeChange"
					@current-change="onPageChange"
				/>
			</el-card>
		</div>

		<el-dialog v-model="state.filterDialog.visible" title="编辑筛选条件" width="400px">
			<el-form :model="state.filterDialog.form" label-width="80px" v-if="state.filterDialog.editingParam">
				<el-form-item :label="state.filterDialog.editingParam.label">
					<el-select v-if="state.filterDialog.editingParam.dataType === 'select'" v-model="state.filterDialog.form.value" placeholder="选择值">
						<el-option v-for="opt in state.filterDialog.editingParam.options" :key="opt" :label="opt" :value="opt" />
					</el-select>
					<el-select v-else-if="state.filterDialog.editingParam.dataType === 'boolean'" v-model="state.filterDialog.form.value">
						<el-option label="是" :value="true" />
						<el-option label="否" :value="false" />
					</el-select>
					<el-input-number v-else-if="state.filterDialog.editingParam.dataType === 'number'" v-model="state.filterDialog.form.value" />
					<el-input v-else v-model="state.filterDialog.form.value" :placeholder="state.filterDialog.editingParam.unit" />
				</el-form-item>
			</el-form>
			<template #footer>
				<el-button @click="state.filterDialog.visible = false">取消</el-button>
				<el-button type="primary" @click="confirmFilter">确定</el-button>
			</template>
		</el-dialog>

		<el-dialog v-model="state.dialog.visible" :title="state.dialog.editing ? '编辑配件' : '新增配件'" width="800px">
			<el-tabs>
				<el-tab-pane label="基本信息">
					<el-form :model="state.dialog.form" label-width="100px">
						<el-form-item label="名称" required>
							<el-input v-model="state.dialog.form.name" placeholder="配件名称" clearable />
						</el-form-item>
						<el-form-item label="型号">
							<el-input v-model="state.dialog.form.model" placeholder="型号" clearable />
						</el-form-item>
						<el-form-item label="描述">
							<el-input v-model="state.dialog.form.description" type="textarea" rows="2" />
						</el-form-item>
						<el-form-item label="厂商">
							<el-input v-model="state.dialog.form.manufacturer" placeholder="厂商" clearable />
						</el-form-item>
						<el-form-item label="品牌">
							<el-input v-model="state.dialog.form.brand" placeholder="品牌" clearable />
						</el-form-item>
						<el-form-item label="分类">
							<el-input :model-value="state.dialog.form.category" disabled />
						</el-form-item>
						<el-form-item label="模板">
							<el-select v-model="state.dialog.selectedTemplateId" @change="onTemplateChange" clearable placeholder="选择模板">
								<el-option v-for="tpl in templates" :key="tpl.id" :label="tpl.category" :value="tpl.id" />
							</el-select>
						</el-form-item>

						<template v-if="state.dialog.selectedTemplate">
							<el-divider content-position="left">规格参数</el-divider>
							<el-form-item v-for="param in state.dialog.selectedTemplate.paramDefs" :key="param.key" :label="param.label">
								<el-input v-if="param.dataType === 'string'" v-model="state.dialog.specValues[param.key]" :placeholder="param.unit" />
								<el-input-number v-else-if="param.dataType === 'number'" v-model="state.dialog.specValues[param.key]" :placeholder="param.unit" />
								<el-switch v-else-if="param.dataType === 'boolean'" v-model="state.dialog.specValues[param.key]" />
								<el-select v-else-if="param.dataType === 'select'" v-model="state.dialog.specValues[param.key]" clearable :placeholder="param.label">
									<el-option v-for="opt in param.options" :key="opt" :label="opt" :value="opt" />
								</el-select>
							</el-form-item>
						</template>

						<el-divider content-position="left">标签</el-divider>
						<el-form-item label="标签">
							<el-select v-model="state.dialog.tags" multiple filterable allow-create default-first-option placeholder="添加标签">
								<el-option v-for="tag in allTags" :key="tag" :label="tag" :value="tag" />
							</el-select>
						</el-form-item>

						<el-divider content-position="left">库存</el-divider>
						<el-form-item label="总数量">
							<el-input-number v-model="state.dialog.form.totalQty" :min="0" controls-position="right" />
						</el-form-item>
					</el-form>
				</el-tab-pane>

				<el-tab-pane label="文件管理">
					<div class="file-management">
						<div class="file-upload-header">
							<el-button type="primary" :loading="state.dialog.uploading" @click="triggerFileUpload">
								<el-icon><ele-Upload /></el-icon>
								上传文件
							</el-button>
							<input
								ref="fileInputRef"
								type="file"
								style="display: none"
								@change="handleFileSelected"
							/>
						</div>

						<el-table :data="state.dialog.partFiles" stripe style="width: 100%; margin-top: 12px" v-loading="state.dialog.filesLoading">
							<el-table-column prop="fileName" label="文件名" min-width="200" />
							<el-table-column label="大小" width="100">
								<template #default="{ row }">
									{{ formatFileSize(row.fileSize) }}
								</template>
							</el-table-column>
							<el-table-column prop="uploadedBy" label="上传人" width="100" />
							<el-table-column label="上传时间" width="180">
								<template #default="{ row }">
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

						<el-empty v-if="!state.dialog.filesLoading && state.dialog.partFiles.length === 0" description="暂无文件" />
					</div>
				</el-tab-pane>
			</el-tabs>
			<template #footer>
				<el-button @click="state.dialog.visible = false">取消</el-button>
				<el-button type="primary" :loading="state.dialog.saving" @click="onSave">保存</el-button>
			</template>
		</el-dialog>
	</div>
</template>
<script setup lang="ts">
import { computed, reactive, ref, watch, onMounted, nextTick } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { ElMessage, ElMessageBox } from 'element-plus';
import AccessoryCategoryTree from '/@/views/accessoryManagement/components/AccessoryCategoryTree.vue';
import type { AccessoryCategoryTreeItem, AccessoryItem, AccessorySpecTemplate, AccessoryFile, AccessoryParamDef } from '/@/api/accessory';
import { 
	createAccessory, 
	deleteAccessory, 
	searchAccessories, 
	updateAccessory,
	getAccessorySpecTemplates,
	getAccessorySpecTemplateById,
	uploadFile,
	getPartFiles,
	deleteFile,
	getAccessoryCategories
} from '/@/api/accessory';

const route = useRoute();
const router = useRouter();

const treeRef = ref<any>(null);
const selectedCategoryId = ref<string | null>(null); // 只保存 ID，让树组件来处理选中
const selectedCategory = ref<AccessoryCategoryTreeItem | null>(null);
const fileInputRef = ref<HTMLInputElement | null>(null);
const templates = ref<AccessorySpecTemplate[]>([]);
const allCategories = ref<AccessoryCategoryTreeItem[]>([]);

const state = reactive({
	search: {
		keyword: '',
		minQty: null as number | null,
		maxQty: null as number | null,
	},
	activeFilters: [] as any[],
	table: {
		loading: false,
		data: [] as AccessoryItem[],
		total: 0,
		pageNum: 1,
		pageSize: 10,
	},
	dialog: {
		visible: false,
		saving: false,
		editing: null as AccessoryItem | null,
		tags: [] as string[],
		selectedTemplateId: null as string | null,
		selectedTemplate: null as AccessorySpecTemplate | null,
		specValues: {} as Record<string, any>,
		partFiles: [] as AccessoryFile[],
		filesLoading: false,
		uploading: false,
		form: {
			name: '',
			model: '',
			brand: '',
			manufacturer: '',
			description: '',
			category: '',
			totalQty: 0,
		},
	},
	filterDialog: {
		visible: false,
		editingParam: null as AccessoryParamDef | null,
		form: {
			value: null as any,
		},
	},
});

const allTags = computed(() => {
	const tags = new Set<string>();
	state.table.data.forEach((p) => (p.tags || []).forEach((t) => tags.add(t)));
	return Array.from(tags);
});

const hasActiveFilters = computed(() => {
	return state.activeFilters.length > 0 || 
		   state.search.minQty !== null || 
		   state.search.maxQty !== null || 
		   state.search.keyword;
});

// 获取当前选中分类的规格模板
const currentCategoryTemplate = computed(() => {
	if (!selectedCategory.value?.specTemplateId) return null;
	return templates.value.find(t => t.id === selectedCategory.value?.specTemplateId);
});

// 可用于添加的筛选参数
const availableParams = computed(() => {
	if (!currentCategoryTemplate.value?.paramDefs) return [];
	const activeKeys = new Set(state.activeFilters.map(f => f.key));
	return currentCategoryTemplate.value.paramDefs.filter(p => !activeKeys.has(p.key) && String(p.label || '').trim());
});

// 从分类数据中获取有效的规格参数
const getEffectiveSpecParams = (categoryId: string) => {
	const cat = allCategories.value.find(c => c.id === categoryId);
	if (!cat) return [];
	let params: AccessoryParamDef[] = [];
	if (cat.parentId) {
		params = getEffectiveSpecParams(cat.parentId);
	}
	if (cat.specParams?.length) {
		const currentKeys = new Set(cat.specParams.map(p => p.key));
		params = [
			...params.filter(p => !currentKeys.has(p.key)),
			...cat.specParams
		];
	}
	return params;
};

const pagedData = computed(() => {
	const start = (state.table.pageNum - 1) * state.table.pageSize;
	return state.table.data.slice(start, start + state.table.pageSize);
});

// 选择左侧分类后加载右侧列表
const onCategorySelect = (cat: AccessoryCategoryTreeItem) => {
	selectedCategoryId.value = cat.id;
	selectedCategory.value = cat;
	state.table.pageNum = 1;
	loadAccessories();
};

// 组装搜索参数（与后端 /api/parts/search 对齐）
const buildSearchPayload = () => {
	const payload: any = {
		categoryPath: selectedCategory.value?.path || null,
		keyword: state.search.keyword || null,
		minAvailableQty: state.search.minQty,
		maxAvailableQty: state.search.maxQty,
	};
	// 添加规格筛选
	if (state.activeFilters.length > 0) {
		payload.specFilters = state.activeFilters.map(f => ({
			key: f.key,
			op: f.dataType === 'number' ? 'eq' : 'eq',
			value: String(f.value)
		}));
	}
	Object.keys(payload).forEach((k) => {
		if (payload[k] === null || payload[k] === '' || payload[k] === undefined) delete payload[k];
		if (Array.isArray(payload[k]) && payload[k].length === 0) delete payload[k];
	});
	return payload;
};

// 格式化筛选值显示
const formatFilterValue = (filter: any) => {
	if (filter.value === true) return '是';
	if (filter.value === false) return '否';
	return filter.value;
};

// 处理添加筛选
const handleAddFilter = (param: AccessoryParamDef) => {
	state.filterDialog.editingParam = param;
	if (param.dataType === 'boolean') {
		state.filterDialog.form.value = false;
	} else if (param.dataType === 'number') {
		state.filterDialog.form.value = null;
	} else if (param.options?.length > 0) {
		state.filterDialog.form.value = param.options[0];
	} else {
		state.filterDialog.form.value = '';
	}
	state.filterDialog.visible = true;
};

// 确认筛选
const confirmFilter = () => {
	if (!state.filterDialog.editingParam) return;
	state.activeFilters.push({
		key: state.filterDialog.editingParam.key,
		label: state.filterDialog.editingParam.label,
		value: state.filterDialog.form.value,
		dataType: state.filterDialog.editingParam.dataType,
		unit: state.filterDialog.editingParam.unit,
		options: state.filterDialog.editingParam.options
	});
	state.filterDialog.visible = false;
	state.filterDialog.editingParam = null;
};

// 删除筛选
const removeFilter = (key: string) => {
	state.activeFilters = state.activeFilters.filter(f => f.key !== key);
};

// 加载配件列表
const loadAccessories = async () => {
	state.table.loading = true;
	try {
		const res = (await searchAccessories(buildSearchPayload())) as AccessoryItem[];
		state.table.data = Array.isArray(res) ? res : [];
		state.table.total = state.table.data.length;
	} catch (e: any) {
		ElMessage.error(e?.message || '加载配件失败');
		state.table.data = [];
		state.table.total = 0;
	} finally {
		state.table.loading = false;
	}
};

// 加载模板列表
const loadTemplates = async () => {
	try {
		const res = (await getAccessorySpecTemplates()) as any;
		templates.value = Array.isArray(res) ? res : [];
	} catch (error) {
		console.error('加载模板失败', error);
	}
};

// 重置搜索条件并重新加载
const onReset = () => {
	state.search.keyword = '';
	state.search.minQty = null;
	state.search.maxQty = null;
	state.activeFilters = [];
	state.table.pageNum = 1;
	loadAccessories();
};

// 加载分类列表
const loadCategories = async () => {
	try {
		const res = await getAccessoryCategories();
		allCategories.value = Array.isArray(res) ? res : [];
	} catch (error) {
		console.error('加载分类失败', error);
	}
};

// 切换每页条数
const onPageSizeChange = (val: number) => {
	state.table.pageSize = val;
	state.table.pageNum = 1;
};

// 切换页码
const onPageChange = (val: number) => {
	state.table.pageNum = val;
};

// 初始化弹窗表单
const resetDialogForm = () => {
	state.dialog.form = {
		name: '',
		model: '',
		brand: '',
		manufacturer: '',
		description: '',
		category: selectedCategory.value?.path || '',
		totalQty: 0,
	};
	state.dialog.tags = [];
	state.dialog.editing = null;
	state.dialog.selectedTemplateId = null;
	state.dialog.selectedTemplate = null;
	state.dialog.specValues = {};
	state.dialog.partFiles = [];
};

// 打开新增弹窗
const onOpenAdd = () => {
	resetDialogForm();
	state.dialog.visible = true;
};

// 模板变化处理
const onTemplateChange = async (templateId: string | null) => {
	if (!templateId) {
		state.dialog.selectedTemplate = null;
		state.dialog.specValues = {};
		return;
	}
	try {
		const res = (await getAccessorySpecTemplateById(templateId)) as any;
		state.dialog.selectedTemplate = res;
		// 初始化规格值
		if (res?.paramDefs) {
			res.paramDefs.forEach((param: any) => {
				if (param.dataType === 'boolean') {
					state.dialog.specValues[param.key] = false;
				} else if (param.dataType === 'number') {
					state.dialog.specValues[param.key] = null;
				} else {
					state.dialog.specValues[param.key] = '';
				}
			});
		}
	} catch (error) {
		ElMessage.error('加载模板失败');
	}
};

// 打开编辑弹窗
const onOpenEdit = async (row: AccessoryItem) => {
	state.dialog.editing = row;
	state.dialog.form = {
		name: row.name || '',
		model: row.model || '',
		brand: row.brand || '',
		manufacturer: row.manufacturer || '',
		description: row.description || '',
		category: row.category || selectedCategory.value?.path || '',
		totalQty: row.totalQty ?? 0,
	};
	state.dialog.tags = Array.isArray(row.tags) ? [...row.tags] : [];
	
	// 处理规格相关
	if (row.specTemplateId) {
		state.dialog.selectedTemplateId = row.specTemplateId;
		await onTemplateChange(row.specTemplateId);
		// 填充规格值
		if (row.specs) {
			row.specs.forEach(spec => {
				if (state.dialog.specValues.hasOwnProperty(spec.key)) {
					state.dialog.specValues[spec.key] = spec.value;
				}
			});
		}
	} else {
		state.dialog.selectedTemplateId = null;
		state.dialog.selectedTemplate = null;
		state.dialog.specValues = {};
	}
	
	// 加载文件
	await loadPartFiles(row.id);
	
	state.dialog.visible = true;
};

// 加载配件文件
const loadPartFiles = async (partId: string) => {
	state.dialog.filesLoading = true;
	try {
		const res = (await getPartFiles(partId)) as any;
		state.dialog.partFiles = Array.isArray(res) ? res : [];
	} catch (error) {
		console.error('加载配件文件失败:', error);
		state.dialog.partFiles = [];
	} finally {
		state.dialog.filesLoading = false;
	}
};

// 触发文件上传
const triggerFileUpload = () => {
	if (!state.dialog.editing?.id) {
		ElMessage.warning('请先保存配件后再上传文件');
		return;
	}
	fileInputRef.value?.click();
};

// 处理文件选择
const handleFileSelected = async (event: Event) => {
	const target = event.target as HTMLInputElement;
	const file = target.files?.[0];
	if (!file) return;

	state.dialog.uploading = true;
	try {
		const formData = new FormData();
		formData.append('file', file);
		formData.append('bucket', 'parts');
		formData.append('relatedId', state.dialog.editing!.id);
		formData.append('fileType', 'PART');
		formData.append('description', '');

		await uploadFile(formData);
		ElMessage.success('文件上传成功');
		await loadPartFiles(state.dialog.editing!.id);
	} catch (error: any) {
		ElMessage.error('文件上传失败: ' + (error.message || '未知错误'));
	} finally {
		state.dialog.uploading = false;
		if (fileInputRef.value) {
			fileInputRef.value.value = '';
		}
	}
};

// 下载文件
const handleFileDownload = (file: AccessoryFile) => {
	window.open(`/api/files/${file.id}/download`, '_blank');
};

// 删除文件
const handleFileDelete = async (file: AccessoryFile) => {
	try {
		await ElMessageBox.confirm(`确定删除文件“${file.fileName}”吗？`, '警告', { type: 'warning' });
		await deleteFile(file.id);
		ElMessage.success('文件删除成功');
		await loadPartFiles(state.dialog.editing!.id);
	} catch (error) {
		// 取消操作不显示错误
	}
};

// 格式化文件大小
const formatFileSize = (bytes?: number) => {
	if (!bytes) return '-';
	if (bytes < 1024) return bytes + ' B';
	if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
	return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
};

// 保存（新增/编辑）
const onSave = async () => {
	if (!state.dialog.form.name?.trim()) {
		ElMessage.warning('请输入配件名称');
		return;
	}
	if (!state.dialog.form.category) {
		ElMessage.warning('请先选择分类');
		return;
	}
	state.dialog.saving = true;
	try {
		// 组装规格
		const specs: any[] = [];
		if (state.dialog.selectedTemplate) {
			state.dialog.selectedTemplate.paramDefs.forEach(param => {
				const val = state.dialog.specValues[param.key];
				if (val !== '' && val !== null && val !== undefined) {
					specs.push({
						key: param.key,
						label: param.label,
						value: String(val),
						unit: param.unit || ''
					});
				}
			});
		}

		const payload = {
			...state.dialog.form,
			tags: state.dialog.tags,
			specTemplateId: state.dialog.selectedTemplateId,
			specs
		};

		if (state.dialog.editing?.id) {
			await updateAccessory(state.dialog.editing.id, payload);
			ElMessage.success('配件更新成功');
		} else {
			await createAccessory(payload);
			ElMessage.success('配件创建成功');
		}
		state.dialog.visible = false;
		await loadAccessories();
	} catch (e: any) {
		ElMessage.error(e?.response?.data?.message || e?.message || '保存失败');
	} finally {
		state.dialog.saving = false;
	}
};

// 删除配件
const onDelete = async (row: AccessoryItem) => {
	try {
		await ElMessageBox.confirm(`此操作将永久删除配件：“${row.name}”，是否继续?`, '提示', {
			confirmButtonText: '删除',
			cancelButtonText: '取消',
			type: 'warning',
		});
		await deleteAccessory(row.id);
		ElMessage.success('删除成功');
		await loadAccessories();
	} catch (e) {}
};

let categoriesLoaded = false;
const initCategories = async () => {
	await loadCategories();
	categoriesLoaded = true;
};

// 初始化
loadTemplates();
initCategories();

onMounted(() => {
	// 检查路由参数中是否有 categoryId
	const categoryId = route.query.categoryId;
	if (categoryId && typeof categoryId === 'string') {
		// 只需要设置 selectedCategoryId，树组件会自动处理选中和加载配件列表
		selectedCategoryId.value = categoryId;
	}
});
</script>
<style lang="scss" scoped>
.accessory-list-container .accessory-list-layout {
	display: flex;
	flex-direction: row !important;
	flex-wrap: nowrap;
	gap: 12px;
	overflow: hidden;
	flex: 1;
	min-height: 0;
	width: 100%;
	height: 100%;
}

.accessory-left {
	width: 280px;
	flex: none;
	display: flex;
	flex-direction: column;
	:deep(.el-card__body) {
		flex: 1;
		display: flex;
		flex-direction: column;
		overflow: hidden;
	}
}

.accessory-right {
	flex: 1;
	min-width: 0;
	display: flex;
	flex-direction: column;
	:deep(.el-card__body) {
		flex: 1;
		display: flex;
		flex-direction: column;
		overflow: hidden;
	}
	.el-table {
		flex: 1;
	}
}

.search-bar {
	padding: 12px 16px;
}

.search-row {
	display: flex;
	align-items: center;
	flex-wrap: wrap;
	gap: 8px;
}

.filter-tags {
	display: flex;
	align-items: center;
	flex-wrap: wrap;
	gap: 4px;
}

.filter-tags .el-tag {
	margin-right: 0;
}

.spacer {
	flex: 1;
}

.range-label {
	font-size: 13px;
	color: var(--el-text-color-secondary);
}

.range-sep {
	color: var(--el-text-color-secondary);
}

.specs-cell {
	display: flex;
	flex-direction: column;
	gap: 2px;
	font-size: 12px;
	color: var(--el-text-color-secondary);
}

.spec-item {
	white-space: nowrap;
}

.more-specs {
	color: var(--el-color-primary);
	font-size: 11px;
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
