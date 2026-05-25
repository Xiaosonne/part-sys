<template>
	<el-dialog v-model="visible" title="出库" width="720px" class="inventory-action-dialog">
		<el-form :model="state.form" label-width="100px">
			<el-form-item label="配件" required>
				<el-select v-model="state.form.partId" placeholder="选择配件" style="width: 100%" @change="onPartChange">
					<el-option
						v-for="part in state.parts"
						:key="part.id"
						:label="`${part.name} - ${part.model} (可用: ${part.availableQty})`"
						:value="part.id"
					/>
				</el-select>
			</el-form-item>

			<el-form-item label="出库数量">
				<el-input-number v-model="state.form.quantity" :min="1" :max="state.form.maxQty" controls-position="right" />
			</el-form-item>

			<el-form-item label="关联项目" required>
				<el-select v-model="state.form.projectId" placeholder="选择项目" style="width: 100%" @change="onProjectChange">
					<el-option v-for="proj in state.projects" :key="proj.id" :label="proj.name" :value="proj.id" />
				</el-select>
			</el-form-item>

			<el-form-item v-if="state.form.projectId" label="关联选型">
				<el-select v-model="state.form.selectionPlanId" placeholder="选择选型单（可选）" style="width: 100%" clearable>
					<el-option v-for="plan in selectionPlans" :key="plan.id" :label="plan.name" :value="plan.id" />
				</el-select>
			</el-form-item>

			<el-form-item label="用途">
				<el-select v-model="state.form.usage" placeholder="选择用途" style="width: 300px" clearable>
					<el-option label="生产使用" value="生产使用" />
					<el-option label="测试" value="测试" />
					<el-option label="维修" value="维修" />
					<el-option label="其他" value="其他" />
				</el-select>
			</el-form-item>

			<el-form-item label="备注">
				<el-input v-model="state.form.note" placeholder="可选" clearable />
			</el-form-item>
		</el-form>

		<template #footer>
			<el-button @click="visible = false">取消</el-button>
			<el-button type="primary" :loading="state.submitting" @click="submit">提交出库</el-button>
		</template>
	</el-dialog>
</template>

<script setup lang="ts">
import { computed, reactive, watch } from 'vue';
import { ElMessage } from 'element-plus';
import type { PartItem, ProjectNode, SelectionPlan } from '/@/api/inventory';
import { getParts, getProjects, getSelections, outboundStock } from '/@/api/inventory';

const props = defineProps<{ modelValue: boolean }>();
const emit = defineEmits<{ (e: 'update:modelValue', val: boolean): void; (e: 'success'): void }>();

const visible = computed({
	get: () => props.modelValue,
	set: (val: boolean) => emit('update:modelValue', val),
});

const state = reactive({
	submitting: false,
	parts: [] as PartItem[],
	projects: [] as Array<{ id: string; name: string }>,
	selectionPlansByProject: {} as Record<string, SelectionPlan[]>,
	form: {
		partId: '',
		quantity: 1,
		maxQty: Number.POSITIVE_INFINITY,
		projectId: '',
		selectionPlanId: '' as string | null,
		usage: '' as string,
		note: '' as string,
	},
});

const selectionPlans = computed(() => {
	if (!state.form.projectId) return [];
	return state.selectionPlansByProject[state.form.projectId] || [];
});

const loadParts = async () => {
	try {
		const res = (await getParts()) as PartItem[];
		state.parts = Array.isArray(res) ? res : [];
	} catch (e) {
		state.parts = [];
	}
};

const loadProjects = async () => {
	try {
		const res = (await getProjects()) as ProjectNode[];
		const nodes = Array.isArray(res) ? res : [];
		state.projects = nodes.filter((n) => !n.type || n.type === 'project').map((n) => ({ id: n.id, name: n.name }));
	} catch (e) {
		state.projects = [];
	}
};

const onPartChange = (partId: string) => {
	const part = state.parts.find((p) => p.id === partId);
	state.form.maxQty = part?.availableQty ?? 0;
	state.form.quantity = 1;
};

const onProjectChange = async () => {
	state.form.selectionPlanId = '';
	const pid = state.form.projectId;
	if (!pid || state.selectionPlansByProject[pid]) return;
	try {
		const res = (await getSelections(pid)) as SelectionPlan[];
		const list = Array.isArray(res) ? res : [];
		state.selectionPlansByProject[pid] = list.filter((p: any) => p.status === 'Submitted' || p.status === 1);
	} catch (e) {
		state.selectionPlansByProject[pid] = [];
	}
};

const submit = async () => {
	if (!state.form.partId) {
		ElMessage.warning('请选择配件');
		return;
	}
	if (!state.form.projectId) {
		ElMessage.warning('请选择关联项目');
		return;
	}
	state.submitting = true;
	try {
		await outboundStock({
			partId: state.form.partId,
			quantity: state.form.quantity,
			projectId: state.form.projectId,
			selectionPlanId: state.form.selectionPlanId || null,
			usage: state.form.usage || null,
			note: state.form.note || null,
		});
		ElMessage.success('出库成功');
		visible.value = false;
		state.form = { partId: '', quantity: 1, maxQty: Number.POSITIVE_INFINITY, projectId: '', selectionPlanId: '', usage: '', note: '' };
		emit('success');
	} catch (e: any) {
		ElMessage.error(e?.response?.data?.message || e?.message || '出库失败');
	} finally {
		state.submitting = false;
	}
};

watch(
	() => props.modelValue,
	async (val) => {
		if (!val) return;
		await Promise.all([loadParts(), loadProjects()]);
	}
);
</script>

<style scoped lang="scss">
.inventory-action-dialog :deep(.el-form) {
	background: var(--el-bg-color);
	border: 1px solid var(--el-border-color-lighter);
	border-radius: 8px;
	padding: 16px 18px;
}
</style>
