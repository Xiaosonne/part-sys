<template>
	<el-dialog v-model="visible" title="解锁" width="640px" class="inventory-action-dialog">
		<el-form :model="state.form" label-width="100px">
			<el-form-item label="配件" required>
				<el-select v-model="state.form.partId" placeholder="选择配件" style="width: 100%" @change="onPartChange">
					<el-option
						v-for="part in lockedParts"
						:key="part.id"
						:label="`${part.name} - ${part.model} (已锁定: ${part.lockedQty})`"
						:value="part.id"
					/>
				</el-select>
			</el-form-item>

			<el-form-item label="解锁数量">
				<el-input-number v-model="state.form.quantity" :min="1" :max="state.form.maxQty" controls-position="right" />
			</el-form-item>

			<el-form-item label="备注">
				<el-input v-model="state.form.note" placeholder="可选" clearable />
			</el-form-item>
		</el-form>

		<template #footer>
			<el-button @click="visible = false">取消</el-button>
			<el-button type="primary" :loading="state.submitting" @click="submit">提交解锁</el-button>
		</template>
	</el-dialog>
</template>

<script setup lang="ts">
import { computed, reactive, watch } from 'vue';
import { ElMessage } from 'element-plus';
import type { PartItem } from '/@/api/inventory';
import { getParts, unlockStock } from '/@/api/inventory';

const props = defineProps<{ modelValue: boolean }>();
const emit = defineEmits<{ (e: 'update:modelValue', val: boolean): void; (e: 'success'): void }>();

const visible = computed({
	get: () => props.modelValue,
	set: (val: boolean) => emit('update:modelValue', val),
});

const state = reactive({
	submitting: false,
	parts: [] as PartItem[],
	form: {
		partId: '',
		quantity: 1,
		maxQty: Number.POSITIVE_INFINITY,
		note: '' as string,
	},
});

const lockedParts = computed(() => state.parts.filter((p) => (p.lockedQty || 0) > 0));

const loadParts = async () => {
	try {
		const res = (await getParts()) as PartItem[];
		state.parts = Array.isArray(res) ? res : [];
	} catch (e) {
		state.parts = [];
	}
};

const onPartChange = (partId: string) => {
	const part = state.parts.find((p) => p.id === partId);
	state.form.maxQty = part?.lockedQty ?? 0;
	state.form.quantity = 1;
};

const submit = async () => {
	if (!state.form.partId) {
		ElMessage.warning('请选择配件');
		return;
	}
	state.submitting = true;
	try {
		await unlockStock({
			partId: state.form.partId,
			quantity: state.form.quantity,
			note: state.form.note || null,
		});
		ElMessage.success('解锁成功');
		visible.value = false;
		state.form = { partId: '', quantity: 1, maxQty: Number.POSITIVE_INFINITY, note: '' };
		emit('success');
	} catch (e: any) {
		ElMessage.error(e?.response?.data?.message || e?.message || '解锁失败');
	} finally {
		state.submitting = false;
	}
};

watch(
	() => props.modelValue,
	async (val) => {
		if (!val) return;
		await loadParts();
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
