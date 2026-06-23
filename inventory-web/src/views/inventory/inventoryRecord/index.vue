<template>
	<div class="inventory-record-container layout-padding">
		<div class="inventory-record layout-padding-auto">
			<!-- 筛选栏 -->


			<!-- 列表卡片 -->
			<el-card shadow="hover" class="table-card mt15">
				<template #header>
					<div class="filter-bar">
					<el-date-picker
						v-model="state.filters.dateRange"
						type="daterange"
						range-separator="至"
						start-placeholder="开始日期"
						end-placeholder="结束日期"
						value-format="YYYY-MM-DD"
						style="width: 200px"
						@change="loadData"
					/>
					<el-select v-model="state.filters.type" placeholder="类型" clearable style="width: 300px" @change="loadData">
						<el-option label="入库" value="INBOUND" />
						<el-option label="出库" value="OUTBOUND" />
						<el-option label="锁定" value="LOCK" />
						<el-option label="解锁" value="UNLOCK" />
						<el-option label="归还" value="RETURN" />
					</el-select>
					<el-select v-model="state.filters.sourceType" placeholder="来源" clearable style="width: 300px" @change="loadData">
						<el-option label="手动" :value="0" />
						<el-option label="采购入库" :value="1" />
						<el-option label="选型锁定" :value="2" />
						<el-option label="选型解锁" :value="3" />
						<el-option label="选型出库" :value="4" />
					</el-select>
					<el-input
						v-model="state.filters.keyword"
						placeholder="配件名称搜索"
						clearable
						style="width: 300px"
						@keyup.enter="loadData"
					>
						<template #prefix>
							<el-icon><ele-Search /></el-icon>
						</template>
					</el-input>
					<el-button type="primary" @click="loadData" :loading="state.loading">
						<el-icon><ele-Search /></el-icon>
						搜索
					</el-button>
					<el-button @click="resetFilters">
						<el-icon><ele-Refresh /></el-icon>
						重置
					</el-button>
				</div>
				</template>

				<el-table :data="state.transactions" stripe style="width: 100%" v-loading="state.loading">
					<el-table-column label="时间" width="160">
						<template #default="{ row }">
							{{ formatDate(row.createdAt) }}
						</template>
					</el-table-column>
					<el-table-column prop="partName" label="配件" min-width="200">
						<template #default="{ row }">
							<div class="part-name">{{ row.partName }}</div>
							<div class="part-model">{{ row.partModel }}</div>
						</template>
					</el-table-column>
					<el-table-column label="类型" width="160" align="center">
						<template #default="{ row }">
							<div class="type-tags">
								<el-tag :type="typeTagType(row.type)" size="small" class="type-tag">{{ typeLabel(row.type) }}</el-tag>
								<!-- <el-tag size="small" :class="'source-tag ' + sourceTypeClass(row.sourceType)">{{ row.sourceTypeName }}</el-tag> -->
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
					<el-table-column prop="operatorName" label="操作人" width="100" show-overflow-tooltip />
					<el-table-column prop="projectName" label="关联项目" min-width="160" show-overflow-tooltip>
						<template #default="{ row }">
							<span v-if="row.projectName">{{ row.projectName }}</span>
							<span v-else class="text-muted">-</span>
						</template>
					</el-table-column>
					<el-table-column prop="selectionPlanName" label="选型单" min-width="140" show-overflow-tooltip>
						<template #default="{ row }">
							<span v-if="row.selectionPlanName">{{ row.selectionPlanName }}</span>
							<span v-else class="text-muted">-</span>
						</template>
					</el-table-column>
					<el-table-column prop="usage" label="用途" width="100" show-overflow-tooltip>
						<template #default="{ row }">
							<span v-if="row.usage">{{ row.usage }}</span>
							<span v-else class="text-muted">-</span>
						</template>
					</el-table-column>
					<el-table-column prop="note" label="备注" min-width="180" show-overflow-tooltip>
						<template #default="{ row }">
							<span v-if="row.note">{{ row.note }}</span>
							<span v-else class="text-muted">-</span>
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
	filters: {
		dateRange: [] as string[],
		type: '',
		sourceType: '' as string | number,
		keyword: '',
	},
	pagination: {
		pageNum: 1,
		pageSize: 20,
	},
});

