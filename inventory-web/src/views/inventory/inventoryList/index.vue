<template>
	<div class="inventory-list-container layout-padding">
		<div class="inventory-list layout-padding-auto">
			<div class="stats-row">
				<el-card shadow="hover" class="stat-card">
					<div class="stat-inner">
						<div class="stat-icon total">
							<el-icon><ele-Box /></el-icon>
						</div>
						<div class="stat-main">
							<div class="stat-value">{{ stats.total }}</div>
							<div class="stat-label">总库存</div>
						</div>
					</div>
				</el-card>
				<el-card shadow="hover" class="stat-card">
					<div class="stat-inner">
						<div class="stat-icon locked">
							<el-icon><ele-Lock /></el-icon>
						</div>
						<div class="stat-main">
							<div class="stat-value">{{ stats.locked }}</div>
							<div class="stat-label">已锁定</div>
						</div>
					</div>
				</el-card>
				<el-card shadow="hover" class="stat-card">
					<div class="stat-inner">
						<div class="stat-icon available">
							<el-icon><ele-CircleCheck /></el-icon>
						</div>
						<div class="stat-main">
							<div class="stat-value">{{ stats.available }}</div>
							<div class="stat-label">可用库存</div>
						</div>
					</div>
				</el-card>
				<el-card shadow="hover" class="stat-card">
					<div class="stat-inner">
						<div class="stat-icon pending">
							<el-icon><ele-ShoppingCart /></el-icon>
						</div>
						<div class="stat-main">
							<div class="stat-value">{{ stats.pending }}</div>
							<div class="stat-label">待采购</div>
						</div>
					</div>
				</el-card>
			</div>

			<!-- <el-card shadow="hover" class="filter-card mt15">
				<div class="filter-bar">
					<el-input
						v-model="state.filters.keyword"
						placeholder="搜索配件名称、型号..."
						clearable
						style="width: 240px"
						@keyup.enter="applyFilters"
					>
						<template #prefix>
							<el-icon><ele-Search /></el-icon>
						</template>
					</el-input>
					<el-select v-model="state.filters.category" placeholder="分类筛选" clearable style="width: 200px">
						<el-option v-for="cat in categories" :key="cat" :label="cat" :value="cat" />
					</el-select>
					<el-button type="primary" @click="applyFilters">
						<el-icon><ele-Search /></el-icon>
						搜索
					</el-button>
					<el-button @click="resetFilters">重置</el-button>
					<div class="flex-fill"></div>
					<div class="action-buttons">
						<el-button type="primary" plain @click="dialogs.inbound = true">入库</el-button>
						<el-button type="primary" plain @click="dialogs.outbound = true">出库</el-button>
						<el-button type="primary" plain @click="dialogs.lock = true">锁定</el-button>
						<el-button type="primary" plain @click="dialogs.unlock = true">解锁</el-button>
					</div>
					<div class="table-count">共 {{ filteredParts.length }} 条</div>
				</div>
			</el-card> -->

			<el-card shadow="hover" class="table-card mt15">
				<template #header>
					<div class="card-header">
					<div class="filter-bar">
					<el-input
						v-model="state.filters.keyword"
						placeholder="搜索配件名称、型号..."
						clearable
						style="width: 240px"
						@keyup.enter="applyFilters"
					>
						<template #prefix>
							<el-icon><ele-Search /></el-icon>
						</template>
					</el-input>
					<el-select v-model="state.filters.category" placeholder="分类筛选" clearable style="width: 200px">
						<el-option v-for="cat in categories" :key="cat" :label="cat" :value="cat" />
					</el-select>
					<el-button type="primary" @click="applyFilters">
						<el-icon><ele-Search /></el-icon>
						搜索
					</el-button>
					<el-button @click="resetFilters">重置</el-button>
					<div class="flex-fill"></div>
					<div class="action-buttons">
						<el-button type="primary" plain @click="dialogs.inbound = true">入库</el-button>
						<el-button type="primary" plain @click="dialogs.outbound = true">出库</el-button>
						<el-button type="primary" plain @click="dialogs.lock = true">锁定</el-button>
						<el-button type="primary" plain @click="dialogs.unlock = true">解锁</el-button>
					</div>
				</div>
					</div>
				</template>

				<el-table
					:data="pagedParts"
					stripe
					style="width: 100%"
					v-loading="state.loading"
					row-key="partId"
					:expand-row-keys="state.expandedRows"
					@expand-change="onExpandChange"
					table-layout="fixed"
				>
					<el-table-column type="expand" width="44">
						<template #default="{ row }">
							<div class="lock-details">
								<div class="lock-details-title">锁定明细</div>
								<el-table
									v-if="row.loadingLocks || (row.locks && row.locks.length > 0)"
									:data="row.locks || []"
									stripe
									size="small"
									max-height="200"
									v-loading="row.loadingLocks"
									table-layout="fixed"
									style="width: 100%"
								>
									<el-table-column prop="projectName" label="项目" min-width="140" show-overflow-tooltip />
									<el-table-column prop="selectionPlanName" label="选型单" min-width="120" show-overflow-tooltip />
									<el-table-column prop="lockedQty" label="锁库数量" width="100" align="center" />
									<el-table-column prop="operatorName" label="操作人" width="100" show-overflow-tooltip />
									<el-table-column label="锁定时间" width="170">
										<template #default="{ row: lock }">
											{{ formatDate(lock.lockedAt) }}
										</template>
									</el-table-column>
								</el-table>
								<el-empty v-else description="无锁定明细" :image-size="60" />
							</div>
						</template>
					</el-table-column>

					<el-table-column prop="partName" label="配件名称" min-width="220" show-overflow-tooltip />
					<el-table-column prop="partModel" label="型号" width="160" show-overflow-tooltip />
					<el-table-column prop="category" label="分类" min-width="180" show-overflow-tooltip />
					<el-table-column prop="totalQty" label="总数" width="80" align="center" />
					<el-table-column prop="lockedQty" label="已锁定" width="80" align="center">
						<template #default="{ row }">
							<el-tag v-if="row.lockedQty > 0" type="warning" size="small">{{ row.lockedQty }}</el-tag>
							<span v-else>-</span>
						</template>
					</el-table-column>
					<el-table-column prop="availableQty" label="可用" width="80" align="center">
						<template #default="{ row }">
							<el-tag v-if="row.availableQty === 0" type="danger" size="small">缺货</el-tag>
							<span v-else>{{ row.availableQty }}</span>
						</template>
					</el-table-column>
					<el-table-column prop="pendingPurchaseQty" label="待采购" width="90" align="center">
						<template #default="{ row }">
							<el-tag v-if="row.pendingPurchaseQty > 0" type="danger" size="small">{{ row.pendingPurchaseQty }}</el-tag>
							<span v-else>-</span>
						</template>
					</el-table-column>
					<el-table-column label="更新时间" width="170">
						<template #default="{ row }">
							{{ formatDate(row.updatedAt) }}
						</template>
					</el-table-column>
				</el-table>

				<div class="pagination-wrapper" v-if="filteredParts.length > 0">
					<el-pagination
						v-model:current-page="state.pagination.pageNum"
						v-model:page-size="state.pagination.pageSize"
						:page-sizes="[20, 50, 100]"
						:total="filteredParts.length"
						layout="total, sizes, prev, pager, next"
						background
					/>
				</div>
			</el-card>

			<InboundDialog v-model="dialogs.inbound" @success="onActionSuccess" />
			<OutboundDialog v-model="dialogs.outbound" @success="onActionSuccess" />
			<LockDialog v-model="dialogs.lock" @success="onActionSuccess" />
			<UnlockDialog v-model="dialogs.unlock" @success="onActionSuccess" />
		</div>
	</div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive } from 'vue';
