<template>
	<div class="accessory-classification-container layout-padding">
		<div class="accessory-classification-layout layout-padding-auto">
			<el-card class="accessory-left" shadow="hover">
				<AccessoryCategoryTree
					ref="treeRef"
					:selected-id="state.selectedId"
					@select="onCategorySelect"
				/>
			</el-card>

			<div class="accessory-right">
				<div class="accessory-right-scroll">
					<el-card shadow="hover" class="right-card">
						<template #header>
							<div class="card-header">
								<span>分类信息</span>
								<el-button type="primary" text :disabled="!state.category" @click="openEditDialog">
									编辑
								</el-button>
							</div>
						</template>

						<el-empty v-if="!state.category" description="请从左侧选择分类" />

						<template v-else>
							<el-descriptions :column="2" border>
								<el-descriptions-item label="名称">{{ state.category.name }}</el-descriptions-item>
								<el-descriptions-item label="路径">{{ state.category.path }}</el-descriptions-item>
								<el-descriptions-item label="排序">{{ state.category.sortOrder || 0 }}</el-descriptions-item>
								<el-descriptions-item label="关联模板">
									<el-tag v-if="currentTemplate" type="success">{{ currentTemplate.category }}</el-tag>
									<span v-else class="muted">无</span>
								</el-descriptions-item>
							</el-descriptions>

							<el-divider content-position="left">模板参数</el-divider>
							<el-table :data="templateParams" stripe size="small" table-layout="auto" style="width: 100%">
								<el-table-column prop="label" label="参数名" min-width="120" />
								<el-table-column prop="key" label="Key" min-width="120" />
								<el-table-column prop="dataType" label="类型" width="100" />
								<el-table-column prop="unit" label="单位" width="80" />
								<el-table-column label="选项" min-width="180">
									<template #default="{ row }">
										{{ row.options?.length ? row.options.join(', ') : '-' }}
									</template>
								</el-table-column>
							</el-table>
						</template>
					</el-card>

					<el-card shadow="hover" class="right-card child-card">
						<template #header>
							<div class="card-header">
								<span>子分类 ({{ children.length }})</span>
								<el-button type="primary" text :disabled="!state.category" @click="openAddChildDialog">
									<el-icon><ele-Plus /></el-icon>
									新增子分类
								</el-button>
							</div>
						</template>

						<el-table v-if="children.length > 0" :data="children" stripe size="small" table-layout="auto" style="width: 100%">
							<el-table-column prop="name" label="名称" min-width="160" show-overflow-tooltip />
							<el-table-column prop="sortOrder" label="排序" width="80" />
							<el-table-column label="模板" width="120">
								<template #default="{ row }">
									<el-tag v-if="row.specTemplateId" size="small" type="success">有</el-tag>
									<span v-else>-</span>
								</template>
							</el-table-column>
							<el-table-column label="操作" width="140" fixed="right">
								<template #default="{ row }">
									<el-button size="small" text type="primary" @click="selectFromTable(row)">选择</el-button>
									<el-button size="small" text type="primary" @click="openEditChildDialog(row)">编辑</el-button>
									<el-button size="small" text type="danger" @click="deleteChild(row)">删除</el-button>
								</template>
							</el-table-column>
						</el-table>
						<el-empty v-else description="暂无子分类" />
					</el-card>

					<!-- 父分类 -->
					<el-card v-if="parent" shadow="hover" class="right-card">
						<template #header>
							<span>父分类</span>
						</template>
						<div class="parent-info">
							<el-tag>{{ parent.name }}</el-tag>
							<el-button type="primary" text @click="selectParentCategory">选择父分类</el-button>
						</div>
					</el-card>
				</div>
			</div>
		</div>

		<el-dialog v-model="state.dialog.visible" :title="state.dialog.mode === 'edit' ? '编辑分类' : '新增子分类'" width="520px">
			<el-form :model="state.dialog.form" label-width="90px">
				<el-form-item label="名称" required>
					<el-input v-model="state.dialog.form.name" placeholder="输入分类名称" clearable />
				</el-form-item>
				<el-form-item label="关联模板">
					<el-select v-model="state.dialog.form.specTemplateId" clearable placeholder="选择模板（可选）">
						<el-option v-for="tpl in state.templates" :key="tpl.id" :label="tpl.category" :value="tpl.id" />
					</el-select>
				</el-form-item>
				<el-form-item label="排序">
					<el-input-number v-model="state.dialog.form.sortOrder" :min="0" controls-position="right" />
				</el-form-item>
			</el-form>
			<template #footer>
				<el-button @click="state.dialog.visible = false">取消</el-button>
				<el-button type="primary" :loading="state.dialog.saving" @click="saveCategory">保存</el-button>
			</template>
		</el-dialog>
	</div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue';
