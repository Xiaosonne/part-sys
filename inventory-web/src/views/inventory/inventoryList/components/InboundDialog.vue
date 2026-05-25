<template>
	<el-dialog v-model="visible" title="入库" width="720px" class="inventory-action-dialog">
		<el-radio-group v-model="state.inboundType" class="type-switch">
			<el-radio-button label="normal">普通入库</el-radio-button>
			<el-radio-button label="purchase">采购入库</el-radio-button>
			<el-radio-button label="return">归还入库</el-radio-button>
		</el-radio-group>

		<div v-if="state.inboundType === 'normal'" class="form-content">
			<el-form :model="state.normalForm" label-width="100px">
				<el-form-item label="配件" required>
					<el-select v-model="state.normalForm.partId" placeholder="选择配件" style="width: 100%" @change="onNormalPartChange">
						<el-option
							v-for="part in state.parts"
							:key="part.id"
							:label="`${part.name} - ${part.model} (可用: ${part.availableQty})`"
							:value="part.id"
						/>
					</el-select>
				</el-form-item>
				<el-form-item label="数量">
					<el-input-number v-model="state.normalForm.quantity" :min="1" controls-position="right" />
				</el-form-item>
				<el-form-item label="备注">
					<el-input v-model="state.normalForm.note" placeholder="可选" clearable />
				</el-form-item>
				<el-form-item>
					<el-button type="primary" :loading="state.submitting" @click="submitNormal">提交入库</el-button>
				</el-form-item>
			</el-form>
		</div>

		<div v-if="state.inboundType === 'purchase'" class="form-content">
			<el-form :model="state.purchaseForm" label-width="100px">
				<el-form-item label="采购任务" required>
					<el-select v-model="state.purchaseForm.taskId" placeholder="选择采购任务" style="width: 100%" @change="onTaskChange">
						<el-option
							v-for="task in purchaseTasks"
							:key="task.id"
							:label="task.name"
							:value="task.id"
						/>
					</el-select>
				</el-form-item>
			</el-form>

			<div v-if="state.purchaseForm.taskId && selectedTaskPart" class="purchase-items">
				<div class="items-header">配件明细</div>
				<el-descriptions :column="2" border>
					<el-descriptions-item label="配件名称">{{ selectedTaskPart.partName }}</el-descriptions-item>
					<el-descriptions-item label="型号">{{ selectedTaskPart.partModel }}</el-descriptions-item>
					<el-descriptions-item label="需采购数量">{{ selectedTaskPart.requiredQty }}</el-descriptions-item>
					<el-descriptions-item label="原始锁定">{{ selectedTaskPart.lockedQty }}</el-descriptions-item>
				</el-descriptions>
			</div>

			<div class="form-actions">
				<el-button type="primary" :loading="state.submitting" :disabled="!state.purchaseForm.taskId" @click="submitPurchase">
					确认入库
				</el-button>
			</div>
		</div>

		<div v-if="state.inboundType === 'return'" class="form-content">
			<el-form :model="state.returnForm" label-width="100px">
				<el-form-item label="关联项目" required>
					<el-select v-model="state.returnForm.projectId" placeholder="选择项目" style="width: 100%" @change="onReturnProjectChange">
						<el-option v-for="proj in projects" :key="proj.id" :label="proj.name" :value="proj.id" />
					</el-select>
				</el-form-item>
			</el-form>

			<div v-if="state.returnForm.projectId && state.returnLocks.length > 0" class="return-locks">
				<div class="locks-header">该项目的锁定配件</div>
				<el-table
					:data="state.returnLocks"
					border
					stripe
					max-height="300"
					table-layout="fixed"
					@selection-change="onReturnSelectionChange"
				>
					<el-table-column type="selection" width="50" />
					<el-table-column prop="partName" label="配件名称" min-width="160" show-overflow-tooltip />
					<el-table-column prop="partModel" label="型号" width="140" show-overflow-tooltip />
					<el-table-column prop="selectionPlanName" label="选型单" min-width="140" show-overflow-tooltip />
					<el-table-column prop="lockedQty" label="已锁定" width="90" align="center">
						<template #default="{ row }">
							<el-tag type="warning" size="small">{{ row.lockedQty }}</el-tag>
						</template>
					</el-table-column>
					<el-table-column label="归还数量" width="140">
						<template #default="{ row }">
							<el-input-number v-model="row.returnQty" :min="1" :max="row.lockedQty" size="small" controls-position="right" />
						</template>
					</el-table-column>
				</el-table>

				<div class="return-actions">
					<div class="return-tip">选择要归还的配件，设置归还数量后提交</div>
					<el-button type="primary" :loading="state.submitting" :disabled="state.selectedReturnItems.length === 0" @click="submitReturn">
						提交归还 ({{ state.selectedReturnItems.length }} 项)
					</el-button>
				</div>
			</div>

			<el-empty v-else-if="state.returnForm.projectId && state.returnLocks.length === 0" description="该项目暂无锁定配件" />

			<el-form v-if="state.returnForm.projectId" :model="state.returnForm" label-width="100px" class="mt15">
				<el-form-item label="备注">
					<el-input v-model="state.returnForm.note" placeholder="可选" clearable />
				</el-form-item>
			</el-form>
		</div>

		<template #footer>
			<el-button @click="visible = false">关闭</el-button>
		</template>
	</el-dialog>