import { ElMessage } from 'element-plus';
import { getStockLocksByPartId, getStockOverview, type StockOverviewItem } from '/@/api/inventory';
import InboundDialog from '/@/views/inventory/inventoryList/components/InboundDialog.vue';
import OutboundDialog from '/@/views/inventory/inventoryList/components/OutboundDialog.vue';
import LockDialog from '/@/views/inventory/inventoryList/components/LockDialog.vue';
import UnlockDialog from '/@/views/inventory/inventoryList/components/UnlockDialog.vue';

type StockRow = StockOverviewItem & {
	expanded?: boolean;
	locks?: any[];
	loadingLocks?: boolean;
};

const state = reactive({
	loading: false,
	allParts: [] as StockRow[],
	expandedRows: [] as string[],
	filters: {
		keyword: '',
		category: '',
	},
	pagination: {
		pageNum: 1,
		pageSize: 20,
	},
});

const dialogs = reactive({
	inbound: false,
	outbound: false,
	lock: false,
	unlock: false,
});

const lockCache = new Map<string, any[]>();

const stats = computed(() => ({
	total: state.allParts.reduce((sum, p) => sum + (p.totalQty || 0), 0),
	locked: state.allParts.reduce((sum, p) => sum + (p.lockedQty || 0), 0),
	available: state.allParts.reduce((sum, p) => sum + (p.availableQty || 0), 0),
	pending: state.allParts.reduce((sum, p) => sum + (p.pendingPurchaseQty || 0), 0),
}));

const categories = computed(() => {
	const set = new Set<string>();
	state.allParts.forEach((p) => {
		if (p.category) set.add(p.category);
	});
	return Array.from(set).sort((a, b) => a.localeCompare(b, 'zh-Hans-CN'));
});

const filteredParts = computed(() => {
	const kw = state.filters.keyword?.trim().toLowerCase();
	return state.allParts.filter((p) => {
		const matchKeyword =
			!kw || String(p.partName || '').toLowerCase().includes(kw) || String(p.partModel || '').toLowerCase().includes(kw);
		const matchCategory = !state.filters.category || p.category === state.filters.category;
		return matchKeyword && matchCategory;
	});
});

const pagedParts = computed(() => {
	const start = (state.pagination.pageNum - 1) * state.pagination.pageSize;
	return filteredParts.value.slice(start, start + state.pagination.pageSize);
});

