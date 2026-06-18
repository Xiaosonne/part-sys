<template>
	<div class="lock-list-container layout-padding">
		<div class="lock-list layout-padding-auto">
			<!-- 统计卡片 -->
			<div class="stats-row">
				<el-card shadow="hover" class="stat-card">
					<div class="stat-inner">
						<div class="stat-icon locked">
							<el-icon><ele-Lock /></el-icon>
						</div>
						<div class="stat-main">
							<div class="stat-value">{{ stats.totalLocked }}</div>
							<div class="stat-label">当前锁定总数</div>
						</div>
					</div>
				</el-card>
				<el-card shadow="hover" class="stat-card">
					<div class="stat-inner">
						<div class="stat-icon part">
							<el-icon><ele-Box /></el-icon>
						</div>
						<div class="stat-main">
							<div class="stat-value">{{ stats.partCount }}</div>
							<div class="stat-label">涉及配件</div>
						</div>
					</div>
				</el-card>
				<el-card shadow="hover" class="stat-card">
					<div class="stat-inner">
						<div class="stat-icon project">
							<el-icon><ele-FolderOpened /></el-icon>
						</div>
						<div class="stat-main">
							<div class="stat-value">{{ stats.projectCount }}</div>
							<div class="stat-label">涉及项目</div>
						</div>
					</div>
				</el-card>
			</div>


			<!-- 列表卡片 -->
			<el-card shadow="hover" class="table-card mt15">
				<template #header>
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
					<el-select v-model="state.filters.projectId" placeholder="项目筛选" clearable style="width: 200px" @change="applyFilters">
						<el-option v-for="proj in state.projects" :key="proj.id" :label="proj.name" :value="proj.id" />
					</el-select>
					<el-button type="primary" @click="applyFilters">
						<el-icon><ele-Search /></el-icon>
						搜索
					</el-button>
					<el-button @click="resetFilters">
						<el-icon><ele-Refresh /></el-icon>
						重置
					</el-button>
				</div>
				</template>

				<el-table
					:data="pagedLocks"
					stripe
					style="width: 100%"
					v-loading="state.loading"
					row-key="partId"
					:expand-row-keys="state.expandedRows"
					@expand-change="onExpandChange"
				>
					<el-table-column type="expand" width="44">
						<template #default="{ row }">
							<div class="lock-details">
								<div class="lock-details-title">锁定明细</div>
								<el-table :data="row.locks || []" stripe size="small" border style="width: 100%">
									<el-table-column prop="projectName" label="项目" min-width="150" show-overflow-tooltip />
									<el-table-column prop="selectionPlanName" label="选型单" min-width="120" show-overflow-tooltip>
										<template #default="{ row: lock }">
											{{ lock.selectionPlanName || '-' }}
										</template>
									</el-table-column>
									<el-table-column prop="lockedQty" label="锁库数量" width="100" align="center" />
									<el-table-column prop="operatorName" label="操作人" width="100" show-overflow-tooltip />
									<el-table-column label="锁定时间" width="170">
										<template #default="{ row: lock }">
											{{ formatDate(lock.lockedAt) }}
										</template>
									</el-table-column>
									<el-table-column label="操作" width="100" fixed="right" align="center">
										<template #default="{ row: lock }">
											<el-button size="small" type="danger" plain @click="handleUnlock(lock, row)">解锁</el-button>
										</template>
									</el-table-column>
								</el-table>
							</div>
						</template>
					</el-table-column>

					<el-table-column prop="partName" label="配件名称" min-width="200" show-overflow-tooltip />
					<el-table-column prop="partModel" label="型号" width="160" show-overflow-tooltip />
					<el-table-column prop="category" label="分类" width="160" show-overflow-tooltip />
					<el-table-column prop="totalLocked" label="锁定总数" width="100" align="center">
						<template #default="{ row }">
							<el-tag type="warning" size="small" effect="plain">{{ row.totalLocked }}</el-tag>
						</template>
					</el-table-column>
					<el-table-column label="锁定来源" min-width="320">
						<template #default="{ row }">
							<div class="lock-sources">
								<div v-for="lock in (row.locks || []).slice(0, 2)" :key="lock.transactionId" class="lock-source-item">
									<el-tag size="small" class="project-tag" effect="light">{{ lock.projectName || '未知项目' }}</el-tag>
									<el-tag v-if="lock.selectionPlanName" size="small" type="success" class="plan-tag" effect="plain">{{ lock.selectionPlanName }}</el-tag>
									<span class="qty-tag">x{{ lock.lockedQty }}</span>
								</div>
								<div v-if="(row.locks || []).length > 2" class="more-hint">
									<el-link type="info" :underline="false" style="font-size: 12px" @click="toggleRow(row)">
										+{{ row.locks.length - 2 }} 更多...
									</el-link>
								</div>
							</div>
						</template>
					</el-table-column>
				</el-table>

				<div class="pagination-wrapper" v-if="filteredLocks.length > 0">
					<el-pagination
						v-model:current-page="state.pagination.pageNum"
						v-model:page-size="state.pagination.pageSize"
						:page-sizes="[10, 20, 50, 100]"
						:total="filteredLocks.length"
						layout="total, sizes, prev, pager, next"
						background
					/>
				</div>
			</el-card>

			<!-- 解锁确认弹窗 -->
			<el-dialog v-model="state.unlockDialog.visible" title="确认解锁" width="450px">
				<div v-if="state.unlockDialog.lock">
					<p class="mb15">确定解锁以下锁定内容吗？</p>
					<el-descriptions :column="1" border size="small">
						<el-descriptions-item label="配件名称">{{ state.unlockDialog.part?.partName }}</el-descriptions-item>
						<el-descriptions-item label="关联项目">{{ state.unlockDialog.lock.projectName || '-' }}</el-descriptions-item>
						<el-descriptions-item label="选型单据">{{ state.unlockDialog.lock.selectionPlanName || '-' }}</el-descriptions-item>
						<el-descriptions-item label="锁定数量">{{ state.unlockDialog.lock.lockedQty }}</el-descriptions-item>
					</el-descriptions>
					<el-form class="mt20" label-width="80px">
						<el-form-item label="解锁数量">
							<el-input-number v-model="state.unlockDialog.quantity" :min="1" :max="state.unlockDialog.lock.lockedQty" />
						</el-form-item>
						<el-form-item label="解锁备注">
							<el-input v-model="state.unlockDialog.note" type="textarea" placeholder="请输入解锁原因或备注" />
						</el-form-item>
					</el-form>
				</div>
				<template #footer>
					<el-button @click="state.unlockDialog.visible = false">取消</el-button>
					<el-button type="primary" @click="confirmUnlock" :loading="state.unlockDialog.loading">确认解锁</el-button>
				</template>
			</el-dialog>
		</div>
	</div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { getStockLocks, getProjects, unlockStock, type StockLockSummary, type StockLockDetail, type ProjectNode } from '/@/api/inventory';

