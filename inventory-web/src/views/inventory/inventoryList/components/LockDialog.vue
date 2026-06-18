<template>
	<el-dialog v-model="visible" title="锁定" width="640px" class="inventory-action-dialog">
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

			<el-form-item label="锁定数量">
				<el-input-number v-model="state.form.quantity" :min="1" :max="state.form.maxQty" controls-position="right" />
			</el-form-item>

			<el-form-item label="关联项目">
				<el-select v-model="state.form.projectId" placeholder="选择项目（可选）" style="width: 100%" clearable>
					<el-option v-for="proj in state.projects" :key="proj.id" :label="proj.name" :value="proj.id" />
				</el-select>
			</el-form-item>

			<el-form-item label="备注">
				<el-input v-model="state.form.note" placeholder="可选" clearable />
			</el-form-item>
		</el-form>

		<template #footer>
			<el-button @click="visible = false">取消</el-button>
			<el-button type="primary" :loading="state.submitting" @click="submit">提交锁定</el-button>
		</template>
	</el-dialog>
</template>

<script setup lang="ts">
import { computed, reactive, watch } from 'vue';
import { ElMessage } from 'element-plus';
import type { PartItem, ProjectNode } from '/@/api/inventory';
import { getParts, getProjects, lockStock } from '/@/api/inventory';

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
	form: {
		partId: '',
		quantity: 1,
		maxQty: Number.POSITIVE_INFINITY,
		projectId: '' as string,
		note: '' as string,
	},
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

const submit = async () => {
	if (!state.form.partId) {
		ElMessage.warning('请选择配件');
		return;
	}
	state.submitting = true;
	try {
		await lockStock({
			partId: state.form.partId,
			quantity: state.form.quantity,
			projectId: state.form.projectId || null,
			note: state.form.note || null,
		});
		ElMessage.success('锁定成功');
		visible.value = false;
		state.form = { partId: '', quantity: 1, maxQty: Number.POSITIVE_INFINITY, projectId: '', note: '' };
		emit('success');
	} catch (e: any) {
		ElMessage.error(e?.response?.data?.message || e?.message || '锁定失败');
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