const formatDate = (dateStr?: string) => {
	if (!dateStr) return '-';
	const d = new Date(dateStr);
	if (Number.isNaN(d.getTime())) return String(dateStr);
	return d.toLocaleString('zh-CN', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' });
};

const loadData = async () => {
	state.loading = true;
	try {
		const res = (await getStockOverview()) as StockOverviewItem[];
		const list = Array.isArray(res) ? res : [];
		state.allParts = (Array.isArray(res) ? res : []).map((p) => ({
			...p,
			expanded: false,
			locks: lockCache.get(p.partId) || [],
			loadingLocks: false,
		}));
		if (list.length === 0) lockCache.clear();
		state.expandedRows = [];
		state.pagination.pageNum = 1;
	} catch (e: any) {
		ElMessage.error(e?.message || '加载库存总览失败');
		state.allParts = [];
		state.expandedRows = [];
	} finally {
		state.loading = false;
	}
};

const resetFilters = () => {
	state.filters.keyword = '';
	state.filters.category = '';
	applyFilters();
	state.pagination.pageNum = 1;
};

const applyFilters = () => {
	state.pagination.pageNum = 1;
	state.expandedRows = [];
};

const onExpandChange = async (row: StockRow, expandedRows: StockRow[]) => {
	const isExpanded = Array.isArray(expandedRows) && expandedRows.some((r: any) => r?.partId === row.partId);
	if (!isExpanded) {
		state.expandedRows = [];
		row.expanded = false;
		return;
	}

	state.expandedRows = [row.partId];
	row.expanded = true;

	const cached = lockCache.get(row.partId);
	if (cached && cached.length > 0) {
		row.locks = cached;
		return;
	}

	if (row.loadingLocks) return;
	row.loadingLocks = true;
	try {
		const res = (await getStockLocksByPartId(row.partId)) as any;
		row.locks = res?.locks || [];
		lockCache.set(row.partId, row.locks || []);
	} catch (e) {
		row.locks = [];
		lockCache.set(row.partId, []);
	} finally {
		row.loadingLocks = false;
	}
};

const onActionSuccess = async () => {
	await loadData();
};

onMounted(() => {
	loadData();
});
</script>

<style scoped lang="scss">
.inventory-list {
	display: flex;
	flex-direction: column;
	min-height: 0;
	overflow: hidden;
}

.stats-row {
	display: grid;
	grid-template-columns: repeat(4, minmax(0, 1fr));
	gap: 12px;
}

.stat-card {  
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
  border-radius: 8px;
	:deep(.el-card__body) {
		padding: 16px;
	}
  &:nth-child(1) {
    border-left: 4px solid #1677ff; 
  }
  &:nth-child(2) {
    border-left: 4px solid #fa8c16; 
  }
  &:nth-child(3) {
    border-left: 4px solid #52c41a; 
  }
  &:nth-child(4) {
    border-left: 4px solid #ff4d4f; 
  }
}

.stat-inner {
	display: flex;
	align-items: center;
	gap: 14px;
}

.stat-icon {
	width: 44px;
	height: 44px;
	border-radius: 10px;
	flex: none;
	background: var(--el-fill-color-light);
	display: flex;
	align-items: center;
	justify-content: center;
	font-size: 22px;
}

.stat-icon.total {
	background: #e6f4ff;
	color: #1677ff;
}
.stat-icon.locked {
	background: #fff7e6;
	color: #fa8c16;
}
.stat-icon.available {
	background: #f6ffed;
	color: #52c41a;
}
.stat-icon.pending {
	background: #fff1f0;
	color: #ff4d4f;
}

.stat-main {
	min-width: 0;
}

.stat-value {
	font-size: 24px;
	font-weight: 700;
	line-height: 28px;
	color: var(--el-text-color-primary);
}

.stat-label {
	margin-top: 4px;
	font-size: 13px;
	color: var(--el-text-color-secondary);
}

.filter-bar {
	display: flex;
	align-items: center;
	gap: 12px;
	flex-wrap: wrap;
}

.flex-fill {
	flex: 1;
}

.action-buttons {
	display: inline-flex;
	align-items: center;
	gap: 8px;
	white-space: nowrap;
}

.table-count {
	color: var(--el-text-color-secondary);
	font-size: 13px;
	white-space: nowrap;
}

.table-card {
	flex: 1;
	min-height: 0;
	display: flex;
	flex-direction: column;
	:deep(.el-card__body) {
		flex: 1;
		min-height: 0;
		overflow: hidden;
	}
	:deep(.el-table__body-wrapper),
	:deep(.el-scrollbar__wrap) {
		overflow-x: hidden;
	}
}

.lock-details {
	padding: 12px 20px;
	background: var(--el-bg-color-page);
}

.lock-details-title {
	font-size: 13px;
	font-weight: 600;
	color: var(--el-text-color-secondary);
	margin-bottom: 10px;
}

.pagination-wrapper {
	padding: 12px 0 0;
	display: flex;
	justify-content: flex-end;
}
</style>