const state = reactive({
	loading: false,
	allLocks: [] as StockLockSummary[],
	projects: [] as ProjectNode[],
	expandedRows: [] as string[],
	filters: {
		keyword: '',
		projectId: '',
	},
	pagination: {
		pageNum: 1,
		pageSize: 20,
	},
	unlockDialog: {
		visible: false,
		loading: false,
		lock: null as StockLockDetail | null,
		part: null as StockLockSummary | null,
		quantity: 1,
		note: '',
	},
});

const stats = computed(() => {
	const totalLocked = state.allLocks.reduce((sum, p) => sum + (p.totalLocked || 0), 0);
	const partCount = state.allLocks.length;
	const projectSet = new Set<string>();
	state.allLocks.forEach((p) => {
		(p.locks || []).forEach((l) => {
			if (l.projectId) projectSet.add(l.projectId);
		});
	});
	return {
		totalLocked,
		partCount,
		projectCount: projectSet.size,
	};
});

const filteredLocks = computed(() => {
	const kw = state.filters.keyword?.trim().toLowerCase();
	return state.allLocks.filter((p) => {
		const matchKeyword = !kw || p.partName.toLowerCase().includes(kw) || p.partModel.toLowerCase().includes(kw);
		const matchProject = !state.filters.projectId || (p.locks || []).some((l) => l.projectId === state.filters.projectId);
		return matchKeyword && matchProject;
	});
});

const pagedLocks = computed(() => {
	const start = (state.pagination.pageNum - 1) * state.pagination.pageSize;
	return filteredLocks.value.slice(start, start + state.pagination.pageSize);
});

const loadData = async () => {
	state.loading = true;
	try {
		const res = (await getStockLocks()) as any;
		state.allLocks = res || [];
	} catch (e: any) {
		ElMessage.error(e?.message || '加载锁定列表失败');
	} finally {
		state.loading = false;
	}
};

const loadProjects = async () => {
	try {
		const res = (await getProjects()) as any;
		state.projects = res || [];
	} catch (e) {
		console.error('加载项目失败', e);
	}
};