import { useRouter } from 'vue-router';
import { ElMessage, ElMessageBox } from 'element-plus';
import AccessoryCategoryTree from '/@/views/accessoryManagement/components/AccessoryCategoryTree.vue';
import type { AccessoryCategory, AccessoryCategoryTreeItem, AccessoryParamDef, AccessorySpecTemplate } from '/@/api/accessory';
import {
	createAccessoryCategory,
	deleteAccessoryCategory,
	getAccessoryCategories,
	getAccessoryCategoryById,
	getAccessorySpecTemplates,
	updateAccessoryCategory,
} from '/@/api/accessory';

const router = useRouter();

const treeRef = ref<any>(null);

const state = reactive({
	selectedId: null as string | null,
	category: null as AccessoryCategory | null,
	allCategories: [] as AccessoryCategory[],
	templates: [] as AccessorySpecTemplate[],
	dialog: {
		visible: false,
		saving: false,
		mode: 'edit' as 'edit' | 'addChild',
		targetId: '' as string,
		parentId: null as string | null,
		form: {
			name: '',
			specTemplateId: '' as string | null,
			sortOrder: 0,
		},
	},
});

const parent = computed(() => {
	if (!state.category?.parentId) return null;
	return state.allCategories.find((c) => c.id === state.category!.parentId) || null;
});

const children = computed(() => {
	if (!state.category?.id) return [];
	return state.allCategories.filter((c) => c.parentId === state.category!.id);
});

const currentTemplate = computed(() => {
	if (!state.category?.specTemplateId) return null;
	return state.templates.find((t) => t.id === state.category!.specTemplateId) || null;
});

const templateParams = computed<AccessoryParamDef[]>(() => {
	if (currentTemplate.value?.paramDefs?.length) return currentTemplate.value.paramDefs;
	return (state.category?.specParams as AccessoryParamDef[]) || [];
});

const refreshCategories = async () => {
	try {
		const res = (await getAccessoryCategories()) as AccessoryCategory[];
		state.allCategories = Array.isArray(res) ? res : [];
	} catch (e: any) {
		state.allCategories = [];
		ElMessage.error(e?.message || '加载分类失败');
	}
};

const refreshTemplates = async () => {
	try {
		const res = (await getAccessorySpecTemplates()) as AccessorySpecTemplate[];
		state.templates = Array.isArray(res) ? res : [];
	} catch (e) {
		state.templates = [];
	}
};

const onCategorySelect = async (cat: AccessoryCategoryTreeItem) => {
	state.selectedId = cat.id;
	try {
		const detail = (await getAccessoryCategoryById(cat.id)) as AccessoryCategory;
		state.category = detail || (cat as any);
	} catch (e) {
		state.category = cat as any;
	}
};

const computePath = (name: string, parentId: string | null | undefined) => {
	if (!parentId) return name;
	const p = state.allCategories.find((c) => c.id === parentId) || parent.value;
	return p?.path ? `${p.path}/${name}` : name;
};

const openEditDialog = () => {
	if (!state.category) return;
	state.dialog.mode = 'edit';
	state.dialog.targetId = state.category.id;
	state.dialog.parentId = state.category.parentId || null;
	state.dialog.form = {
		name: state.category.name || '',
		specTemplateId: state.category.specTemplateId || null,
		sortOrder: state.category.sortOrder || 0,
	};
	state.dialog.visible = true;
};

