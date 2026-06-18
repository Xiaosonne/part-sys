<template>
	<div class="inventory-log-container layout-padding">
		<div class="inventory-log layout-padding-auto">
			<el-card shadow="hover" class="table-card">
				<template #header>
					<div class="card-header">
						<span class="table-title">库存事务记录</span>
					</div>
				</template>

				<el-table :data="state.transactions" stripe style="width: 100%" v-loading="state.loading">
					<el-table-column prop="type" label="类型" width="120" align="center">
						<template #default="{ row }">
							<el-tag :type="typeTagType(row.type)" size="small">{{ typeLabel(row.type) }}</el-tag>
						</template>
					</el-table-column>
					<el-table-column prop="partName" label="配件名称" min-width="200" show-overflow-tooltip>
						<template #default="{ row }">
							<div class="part-info">
								<div class="part-name">{{ row.partName }}</div>
								<div class="part-model text-muted">{{ row.partModel }}</div>
							</div>
						</template>
					</el-table-column>
					<el-table-column prop="quantity" label="数量" width="100" align="center">
						<template #default="{ row }">
							<span :class="row.quantity > 0 ? 'qty-positive' : 'qty-negative'">
								{{ row.quantity > 0 ? '+' : '' }}{{ row.quantity }}
							</span>
						</template>
					</el-table-column>
					<el-table-column prop="createdAt" label="时间" width="180">
						<template #default="{ row }">
							{{ formatDate(row.createdAt) }}
						</template>
					</el-table-column>
					<el-table-column prop="note" label="备注" min-width="150" show-overflow-tooltip>
						<template #default="{ row }">
							<span>{{ row.note || '-' }}</span>
						</template>
					</el-table-column>
				</el-table>

				<div class="pagination-wrapper" v-if="state.total > 0">
					<el-pagination
						v-model:current-page="state.pagination.pageNum"
						v-model:page-size="state.pagination.pageSize"
						:page-sizes="[20, 50, 100, 200]"
						:total="state.total"
						layout="total, sizes, prev, pager, next"
						background
						@size-change="onSizeChange"
						@current-change="onPageChange"
					/>
				</div>
			</el-card>
		</div>
	</div>
</template>

<script setup lang="ts">
import { onMounted, reactive } from 'vue';
import { ElMessage } from 'element-plus';
import { getTransactions, type StockTransaction } from '/@/api/inventory';

const state = reactive({
	loading: false,
	transactions: [] as StockTransaction[],
	total: 0,
	pagination: {
		pageNum: 1,
		pageSize: 20,
	},
});

const loadData = async () => {
	state.loading = true;
	try {
		const params = {
			page: state.pagination.pageNum,
			pageSize: state.pagination.pageSize,
		};
		const res = (await getTransactions(params)) as any;
		state.transactions = res?.items || [];
		state.total = res?.totalCount || 0;
	} catch (e: any) {
		ElMessage.error(e?.message || '加载事务记录失败');
	} finally {
		state.loading = false;
	}
};

const onSizeChange = (size: number) => {
	state.pagination.pageSize = size;
	state.pagination.pageNum = 1;
	loadData();
};

const onPageChange = (page: number) => {
	state.pagination.pageNum = page;
	loadData();
};

const typeLabel = (type: string) => {
	const map: any = { INBOUND: '入库', OUTBOUND: '出库', LOCK: '锁定', UNLOCK: '解锁', RETURN: '归还' };
	return map[type] || type;
};

const typeTagType = (type: string) => {
	const map: any = { INBOUND: 'success', OUTBOUND: 'warning', LOCK: 'danger', UNLOCK: 'info', RETURN: '' };
	return map[type] || '';
};

const formatDate = (dateStr: string) => {
	if (!dateStr) return '-';
	const d = new Date(dateStr);
	if (Number.isNaN(d.getTime())) return dateStr;
	return d.toLocaleString('zh-CN', {
		year: 'numeric',
		month: '2-digit',
		day: '2-digit',
		hour: '2-digit',
		minute: '2-digit',
	});
};

onMounted(() => {
	loadData();
});
</script>

<style scoped lang="scss">
.inventory-log-container {
	.inventory-log {
		display: flex;
		flex-direction: column;
		height: 100%;
	}

	.table-card {
		flex: 1;
		display: flex;
		flex-direction: column;
		min-height: 0;
		:deep(.el-card__body) {
			flex: 1;
			display: flex;
			flex-direction: column;
			min-height: 0;
			padding: 0;
		}
	}

	.card-header {
		display: flex;
		align-items: center;
		justify-content: space-between;
	}

	.table-title {
		font-size: 15px;
		font-weight: 600;
	}

	.part-info {
		display: flex;
		flex-direction: column;
	}

	.part-name {
		font-weight: 500;
		color: var(--el-text-color-primary);
	}

	.text-muted {
		font-size: 12px;
		color: var(--el-text-color-secondary);
		margin-top: 2px;
	}

	.qty-positive {
		color: var(--el-color-success);
		font-weight: 600;
	}

	.qty-negative {
		color: var(--el-color-danger);
		font-weight: 600;
	}

	.pagination-wrapper {
		padding: 12px 16px;
		display: flex;
		justify-content: flex-end;
		border-top: 1px solid var(--el-border-color-lighter);
	}
}
</style>