const applyFilters = () => {
	state.pagination.pageNum = 1;
	state.expandedRows = [];
};

const resetFilters = () => {
	state.filters.keyword = '';
	state.filters.projectId = '';
	applyFilters();
};

const onExpandChange = (row: StockLockSummary, expandedRows: StockLockSummary[]) => {
	const isExpanded = Array.isArray(expandedRows) && expandedRows.some((r: any) => r?.partId === row.partId);
	if (isExpanded) {
		state.expandedRows = [row.partId];
	} else {
		state.expandedRows = [];
	}
};

const toggleRow = (row: StockLockSummary) => {
	if (state.expandedRows.includes(row.partId)) {
		state.expandedRows = [];
	} else {
		state.expandedRows = [row.partId];
	}
};

const handleUnlock = (lock: StockLockDetail, part: StockLockSummary) => {
	state.unlockDialog.lock = lock;
	state.unlockDialog.part = part;
	state.unlockDialog.quantity = lock.lockedQty;
	state.unlockDialog.note = '';
	state.unlockDialog.visible = true;
};

const confirmUnlock = async () => {
	if (!state.unlockDialog.lock || !state.unlockDialog.part) return;
	state.unlockDialog.loading = true;
	try {
		await unlockStock({
			partId: state.unlockDialog.part.partId,
			quantity: state.unlockDialog.quantity,
			projectId: state.unlockDialog.lock.projectId,
			note: state.unlockDialog.note,
		});
		ElMessage.success('解锁成功');
		state.unlockDialog.visible = false;
		await loadData();
	} catch (e: any) {
		ElMessage.error(e?.message || '解锁失败');
	} finally {
		state.unlockDialog.loading = false;
	}
};

const formatDate = (dateStr?: string) => {
	if (!dateStr) return '-';
	const d = new Date(dateStr);
	if (Number.isNaN(d.getTime())) return String(dateStr);
	return d.toLocaleString('zh-CN', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' });
};

onMounted(() => {
	loadData();
	loadProjects();
});
</script>

<style scoped lang="scss">
.lock-list {
	display: flex;
	flex-direction: column;
	min-height: 0;
	overflow: hidden;
}

.stats-row {
	display: grid;
	grid-template-columns: repeat(3, minmax(0, 1fr));
	gap: 15px;
}

.stat-card {
	border-radius: 8px;
	:deep(.el-card__body) {
		padding: 20px;
	}
	&:nth-child(1) { border-left: 4px solid #fa8c16;background: #fff7e6; }
	&:nth-child(2) { border-left: 4px solid #1677ff;background: #e6f4ff; }
	&:nth-child(3) { border-left: 4px solid #52c41a;background: #f6ffed; }
}

.stat-inner {
	display: flex;
	align-items: center;
	gap: 16px;
}

.stat-icon {
	width: 52px;
	height: 52px;
	border-radius: 10px;
	display: flex;
	align-items: center;
	justify-content: center;
	font-size: 26px;
}

.stat-icon.locked { background: #fff7e6; color: #fa8c16; }
.stat-icon.part { background: #e6f4ff; color: #1677ff; }
.stat-icon.project { background: #f6ffed; color: #52c41a; }

.stat-main { flex: 1; }
.stat-value { font-size: 28px; font-weight: 700; line-height: 32px; color: var(--el-text-color-primary); }
.stat-label { margin-top: 4px; font-size: 14px; color: var(--el-text-color-secondary); }

.filter-bar {
	display: flex;
	align-items: center;
	gap: 12px;
	flex-wrap: wrap;
}

.table-card {
	flex: 1;
	min-height: 0;
	display: flex;
	flex-direction: column;
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

.table-title { font-size: 15px; font-weight: 600; }
.table-count { color: var(--el-text-color-secondary); font-size: 13px; }

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

.lock-sources {
	display: flex;
	flex-direction: column;
	gap: 6px;
	padding: 4px 0;
}

.lock-source-item {
	display: flex;
	align-items: center;
	gap: 8px;
	flex-wrap: wrap;
}

.project-tag { max-width: 180px; overflow: hidden; text-overflow: ellipsis; }
.plan-tag { max-width: 150px; overflow: hidden; text-overflow: ellipsis; }
.qty-tag { color: #fa8c16; font-weight: 600; font-size: 13px; }
.more-hint { margin-top: 2px; }

.pagination-wrapper {
	padding: 12px 16px;
	display: flex;
	justify-content: flex-end;
	border-top: 1px solid var(--el-border-color-lighter);
}
</style>