const openAddChildDialog = () => {
	if (!state.category) return;
	state.dialog.mode = 'addChild';
	state.dialog.targetId = '';
	state.dialog.parentId = state.category.id;
	state.dialog.form = {
		name: '',
		specTemplateId: null,
		sortOrder: 0,
	};
	state.dialog.visible = true;
};

const openEditChildDialog = (row: AccessoryCategory) => {
	state.dialog.mode = 'edit';
	state.dialog.targetId = row.id;
	state.dialog.parentId = row.parentId || null;
	state.dialog.form = {
		name: row.name || '',
		specTemplateId: row.specTemplateId || null,
		sortOrder: row.sortOrder || 0,
	};
	state.dialog.visible = true;
};

const saveCategory = async () => {
	const name = state.dialog.form.name?.trim();
	if (!name) {
		ElMessage.warning('请输入分类名称');
		return;
	}
	state.dialog.saving = true;
	try {
		if (state.dialog.mode === 'addChild') {
			const payload: Partial<AccessoryCategory> = {
				name,
				parentId: state.dialog.parentId,
				specTemplateId: state.dialog.form.specTemplateId || null,
				sortOrder: state.dialog.form.sortOrder || 0,
				path: computePath(name, state.dialog.parentId),
			};
			await createAccessoryCategory(payload);
			ElMessage.success('子分类创建成功');
		} else {
			const payload: Partial<AccessoryCategory> = {
				name,
				parentId: state.dialog.parentId,
				specTemplateId: state.dialog.form.specTemplateId || null,
				sortOrder: state.dialog.form.sortOrder || 0,
				path: computePath(name, state.dialog.parentId),
			};
			await updateAccessoryCategory(state.dialog.targetId, payload);
			ElMessage.success('分类更新成功');
		}
		state.dialog.visible = false;
		await refreshCategories();
		await treeRef.value?.loadTree?.();
		if (state.selectedId) {
			const refreshed = state.allCategories.find((c) => c.id === state.selectedId) || null;
			if (refreshed) state.category = refreshed;
		}
	} catch (e: any) {
		ElMessage.error(e?.response?.data?.message || e?.message || '保存失败');
	} finally {
		state.dialog.saving = false;
	}
};

const deleteChild = async (row: AccessoryCategory) => {
	try {
		await ElMessageBox.confirm(`确定删除分类 "${row.name}" 吗？`, '警告', {
			type: 'warning',
			confirmButtonText: '删除',
			cancelButtonText: '取消',
		});
		await deleteAccessoryCategory(row.id);
		ElMessage.success('删除成功');
		await refreshCategories();
		await treeRef.value?.loadTree?.();
	} catch (e: any) {
		if (e !== 'cancel') ElMessage.error(e?.response?.data?.message || e?.message || '删除失败');
	}
};

const selectFromTable = async (row: AccessoryCategory) => {
	// 跳转到配件列表页面，并传递分类ID
	router.push({
		path: '/accessoryManagement/accessoryList',
		query: { categoryId: row.id }
	});
};

const selectParentCategory = async () => {
	if (!parent.value) return;
	// 跳转到配件列表页面，并传递父分类ID
	router.push({
		path: '/accessoryManagement/accessoryList',
		query: { categoryId: parent.value.id }
	});
};

onMounted(async () => {
	await Promise.all([refreshTemplates(), refreshCategories()]);
});
</script>

<style scoped lang="scss">
.accessory-classification-layout {
	display: flex;
	flex-direction: row;
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
	min-height: 0;
	overflow: hidden;
}

.accessory-right-scroll {
	flex: 1;
	min-height: 0;
	display: flex;
	flex-direction: column;
	gap: 12px;
	overflow-y: auto;
	overflow-x: hidden;
}

.right-card {
	flex: none;
	min-width: 0;
}

.child-card {
	flex: 1;
	min-height: 0;
	:deep(.el-card__body) {
		height: 100%;
		min-height: 0;
		overflow: auto;
		overflow-x: hidden;
	}
}

.card-header {
	display: flex;
	align-items: center;
	justify-content: space-between;
}

.muted {
	color: var(--el-text-color-secondary);
}

.parent-info {
	display: flex;
	align-items: center;
	gap: 12px;
}
</style>