</template>

<script setup lang="ts">
import { computed, reactive, watch } from 'vue';
import { ElMessage } from 'element-plus';
import type { PartItem, ProjectNode, PurchaseTask, StockLockSummary } from '/@/api/inventory';
import { getParts, getProjects, getPurchaseTasks, getStockLocks, inboundStock, receivePurchaseTask, returnStock } from '/@/api/inventory';

const props = defineProps<{ modelValue: boolean }>();
const emit = defineEmits<{ (e: 'update:modelValue', val: boolean): void; (e: 'success'): void }>();

const visible = computed({
	get: () => props.modelValue,
	set: (val: boolean) => emit('update:modelValue', val),
});

const state = reactive({
	submitting: false,
	inboundType: 'normal' as 'normal' | 'purchase' | 'return',
	parts: [] as PartItem[],
	projects: [] as Array<{ id: string; name: string }>,
	purchaseTasks: [] as Array<PurchaseTask & { name: string }>,
	normalForm: {
		partId: '',
		quantity: 1,
		note: '',
	},
	purchaseForm: {
		taskId: '',
	},
	returnForm: {
		projectId: '',
		note: '',
	},
	returnLocks: [] as Array<any>,
	selectedReturnItems: [] as Array<any>,
});

const projects = computed(() => state.projects);

const purchaseTasks = computed(() => state.purchaseTasks);

const selectedTaskPart = computed(() => {
	const task = state.purchaseTasks.find((t) => t.id === state.purchaseForm.taskId);
	if (!task) return null;
	return {
		partName: task.partName,
		partModel: task.partModel || '-',
		requiredQty: task.requiredQty,
		lockedQty: task.lockedQty,
	};
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
		state.projects = nodes
			.filter((n) => !n.type || n.type === 'project')
			.map((n) => ({ id: n.id, name: n.name }));
	} catch (e) {
		state.projects = [];
	}
};

const loadPurchaseTasks = async () => {
	try {
		const res = (await getPurchaseTasks()) as PurchaseTask[];
		const list = Array.isArray(res) ? res : [];
		state.purchaseTasks = list
			.filter((t) => t.status === 'Pending' || t.status === 'InProgress')
			.map((t) => ({ ...t, name: `采购: ${t.partName} (${t.requiredQty})` }));
	} catch (e) {
		state.purchaseTasks = [];
	}
};

const onNormalPartChange = () => {
	state.normalForm.quantity = 1;
};

const onTaskChange = () => {};

