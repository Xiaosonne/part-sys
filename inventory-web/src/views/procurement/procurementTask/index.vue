<template>
	<div class="procurement-task-container layout-padding">
		<div class="procurement-task layout-padding-auto">
			<!-- 状态筛选栏 -->
			<el-card shadow="hover" class="filter-card">
				<div class="filter-row">
					<span class="filter-label">状态筛选:</span>
					<el-radio-group v-model="state.statusFilter" size="small" @change="onStatusChange">
						<el-radio-button label="">全部</el-radio-button>
						<el-radio-button label="Pending">待采购</el-radio-button>
						<el-radio-button label="InProgress">采购中</el-radio-button>
						<el-radio-button label="Received">已到货</el-radio-button>
						<el-radio-button label="Cancelled">已取消</el-radio-button>
					</el-radio-group>
				</div>
			</el-card>

			<!-- 任务列表 -->
			<el-card shadow="hover" class="table-card mt15">
				<el-table :data="filteredTasks" stripe v-loading="state.loading" style="width: 100%" class="custom-table">
					<el-table-column prop="partName" label="配件名称" min-width="200" show-overflow-tooltip>
						<template #default="{ row }">
							<div class="part-name-cell">
								<el-icon class="mr5"><ele-Memo /></el-icon>
								<span>{{ row.partName }}</span>
							</div>
						</template>
					</el-table-column>
					<el-table-column prop="projectName" label="项目" min-width="150" show-overflow-tooltip>
						<template #default="{ row }">
							{{ row.projectName || '-' }}
						</template>
					</el-table-column>
					<el-table-column prop="selectionPlanName" label="选型单" min-width="150" show-overflow-tooltip>
						<template #default="{ row }">
							{{ row.selectionPlanName || row.selectionPlanId || '-' }}
						</template>
					</el-table-column>
					<el-table-column prop="requiredQty" label="需采购数量" width="120" align="center">
						<template #default="{ row }">
							<span class="qty-pending">{{ row.requiredQty }}</span>
						</template>
					</el-table-column>
					<el-table-column prop="status" label="状态" width="100" align="center">
						<template #default="{ row }">
							<el-tag size="small" :type="statusType(row.status)" effect="light" round>
								{{ statusText(row.status) }}
							</el-tag>
						</template>
					</el-table-column>
					<el-table-column prop="createdByName" label="创建人" width="100" align="center" />
					<el-table-column prop="createdAt" label="创建时间" width="180" align="center">
						<template #default="{ row }">
							{{ formatDate(row.createdAt) }}
						</template>
					</el-table-column>
					<el-table-column prop="remark" label="备注" min-width="150" show-overflow-tooltip>
						<template #default="{ row }">
							{{ row.remark || '-' }}
						</template>
					</el-table-column>
					<el-table-column label="操作" width="260" fixed="right" align="center">
						<template #default="{ row }">
							<div class="operation-column">
								<el-button
									size="small"
									type="primary"
									link
									@click="onStartTask(row)"
									:disabled="row.status !== 'Pending'"
								>
									<el-icon><ele-Pointer /></el-icon>
									开始采购
								</el-button>
								<el-button
									size="small"
									type="success"
									link
									@click="onReceiveTask(row)"
									:disabled="row.status !== 'InProgress'"
								>
									<el-icon><ele-CircleCheck /></el-icon>
									确认到货
								</el-button>
								<el-button
									size="small"
									type="danger"
									link
									@click="onCancelTask(row)"
									:disabled="row.status === 'Received' || row.status === 'Cancelled'"
								>
									<el-icon><ele-CircleClose /></el-icon>
									取消
								</el-button>
							</div>
						</template>
					</el-table-column>
				</el-table>
				<el-empty v-if="!state.loading && filteredTasks.length === 0" description="暂无采购任务" :image-size="100" />
			</el-card>
		</div>
	</div>
</template>

<script setup lang="ts">
import { reactive, onMounted, computed } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { getPurchaseTasks, startPurchaseTask, receivePurchaseTask, cancelPurchaseTask } from '/@/api/purchase';

// 定义状态
const state = reactive({
	loading: false,
	tasks: [] as any[],
	statusFilter: '',
});

