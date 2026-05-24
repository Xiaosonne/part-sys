<template>
	<div class="spec-template-container layout-padding">
		<el-card shadow="hover" class="layout-padding-auto spec-template-card">
			<template #header>
				<div class="card-header">
					<span>规格模板</span>
					<el-button type="primary" size="default" @click="openCreate">
						<el-icon><ele-Plus /></el-icon>
						添加模板
					</el-button>
				</div>
			</template>

			<el-table :data="state.templates" v-loading="state.loading" stripe style="width: 100%" table-layout="fixed">
				<el-table-column prop="category" label="分类" min-width="180" show-overflow-tooltip />
				<el-table-column label="参数数量" width="100" align="center">
					<template #default="{ row }">
						{{ row.paramDefs?.length || 0 }}
					</template>
				</el-table-column>
				<el-table-column label="操作" width="160">
					<template #default="{ row }">
						<el-button size="small" text type="primary" @click="openEdit(row)">编辑</el-button>
						<el-button size="small" text type="danger" @click="onDelete(row)">删除</el-button>
					</template>
				</el-table-column>
			</el-table>
		</el-card>

		<el-dialog v-model="state.dialog.visible" class="spec-template-dialog" :title="state.dialog.editingId ? '编辑模板' : '添加模板'" width="720px">
			<el-form :model="state.dialog.form" label-width="90px">
				<el-form-item label="分类" required>
					<el-input v-model="state.dialog.form.category" placeholder="例如：电机、轴承" clearable />
				</el-form-item>
				<el-divider />
				<div class="params-header">
					<div class="params-title">参数定义</div>
					<el-button type="primary" size="small" @click="openParamCreate">
						<el-icon><ele-Plus /></el-icon>
						添加参数
					</el-button>
				</div>

				<div v-if="state.dialog.form.paramDefs.length > 0" class="params-list">
					<div v-for="(param, idx) in state.dialog.form.paramDefs" :key="`${param.key}-${idx}`" class="param-item">
						<div class="param-top">
							<div class="param-label">{{ param.label || '未命名参数' }}</div>
							<div class="param-actions">
								<el-button type="primary" text size="small" @click="openParamEdit(idx)">编辑</el-button>
								<el-button type="danger" text size="small" @click="removeParam(idx)">删除</el-button>
							</div>
						</div>
						<div class="param-meta">
							<span>Key: {{ param.key || '-' }}</span>
							<span class="ml10">类型: {{ param.dataType || '-' }}</span>
							<span v-if="param.unit" class="ml10">单位: {{ param.unit }}</span>
							<span v-if="param.required" class="ml10 required-flag">必填</span>
						</div>
					</div>
				</div>
				<el-empty v-else description="暂无参数定义" />
			</el-form>
			<template #footer>
				<el-button @click="closeTemplateDialog">取消</el-button>
				<el-button type="primary" :loading="state.dialog.saving" @click="saveTemplate">保存</el-button>
			</template>
		</el-dialog>

		<el-dialog v-model="state.paramDialog.visible" class="spec-param-dialog" :title="state.paramDialog.editingIndex !== null ? '编辑参数' : '添加参数'" width="520px">
			<el-form :model="state.paramDialog.form" label-width="90px">
				<el-form-item label="Key" required>
					<el-input v-model="state.paramDialog.form.key" placeholder="例如：voltage" clearable />
				</el-form-item>
				<el-form-item label="标签" required>
					<el-input v-model="state.paramDialog.form.label" placeholder="例如：电压" clearable />
				</el-form-item>
				<el-form-item label="单位">
					<el-input v-model="state.paramDialog.form.unit" placeholder="例如：V、kW、rpm" clearable />
				</el-form-item>
				<el-form-item label="数据类型" required>
					<el-select v-model="state.paramDialog.form.dataType" style="width: 100%">
						<el-option label="字符串" value="string" />
						<el-option label="数字" value="number" />
						<el-option label="布尔" value="boolean" />
						<el-option label="下拉选择" value="select" />
					</el-select>
				</el-form-item>
				<el-form-item v-if="state.paramDialog.form.dataType === 'select'" label="选项" required>
					<el-input
						v-model="state.paramDialog.form.optionsText"
						type="textarea"
						:rows="3"
						placeholder="每行一个选项，例如：&#10;选项1&#10;选项2&#10;选项3"
					/>
				</el-form-item>
				<el-form-item label="必填">
					<el-checkbox v-model="state.paramDialog.form.required" />
				</el-form-item>
			</el-form>
			<template #footer>
				<el-button @click="state.paramDialog.visible = false">取消</el-button>
				<el-button type="primary" @click="saveParam">保存</el-button>
			</template>
		</el-dialog>
	</div>
