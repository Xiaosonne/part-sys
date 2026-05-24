<template>
	<div class="accessory-list-container layout-padding">
		<div class="accessory-list-layout layout-padding-auto">
			<el-card class="accessory-left" shadow="hover">
				<AccessoryCategoryTree
					:selected-id="selectedCategory?.id || null"
					@select="onCategorySelect"
				/>
			</el-card>

			<el-card class="accessory-right" shadow="hover">
				<div class="accessory-toolbar mb15">
					<el-input
						v-model="state.search.keyword"
						size="default"
						clearable
						placeholder="搜索配件名称、型号..."
						style="max-width: 220px"
						@keyup.enter="loadAccessories"
					/>
					<span class="ml10 range-label">库存</span>
					<el-input-number v-model="state.search.minQty" class="ml6" size="default" :min="0" controls-position="right" />
					<span class="ml6 mr6">-</span>
					<el-input-number v-model="state.search.maxQty" size="default" :min="0" controls-position="right" />
					<el-button size="default" type="primary" class="ml10" @click="loadAccessories">
						<el-icon><ele-Search /></el-icon>
						搜索
					</el-button>
					<el-button size="default" class="ml10" @click="onReset">
						重置
					</el-button>
					<div class="flex-fill"></div>
					<el-tooltip :disabled="!!selectedCategory?.path" content="请先在左侧选择分类" placement="bottom">
						<el-button size="default" type="primary" @click="onOpenAdd" :disabled="!selectedCategory?.path">
							<el-icon><ele-Plus /></el-icon>
							新增配件
						</el-button>
					</el-tooltip>
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

		<el-dialog v-model="state.dialog.visible" :title="state.dialog.editing ? '编辑配件' : '新增配件'" width="620px">
			<el-form :model="state.dialog.form" label-width="90px">
				<el-form-item label="名称" required>
					<el-input v-model="state.dialog.form.name" placeholder="配件名称" clearable />
				</el-form-item>
				<el-form-item label="型号">
					<el-input v-model="state.dialog.form.model" placeholder="型号" clearable />
				</el-form-item>
				<el-form-item label="品牌">
					<el-input v-model="state.dialog.form.brand" placeholder="品牌" clearable />
				</el-form-item>
				<el-form-item label="厂商">
					<el-input v-model="state.dialog.form.manufacturer" placeholder="厂商" clearable />
				</el-form-item>
				<el-form-item label="描述">
					<el-input v-model="state.dialog.form.description" type="textarea" rows="2" />
				</el-form-item>
				<el-form-item label="分类">
					<el-input :model-value="state.dialog.form.category" disabled />
				</el-form-item>
				<el-form-item label="标签">
					<el-select v-model="state.dialog.tags" multiple filterable allow-create default-first-option placeholder="添加标签">
						<el-option v-for="tag in allTags" :key="tag" :label="tag" :value="tag" />
					</el-select>
				</el-form-item>
				<el-form-item label="总数量">
					<el-input-number v-model="state.dialog.form.totalQty" :min="0" controls-position="right" />
				</el-form-item>
			</el-form>
			<template #footer>
				<el-button @click="state.dialog.visible = false">取消</el-button>
				<el-button type="primary" :loading="state.dialog.saving" @click="onSave">保存</el-button>
			</template>
		</el-dialog>
	</div>
</template>
<script setup lang="ts">
import { computed, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import AccessoryCategoryTree from '/@/views/accessoryManagement/components/AccessoryCategoryTree.vue';
import type { AccessoryCategoryTreeItem, AccessoryItem } from '/@/api/accessory';
import { createAccessory, deleteAccessory, searchAccessories, updateAccessory } from '/@/api/accessory';

const selectedCategory = ref<AccessoryCategoryTreeItem | null>(null);

const state = reactive({
	search: {
		keyword: '',
		minQty: null as number | null,
		maxQty: null as number | null,
	},
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
});

const allTags = computed(() => {
	const tags = new Set<string>();
	state.table.data.forEach((p) => (p.tags || []).forEach((t) => tags.add(t)));
	return Array.from(tags);
});

const pagedData = computed(() => {
	const start = (state.table.pageNum - 1) * state.table.pageSize;
	return state.table.data.slice(start, start + state.table.pageSize);
});

// 选择左侧分类后加载右侧列表
const onCategorySelect = (cat: AccessoryCategoryTreeItem) => {
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
	Object.keys(payload).forEach((k) => {
		if (payload[k] === null || payload[k] === '' || payload[k] === undefined) delete payload[k];
	});
	return payload;
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

// 重置搜索条件并重新加载
const onReset = () => {
	state.search.keyword = '';
	state.search.minQty = null;
	state.search.maxQty = null;
	state.table.pageNum = 1;
	loadAccessories();
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
};

// 打开新增弹窗
const onOpenAdd = () => {
	resetDialogForm();
	state.dialog.visible = true;
};

// 打开编辑弹窗
const onOpenEdit = (row: AccessoryItem) => {
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
	state.dialog.visible = true;
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
		const payload = {
			...state.dialog.form,
			tags: state.dialog.tags,
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

.accessory-toolbar {
	display: flex;
	align-items: center;
	gap: 6px;
	flex-wrap: wrap;
}

.flex-fill {
	flex: 1;
}

.specs-cell {
	display: inline-flex;
	align-items: center;
	gap: 10px;
}

.spec-item {
	color: var(--el-text-color-regular);
}

.more-specs {
	display: inline-flex;
	align-items: center;
	color: var(--el-color-primary);
	background: var(--el-color-primary-light-9);
	border-radius: 10px;
	padding: 0 8px;
	height: 20px;
}
</style>