const onReturnProjectChange = async () => {
	state.returnLocks = [];
	state.selectedReturnItems = [];
	if (!state.returnForm.projectId) return;
	try {
		const res = (await getStockLocks()) as StockLockSummary[];
		const allLocks = Array.isArray(res) ? res : [];
		const projectLocks: any[] = [];
		allLocks.forEach((part) => {
			(part.locks || []).forEach((lock) => {
				if (lock.projectId === state.returnForm.projectId) {
					projectLocks.push({
						partId: part.partId,
						partName: part.partName,
						partModel: part.partModel,
						selectionPlanId: lock.selectionPlanId,
						selectionPlanName: lock.selectionPlanName,
						lockedQty: lock.lockedQty,
						returnQty: 1,
					});
				}
			});
		});
		state.returnLocks = projectLocks;
	} catch (e) {
		state.returnLocks = [];
	}
};

const onReturnSelectionChange = (rows: any[]) => {
	state.selectedReturnItems = rows || [];
};

const submitNormal = async () => {
	if (!state.normalForm.partId) {
		ElMessage.warning('请选择配件');
		return;
	}
	state.submitting = true;
	try {
		await inboundStock({
			partId: state.normalForm.partId,
			quantity: state.normalForm.quantity,
			note: state.normalForm.note || null,
		});
		ElMessage.success('入库成功');
		state.normalForm = { partId: '', quantity: 1, note: '' };
		emit('success');
	} catch (e: any) {
		ElMessage.error(e?.response?.data?.message || e?.message || '入库失败');
	} finally {
		state.submitting = false;
	}
};

const submitPurchase = async () => {
	if (!state.purchaseForm.taskId) {
		ElMessage.warning('请选择采购任务');
		return;
	}
	state.submitting = true;
	try {
		await receivePurchaseTask(state.purchaseForm.taskId);
		ElMessage.success('采购入库成功');
		state.purchaseForm.taskId = '';
		await loadPurchaseTasks();
		emit('success');
	} catch (e: any) {
		ElMessage.error(e?.response?.data?.message || e?.message || '采购入库失败');
	} finally {
		state.submitting = false;
	}
};

const submitReturn = async () => {
	if (!state.returnForm.projectId) {
		ElMessage.warning('请选择关联项目');
		return;
	}
	if (state.selectedReturnItems.length === 0) {
		ElMessage.warning('请选择要归还的配件');
		return;
	}
	state.submitting = true;
	try {
		for (const item of state.selectedReturnItems) {
			await returnStock({
				partId: item.partId,
				quantity: item.returnQty,
				projectId: state.returnForm.projectId,
				note: state.returnForm.note || null,
			});
		}
		ElMessage.success('归还入库成功');
		state.returnForm.note = '';
		state.selectedReturnItems = [];
		await onReturnProjectChange();
		emit('success');
	} catch (e: any) {
		ElMessage.error(e?.response?.data?.message || e?.message || '归还入库失败');
	} finally {
		state.submitting = false;
	}
};

watch(
	() => props.modelValue,
	async (val) => {
		if (!val) return;
		await Promise.all([loadParts(), loadProjects(), loadPurchaseTasks()]);
	}
);
</script>

<style scoped lang="scss">
.type-switch {
	margin-bottom: 16px;
}

.form-content :deep(.el-form) {
	background: var(--el-bg-color);
	border: 1px solid var(--el-border-color-lighter);
	border-radius: 8px;
	padding: 16px 18px;
}

.purchase-items {
	margin-top: 12px;
}

.items-header {
	font-weight: 600;
	margin-bottom: 8px;
}

.form-actions {
	margin-top: 12px;
	display: flex;
	justify-content: flex-end;
}

.return-locks {
	margin-top: 12px;
}

.locks-header {
	font-weight: 600;
	margin-bottom: 8px;
}

.return-actions {
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 12px;
	margin-top: 12px;
}

.return-tip {
	color: var(--el-text-color-secondary);
	font-size: 12px;
}

.inventory-action-dialog :deep(.el-table__body-wrapper),
.inventory-action-dialog :deep(.el-scrollbar__wrap) {
	overflow-x: hidden;
}
</style>