</template>

<script setup lang="ts">
import { onMounted, reactive } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import type { AccessoryParamDef, AccessorySpecTemplate } from '/@/api/accessory';
import { createAccessorySpecTemplate, deleteAccessorySpecTemplate, getAccessorySpecTemplates, updateAccessorySpecTemplate } from '/@/api/accessory';

type EditableParamDef = AccessoryParamDef & { optionsText?: string };

const state = reactive({
	loading: false,
	templates: [] as AccessorySpecTemplate[],
	dialog: {
		visible: false,
		saving: false,
		editingId: null as string | null,
		form: {
			category: '',
			paramDefs: [] as EditableParamDef[],
		},
	},
	paramDialog: {
		visible: false,
		editingIndex: null as number | null,
		form: {
			key: '',
			label: '',
			unit: '',
			dataType: 'string',
			optionsText: '',
			required: false,
		},
	},
});

const loadTemplates = async () => {
	state.loading = true;
	try {
		const res = (await getAccessorySpecTemplates()) as AccessorySpecTemplate[];
		state.templates = Array.isArray(res) ? res : [];
	} catch (e: any) {
		state.templates = [];
		ElMessage.error(e?.message || '加载模板失败');
	} finally {
		state.loading = false;
	}
};

const openCreate = () => {
	state.dialog.editingId = null;
	state.dialog.form = {
		category: '',
		paramDefs: [],
	};
	state.dialog.visible = true;
};

const openEdit = (row: AccessorySpecTemplate) => {
	state.dialog.editingId = row.id;
	state.dialog.form = {
		category: row.category,
		paramDefs: (row.paramDefs || []).map((p) => ({
			...p,
			optionsText: p.options?.length ? p.options.join('\n') : '',
		})),
	};
	state.dialog.visible = true;
};

const closeTemplateDialog = () => {
	state.dialog.visible = false;
};

const openParamCreate = () => {
	state.paramDialog.editingIndex = null;
	state.paramDialog.form = {
		key: '',
		label: '',
		unit: '',
		dataType: 'string',
		optionsText: '',
		required: false,
	};
	state.paramDialog.visible = true;
};

const openParamEdit = (idx: number) => {
	state.paramDialog.editingIndex = idx;
	const p = state.dialog.form.paramDefs[idx];
	state.paramDialog.form = {
		key: p?.key || '',
		label: p?.label || '',
		unit: p?.unit || '',
		dataType: p?.dataType || 'string',
		optionsText: p?.options?.length ? p.options.join('\n') : p?.optionsText || '',
		required: !!p?.required,
	};
	state.paramDialog.visible = true;
};

const saveParam = () => {
	const key = state.paramDialog.form.key?.trim();
	const label = state.paramDialog.form.label?.trim();
	if (!key || !label) {
		ElMessage.warning('请输入 Key 和标签');
		return;
	}
	if (state.paramDialog.form.dataType === 'select' && !state.paramDialog.form.optionsText?.trim()) {
		ElMessage.warning('下拉选择类型需要填写选项');
		return;
	}

	const options = state.paramDialog.form.optionsText
		? state.paramDialog.form.optionsText
				.split('\n')
				.map((o) => o.trim())
				.filter((o) => o)
		: [];

	const param: EditableParamDef = {
		key,
		label,
		unit: state.paramDialog.form.unit || '',
		dataType: state.paramDialog.form.dataType,
		options,
		optionsText: state.paramDialog.form.optionsText || '',
		required: !!state.paramDialog.form.required,
	};

	if (state.paramDialog.editingIndex !== null) {
		state.dialog.form.paramDefs[state.paramDialog.editingIndex] = param;
	} else {
		state.dialog.form.paramDefs.push(param);
	}

	state.paramDialog.visible = false;
	state.paramDialog.editingIndex = null;
};