const loadData = async () => {
	state.loading = true;
	try {
		const params: any = {
			page: state.pagination.pageNum,
			pageSize: state.pagination.pageSize,
		};
		if (state.filters.dateRange && state.filters.dateRange.length === 2) {
			params.startDate = state.filters.dateRange[0] + 'T00:00:00';
			params.endDate = state.filters.dateRange[1] + 'T23:59:59';
		}
		if (state.filters.type) params.type = state.filters.type;
		if (state.filters.sourceType !== '') params.sourceType = state.filters.sourceType;
		if (state.filters.keyword) params.keyword = state.filters.keyword;

		const res = (await getTransactions(params)) as any;
		state.transactions = res?.items || [];
		state.total = res?.totalCount || 0;
	} catch (e: any) {
		ElMessage.error(e?.message || '加载流水记录失败');
	} finally {
		state.loading = false;
	}
};

const resetFilters = () => {
	state.filters.dateRange = [];
	state.filters.type = '';
	state.filters.sourceType = '';
	state.filters.keyword = '';
	state.pagination.pageNum = 1;
	loadData();
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

const sourceTypeClass = (sourceType: number) => {
	const map: any = { 0: 'source-manual', 1: 'source-purchase', 2: 'source-sel-lock', 3: 'source-sel-unlock', 4: 'source-sel-out' };
	return map[sourceType] || '';
};

const formatDate = (dateStr: string) => {
	if (!dateStr) return '-';
	const d = new Date(dateStr);
	if (Number.isNaN(d.getTime())) return dateStr;
	return d.toLocaleString('zh-CN', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' });
};

onMounted(() => {
	loadData();
});
</script>

<style scoped lang="scss">
.inventory-record-container {
	.inventory-record {
		display: flex;
		flex-direction: column;
		height: 100%;
    background-color: #fff;
	}

	.filter-card {
		:deep(.el-card__body) {
			padding: 12px 16px;
		}
	}

	.filter-bar {
		display: flex;
		align-items: center;
		gap: 12px;
		flex-wrap: wrap;
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

	.table-count {
		font-size: 13px;
		color: var(--el-text-color-secondary);
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

	.part-name {
		color: var(--el-text-color-primary);
		font-weight: 500;
	}

	.part-model {
		color: var(--el-text-color-secondary);
		font-size: 12px;
		margin-top: 2px;
	}

	.type-tags {
		display: flex;
		align-items: center;
		justify-content: center;
		gap: 6px;
	}

	.type-tag {
		font-weight: 600;
	}

	.source-tag {
		border-radius: 4px;
		font-weight: 500;
	}

	.source-manual { background: #f5f5f5; color: #666666; border: 1px solid #d9d9d9; }
	.source-purchase { background: #e6f4ff; color: #1677ff; border: 1px solid #91caff; }
	.source-sel-lock { background: #fff1f0; color: #ff4d4f; border: 1px solid #ffa39e; }
	.source-sel-unlock { background: #f6ffed; color: #52c41a; border: 1px solid #b7eb8f; }
	.source-sel-out { background: #fff7e6; color: #fa8c16; border: 1px solid #ffd591; }

	.qty-positive {
		color: var(--el-color-success);
		font-weight: 600;
		font-size: 14px;
	}

	.qty-negative {
		color: var(--el-color-danger);
		font-weight: 600;
		font-size: 14px;
	}

	.text-muted {
		color: var(--el-text-color-disabled);
	}

	.pagination-wrapper {
		padding: 12px 16px;
		display: flex;
		justify-content: flex-end;
		border-top: 1px solid var(--el-border-color-lighter);
	}
}
</style>