// 计算属性：过滤后的任务
const filteredTasks = computed(() => {
	if (!state.statusFilter) return state.tasks;
	return state.tasks.filter((t) => t.status === state.statusFilter);
});

// 状态类型映射
const statusType = (status: string) => {
	const map: any = {
		Pending: 'warning',
		InProgress: 'primary',
		Received: 'success',
		Cancelled: 'info',
	};
	return map[status] || 'info';
};

// 状态文本映射
const statusText = (status: string) => {
	const map: any = {
		Pending: '待采购',
		InProgress: '采购中',
		Received: '已到货',
		Cancelled: '已取消',
	};
	return map[status] || status;
};

// 格式化日期
const formatDate = (d: string) => (d ? new Date(d).toLocaleString('zh-CN') : '-');

// 加载数据
const loadData = async () => {
	state.loading = true;
	try {
		const res = (await getPurchaseTasks()) as any;
		state.tasks = res || [];
	} catch (e: any) {
		ElMessage.error(e?.message || '加载采购任务失败');
	} finally {
		state.loading = false;
	}
};

// 状态筛选切换
const onStatusChange = () => {
	// 已经在 computed 中处理了，这里可以留空或做其他逻辑
};

// 开始采购
const onStartTask = async (task: any) => {
	try {
		await ElMessageBox.confirm(`确认开始采购配件 "${task.partName}" x${task.requiredQty} 吗？`, '操作确认', {
			type: 'info',
			confirmButtonText: '确定',
			cancelButtonText: '取消',
		});
		await startPurchaseTask(task.id);
		ElMessage.success('已开始采购');
		loadData();
	} catch (e: any) {
		if (e !== 'cancel') {
			ElMessage.error(e?.message || '操作失败');
		}
	}
};

// 确认到货
const onReceiveTask = async (task: any) => {
	try {
		await ElMessageBox.confirm(`确认配件 "${task.partName}" x${task.requiredQty} 已到货并办理入库吗？`, '确认到货', {
			type: 'success',
			confirmButtonText: '确认到货',
			cancelButtonText: '取消',
		});
		await receivePurchaseTask(task.id);
		ElMessage.success('已标记为已到货并办理入库');
		loadData();
	} catch (e: any) {
		if (e !== 'cancel') {
			ElMessage.error(e?.message || '操作失败');
		}
	}
};

// 取消任务
const onCancelTask = async (task: any) => {
	try {
		await ElMessageBox.confirm(`确认取消采购任务 "${task.partName}" 吗？`, '取消确认', {
			type: 'warning',
			confirmButtonText: '确定取消',
			cancelButtonText: '取消',
		});
		await cancelPurchaseTask(task.id);
		ElMessage.success('任务已取消');
		loadData();
	} catch (e: any) {
		if (e !== 'cancel') {
			ElMessage.error(e?.message || '操作失败');
		}
	}
};

// 页面加载
onMounted(() => {
	loadData();
});
</script>

<style scoped lang="scss">
.procurement-task-container {
	height: 100%;
}
.filter-card {
	:deep(.el-card__body) {
		padding: 12px 20px;
	}
}
.filter-row {
	display: flex;
	align-items: center;
	gap: 16px;
}
.filter-label {
	font-size: 14px;
	font-weight: 600;
	color: var(--el-text-color-primary);
}
.table-card {
	flex: 1;
	display: flex;
	flex-direction: column;
	overflow: hidden;
	:deep(.el-card__body) {
		flex: 1;
		display: flex;
		flex-direction: column;
		overflow: hidden;
	}
}
.custom-table {
	border-radius: 8px;
	border: 1px solid var(--el-border-color-lighter);
	:deep(.el-table__header-wrapper) th {
		background-color: var(--el-fill-color-light);
		color: var(--el-text-color-primary);
		font-weight: 600;
	}
}
.part-name-cell {
	display: flex;
	align-items: center;
	color: var(--el-text-color-primary);
	font-weight: 500;
}
.operation-column {
	display: flex;
	align-items: center;
	justify-content: center;
	white-space: nowrap;
	gap: 0px;
}
.qty-pending {
	color: var(--el-color-warning);
	font-weight: 700;
	font-size: 15px;
}
.mr5 {
	margin-right: 5px;
}
.mt15 {
	margin-top: 15px;
}
</style>