const removeParam = (idx: number) => {
	state.dialog.form.paramDefs.splice(idx, 1);
};

const saveTemplate = async () => {
	const category = state.dialog.form.category?.trim();
	if (!category) {
		ElMessage.warning('请输入分类名称');
		return;
	}

	const payload: Partial<AccessorySpecTemplate> = {
		category,
		paramDefs: state.dialog.form.paramDefs.map((p) => ({
			key: p.key,
			label: p.label,
			unit: p.unit || '',
			dataType: p.dataType,
			options: p.optionsText
				? p.optionsText
						.split('\n')
						.map((o) => o.trim())
						.filter((o) => o)
				: p.options || [],
			required: !!p.required,
		})),
	};

	state.dialog.saving = true;
	try {
		if (state.dialog.editingId) {
			await updateAccessorySpecTemplate(state.dialog.editingId, payload);
			ElMessage.success('模板更新成功');
		} else {
			await createAccessorySpecTemplate(payload);
			ElMessage.success('模板创建成功');
		}
		state.dialog.visible = false;
		state.dialog.editingId = null;
		await loadTemplates();
	} catch (e: any) {
		ElMessage.error(e?.response?.data?.message || e?.message || '保存失败');
	} finally {
		state.dialog.saving = false;
	}
};

const onDelete = async (row: AccessorySpecTemplate) => {
	try {
		await ElMessageBox.confirm(`确定删除模板 "${row.category}" 吗？`, '警告', {
			type: 'warning',
			confirmButtonText: '删除',
			cancelButtonText: '取消',
		});
		await deleteAccessorySpecTemplate(row.id);
		ElMessage.success('删除成功');
		await loadTemplates();
	} catch (e: any) {
		if (e !== 'cancel') ElMessage.error(e?.response?.data?.message || e?.message || '删除失败');
	}
};

onMounted(() => {
	loadTemplates();
});
</script>

<style scoped lang="scss">
.spec-template-card {
	display: flex;
	flex-direction: column;
	:deep(.el-card__body) {
		flex: 1;
		min-height: 0;
		overflow: hidden;
	}
	:deep(.el-table__body-wrapper) {
		overflow-x: hidden;
	}
	:deep(.el-scrollbar__wrap) {
		overflow-x: hidden;
	}
}

.spec-template-dialog :deep(.el-dialog__body),
.spec-param-dialog :deep(.el-dialog__body) {
	padding-top: 10px !important;
}

.card-header {
	display: flex;
	align-items: center;
	justify-content: space-between;
}

.params-header {
	display: flex;
	align-items: center;
	justify-content: space-between;
	margin-bottom: 10px;
}

.params-title {
	font-weight: 600;
}

.params-list {
	max-height: 320px;
	overflow-y: auto;
	overflow-x: hidden;
	padding: 8px;
	border: 1px solid var(--el-border-color-lighter);
	border-radius: 6px;
	background: var(--el-bg-color-page);
}

.param-item {
	padding: 12px;
	background: var(--el-bg-color);
	border: 1px solid var(--el-border-color-lighter);
	border-radius: 8px;
	& + & {
		margin-top: 10px;
	}
}

.param-top {
	display: flex;
	align-items: flex-start;
	justify-content: space-between;
	gap: 10px;
	margin-bottom: 8px;
}

.param-label {
	font-weight: 600;
	font-size: 14px;
	min-width: 0;
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.param-actions {
	flex: none;
	display: flex;
	gap: 6px;
}

.param-meta {
	color: var(--el-text-color-secondary);
	font-size: 12px;
	line-height: 18px;
	span {
		display: inline-block;
	}
}

.required-flag {
	color: var(--el-color-danger);
}
</style>
