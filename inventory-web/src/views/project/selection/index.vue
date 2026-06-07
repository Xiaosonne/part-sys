<template>
	<div class="selection-container layout-padding">
		<div class="selection-layout">
			<!-- 左侧项目列表 -->
			<div class="layout-side">
				<ProjectSide ref="projectSideRef" :show-selections="true" @node-click="onProjectClick" />
			</div>

			<!-- 右侧内容区 -->
			<div class="layout-main">
				<!-- 未选择状态 -->
				<el-empty v-if="!state.selectedNode && !state.currentPlan" description="请从左侧选择一个项目或选型单" :image-size="100" />

				<!-- 项目选型列表视图 -->
				<div v-else-if="state.selectedNode && state.selectedNode.type === 'project' && !state.currentPlan" class="project-view">
					<el-card shadow="hover" class="section-card">
						<template #header>
							<div class="card-header">
								<div class="header-left">
									<div class="header-icon-box">
										<el-icon><ele-Memo /></el-icon>
									</div>
									<span class="ml10 title-text">{{ state.selectedNode.name }}</span>
									<span class="ml10 subtitle">选型单列表</span>
								</div>
								<el-button type="primary" size="small" @click="onOpenAddPlan">
									<el-icon><ele-Plus /></el-icon>
									新建选型单
								</el-button>
							</div>
						</template>

						<el-table :data="state.projectSelections" stripe v-loading="state.loading" class="custom-table">
							<el-table-column prop="name" label="选型单名称" min-width="200">
								<template #default="{ row }">
									<div class="name-cell">
										<el-icon class="mr10"><ele-DocumentCopy /></el-icon>
										<span>{{ row.name }}</span>
									</div>
								</template>
							</el-table-column>
							<el-table-column label="状态" width="120" align="center">
								<template #default="{ row }">
									<el-tag :type="statusType(row.status)" effect="light" round size="small">
										{{ statusText(row.status) }}
									</el-tag>
								</template>
							</el-table-column>
							<el-table-column label="创建时间" width="180">
								<template #default="{ row }">
									{{ formatDate(row.createdAt) }}
								</template>
							</el-table-column>
							<el-table-column label="配件项" width="100" align="center">
								<template #default="{ row }">
									<el-badge :value="row.items?.length || 0" :type="row.items?.length ? 'primary' : 'info'" class="item-badge" />
								</template>
							</el-table-column>
							<el-table-column label="操作" width="180" align="center" fixed="right">
								<template #default="{ row }">
									<el-button size="small" type="primary" link @click="openPlan(row)">查看详情</el-button>
									<el-button size="small" type="danger" link @click="deletePlan(row)">删除</el-button>
								</template>
							</el-table-column>
						</el-table>
						<el-empty v-if="state.projectSelections.length === 0" description="该项目暂无选型单" :image-size="60" />
					</el-card>
				</div>

				<!-- 选型单详情视图 -->
				<div v-else-if="state.currentPlan" class="plan-view">
					<el-card shadow="hover" class="section-card">
						<template #header>
							<div class="card-header">
								<div class="header-left">
									<el-button size="small" circle @click="backToProject" class="mr10">
										<el-icon><ele-ArrowLeft /></el-icon>
									</el-button>
									<div class="header-icon-box">
										<el-icon><ele-DocumentCopy /></el-icon>
									</div>
									<div class="ml10">
										<div class="title-text">{{ state.currentPlan.name }}</div>
										<div class="subtitle">{{ state.selectedProjectName }}</div>
									</div>
									<el-tag :type="statusType(state.currentPlan.status)" effect="dark" class="ml15">
										{{ statusText(state.currentPlan.status) }}
									</el-tag>
								</div>
								<div class="header-actions">
									<el-button
										v-if="state.currentPlan.status === 'Draft' || state.currentPlan.status === 0"
										type="warning"
										@click="submitPlan"
										:disabled="!state.currentPlan.items?.length"
									>
										提交选型单
									</el-button>
									<el-button
										v-if="state.currentPlan.status === 'Submitted' || state.currentPlan.status === 1"
										type="danger"
										plain
										@click="cancelPlan"
									>
										取消选型单
									</el-button>
								</div>
							</div>
						</template>

						<!-- 统计栏 -->
						<div class="stats-grid mb20">
							<div class="stat-card-mini bg-primary">
								<div class="stat-icon-mini">
									<el-icon><ele-List /></el-icon>
								</div>
								<div class="stat-info-mini">
									<div class="stat-value">{{ state.currentPlan.items?.length || 0 }}</div>
									<div class="stat-label">配件项</div>
								</div>
							</div>
							<div class="stat-card-mini bg-warning">
								<div class="stat-icon-mini">
									<el-icon><ele-Lock /></el-icon>
								</div>
								<div class="stat-info-mini">
									<div class="stat-value">{{ totalLocked }}</div>
									<div class="stat-label">已锁定</div>
								</div>
							</div>
							<div class="stat-card-mini bg-success">
								<div class="stat-icon-mini">
									<el-icon><ele-Promotion /></el-icon>
								</div>
								<div class="stat-info-mini">
									<div class="stat-value">{{ totalOutbound }}</div>
									<div class="stat-label">已出库</div>
								</div>
							</div>
							<div class="stat-card-mini bg-danger">
								<div class="stat-icon-mini">
									<el-icon><ele-ShoppingCart /></el-icon>
								</div>
								<div class="stat-info-mini">
									<div class="stat-value">{{ totalPending }}</div>
									<div class="stat-label">待采购</div>
								</div>
							</div>
							<div class="stat-card-mini bg-info">
								<div class="stat-icon-mini">
									<el-icon><ele-Memo /></el-icon>
								</div>
								<div class="stat-info-mini">
									<div class="stat-value">{{ totalRequired }}</div>
									<div class="stat-label">需求总数</div>
								</div>
							</div>
						</div>

						<div class="mb15 flex-between">
							<div class="section-title">配件清单</div>
							<el-button
								v-if="state.currentPlan.status === 'Draft' || state.currentPlan.status === 0"
								type="primary"
								size="small"
								@click="showAddItem"
							>
								<el-icon><ele-Plus /></el-icon>添加配件
							</el-button>
						</div>

						<el-table :data="state.currentPlan.items" stripe class="custom-table">
							<el-table-column prop="partName" label="配件名称" min-width="180" show-overflow-tooltip />
							<el-table-column prop="category" label="分类" width="120" />
							<el-table-column prop="requiredQty" label="需求" width="80" align="center" />
							<el-table-column label="已锁定" width="80" align="center">
								<template #default="{ row }">
									<span class="text-primary font-bold">{{ row.lockedQty || 0 }}</span>
								</template>
							</el-table-column>
							<el-table-column label="已出库" width="80" align="center">
								<template #default="{ row }">
									<span class="text-success font-bold">{{ row.outboundQty || 0 }}</span>
								</template>
							</el-table-column>
							<el-table-column label="待采购" width="80" align="center">
								<template #default="{ row }">
									<span class="text-warning font-bold">{{ row.pendingQty || 0 }}</span>
								</template>
							</el-table-column>
							<el-table-column label="状态" width="100" align="center">
								<template #default="{ row }">
									<el-tag size="small" :type="itemStatusType(row)" effect="plain">{{ itemStatusText(row) }}</el-tag>
								</template>
							</el-table-column>
							<el-table-column label="操作" width="140" align="center" fixed="right">
								<template #default="{ row, $index }">
									<div class="operation-column">
										<template v-if="state.currentPlan.status === 'Draft' || state.currentPlan.status === 0">
											<el-button size="small" type="primary" link @click="selectPart(row)">更换</el-button>
											<el-button size="small" type="danger" link @click="removeItem($index)">删除</el-button>
										</template>
										<template v-else-if="state.currentPlan.status === 'Submitted' || state.currentPlan.status === 1">
											<el-button size="small" type="success" link @click="showOutbound(row)" :disabled="row.lockedQty <= 0">出库</el-button>
										</template>
									</div>
								</template>
							</el-table-column>
						</el-table>
					</el-card>
				</div>
			</div>
		</div>

		<!-- 新建选型单弹窗 -->
		<el-dialog v-model="state.addPlanDialog.visible" title="新建选型单" width="400px">
			<el-form :model="state.addPlanDialog.form" label-width="80px">
				<el-form-item label="所属项目">
					<el-input :value="state.selectedNode?.name" disabled />
				</el-form-item>
				<el-form-item label="名称" required>
					<el-input v-model="state.addPlanDialog.form.name" placeholder="请输入选型单名称" clearable />
				</el-form-item>
			</el-form>
			<template #footer>
				<el-button @click="state.addPlanDialog.visible = false">取 消</el-button>
				<el-button type="primary" @click="confirmAddPlan" :loading="state.addPlanDialog.loading">确 定</el-button>
			</template>
		</el-dialog>

		<!-- 配件选择弹窗 -->
		<el-dialog v-model="state.partDialog.visible" :title="state.partDialog.currentItem ? '更换配件' : '添加配件'" width="1000px">
			<div class="part-selector">
				<div class="search-bar mb15">
					<el-input v-model="state.partDialog.filters.keyword" placeholder="搜索名称/型号/品牌" clearable style="width: 220px" @keyup.enter="searchParts" />
					<el-cascader
						v-model="state.partDialog.filters.categoryPath"
						:options="state.categories"
						:props="{ label: 'name', value: 'path', children: 'children', checkStrictly: true }"
						placeholder="全部分类"
						clearable
						class="ml10"
						style="width: 200px"
						@change="searchParts"
					/>
					<el-button type="primary" class="ml10" @click="searchParts" :loading="state.partDialog.loading">搜索</el-button>
					<el-button @click="resetPartFilters">重置</el-button>
				</div>

				<el-table
					:data="state.partDialog.results"
					stripe
					max-height="400"
					v-loading="state.partDialog.loading"
					highlight-current-row
					@current-change="onPartSelect"
					class="custom-table"
				>
					<el-table-column prop="name" label="配件名称" min-width="150" />
					<el-table-column prop="model" label="型号" width="140" />
					<el-table-column prop="brand" label="品牌" width="100" />
					<el-table-column prop="category" label="分类" width="120" />
					<el-table-column prop="availableQty" label="可用库存" width="100" align="center">
						<template #default="{ row }">
							<span :class="{ 'text-danger font-bold': row.availableQty < 5 }">{{ row.availableQty }}</span>
						</template>
					</el-table-column>
				</el-table>

				<div v-if="state.partDialog.selectedPart" class="selected-preview mt15">
					<div class="preview-info">
						已选择: <span class="font-bold text-primary">{{ state.partDialog.selectedPart.name }}</span>
						<span class="ml10 text-muted">({{ state.partDialog.selectedPart.model }})</span>
					</div>
					<el-form v-if="!state.partDialog.currentItem" inline class="mt10">
						<el-form-item label="需求数量">
							<el-input-number v-model="state.partDialog.requiredQty" :min="1" :max="9999" />
						</el-form-item>
					</el-form>
				</div>
			</div>
			<template #footer>
				<el-button @click="state.partDialog.visible = false">取 消</el-button>
				<el-button type="primary" @click="confirmPart" :disabled="!state.partDialog.selectedPart">确 定</el-button>
			</template>
		</el-dialog>

		<!-- 出库弹窗 -->
		<el-dialog v-model="state.outboundDialog.visible" title="配件出库" width="400px">
			<el-form :model="state.outboundDialog.form" label-width="100px">
				<el-form-item label="配件">
					<span class="font-bold">{{ state.outboundDialog.item?.partName }}</span>
				</el-form-item>
				<el-form-item label="可出库数量">
					<span class="text-primary font-bold">{{ state.outboundDialog.item?.lockedQty }}</span>
				</el-form-item>
				<el-form-item label="出库数量">
					<el-input-number v-model="state.outboundDialog.form.qty" :min="1" :max="state.outboundDialog.item?.lockedQty" />
				</el-form-item>
				<el-form-item label="领用人">
					<el-input v-model="state.outboundDialog.form.recipientName" placeholder="请输入领用人姓名" />
				</el-form-item>
			</el-form>
			<template #footer>
				<el-button @click="state.outboundDialog.visible = false">取 消</el-button>
				<el-button type="primary" @click="confirmOutbound" :loading="state.outboundDialog.loading">确认出库</el-button>
			</template>
		</el-dialog>
	</div>
</template>

<script setup lang="ts">
import { reactive, ref, computed, onMounted, defineAsyncComponent } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { getSelections, getSelection, createSelection, updateSelection, deleteSelection, submitSelection, cancelSelection, outboundSelection } from '/@/api/project';
import { getAccessoryCategoryTree, searchAccessories } from '/@/api/accessory';

// 异步引入左侧组件
const ProjectSide = defineAsyncComponent(() => import('../components/ProjectSide.vue'));

const projectSideRef = ref();
const state = reactive({
	loading: false,
	selectedNode: null as any,
	projectSelections: [] as any[],
	currentPlan: null as any,
	selectedProjectName: '',
	categories: [] as any[],
	addPlanDialog: {
		visible: false,
		loading: false,
		form: {
			name: '',
		},
	},
	partDialog: {
		visible: false,
		loading: false,
		currentItem: null as any,
		selectedPart: null as any,
		requiredQty: 1,
		filters: {
			keyword: '',
			categoryPath: [] as string[],
		},
		results: [] as any[],
	},
	outboundDialog: {
		visible: false,
		loading: false,
		item: null as any,
		form: {
			qty: 1,
			recipientName: '',
		},
	},
});

// 计算属性
const totalLocked = computed(() => state.currentPlan?.items?.reduce((s: number, i: any) => s + (i.lockedQty || 0), 0) || 0);
const totalOutbound = computed(() => state.currentPlan?.items?.reduce((s: number, i: any) => s + (i.outboundQty || 0), 0) || 0);
const totalPending = computed(() => state.currentPlan?.items?.reduce((s: number, i: any) => s + (i.pendingQty || 0), 0) || 0);
const totalRequired = computed(() => state.currentPlan?.items?.reduce((s: number, i: any) => s + (i.requiredQty || 0), 0) || 0);

// 左侧节点点击
const onProjectClick = async (data: any) => {
	if (data.type === 'selection') {
		// 如果点击的是选型单子节点
		openPlan(data._selection || { id: data.id.replace('sel_', '') });
	} else if (data.type === 'project') {
		// 如果点击的是项目节点
		state.selectedNode = data;
		state.currentPlan = null;
		loadProjectSelections(data.id);
	} else {
		// 文件夹等其他节点
		state.selectedNode = data;
		state.currentPlan = null;
		state.projectSelections = [];
	}
};

// 加载项目下的选型单
const loadProjectSelections = async (projectId: string) => {
	state.loading = true;
	try {
		const res = (await getSelections(projectId)) as any;
		state.projectSelections = res || [];
	} catch (err) {
		ElMessage.error('加载选型单列表失败');
	} finally {
		state.loading = false;
	}
};

// 打开选型单详情
const openPlan = async (plan: any) => {
	state.loading = true;
	try {
		const res = (await getSelection(plan.id)) as any;
		state.currentPlan = res;
		// 寻找项目名称
		if (state.selectedNode && state.selectedNode.id === res.projectId) {
			state.selectedProjectName = state.selectedNode.name;
		} else {
			// 如果是从树直接点击选型单，或者 state.selectedNode 不匹配，尝试从项目列表中找
			const project = projectSideRef.value?.state?.projects?.find((p: any) => p.id === res.projectId);
			state.selectedProjectName = project?.name || '未知项目';
		}
	} catch (err) {
		ElMessage.error('加载选型单详情失败');
	} finally {
		state.loading = false;
	}
};

// 返回项目视图
const backToProject = () => {
	state.currentPlan = null;
	if (state.selectedNode?.id) {
		loadProjectSelections(state.selectedNode.id);
	}
};

// 新建选型单
const onOpenAddPlan = () => {
	state.addPlanDialog.form.name = '';
	state.addPlanDialog.visible = true;
};

const confirmAddPlan = async () => {
	if (!state.addPlanDialog.form.name) {
		ElMessage.warning('请输入选型单名称');
		return;
	}
	state.addPlanDialog.loading = true;
	try {
		const res = (await createSelection({
			name: state.addPlanDialog.form.name,
			projectId: state.selectedNode.id,
			items: [],
		})) as any;
		ElMessage.success('创建成功');
		state.addPlanDialog.visible = false;
		projectSideRef.value?.loadData();
		openPlan(res);
	} catch (err) {
		ElMessage.error('创建失败');
	} finally {
		state.addPlanDialog.loading = false;
	}
};

// 删除选型单
const deletePlan = (plan: any) => {
	ElMessageBox.confirm(`确定删除选型单 "${plan.name}" 吗？`, '提示', { type: 'warning' })
		.then(async () => {
			try {
				await deleteSelection(plan.id);
				ElMessage.success('删除成功');
				projectSideRef.value?.loadData();
				loadProjectSelections(state.selectedNode.id);
			} catch (err) {
				ElMessage.error('删除失败');
			}
		})
		.catch(() => {});
};

// 提交选型单
const submitPlan = () => {
	ElMessageBox.confirm('提交后将锁定库存，是否继续？', '提示', { type: 'warning' })
		.then(async () => {
			try {
				await submitSelection(state.currentPlan.id);
				ElMessage.success('提交成功');
				openPlan(state.currentPlan);
			} catch (err: any) {
				ElMessage.error(err.response?.data?.message || '提交失败');
			}
		})
		.catch(() => {});
};

// 取消选型单
const cancelPlan = () => {
	ElMessageBox.confirm('取消后将解锁已锁定的库存，是否继续？', '提示', { type: 'warning' })
		.then(async () => {
			try {
				await cancelSelection(state.currentPlan.id);
				ElMessage.success('已取消');
				openPlan(state.currentPlan);
			} catch (err) {
				ElMessage.error('取消失败');
			}
		})
		.catch(() => {});
};

// 添加/更换配件
const showAddItem = () => {
	state.partDialog.currentItem = null;
	state.partDialog.selectedPart = null;
	state.partDialog.requiredQty = 1;
	state.partDialog.visible = true;
	searchParts();
};

const selectPart = (item: any) => {
	state.partDialog.currentItem = item;
	state.partDialog.selectedPart = null;
	state.partDialog.visible = true;
	searchParts();
};

const searchParts = async () => {
	state.partDialog.loading = true;
	try {
		const categoryPath = state.partDialog.filters.categoryPath;
		const res = (await searchAccessories({
			keyword: state.partDialog.filters.keyword,
			categoryPath: categoryPath && categoryPath.length ? categoryPath[categoryPath.length - 1] : null,
		})) as any;
		state.partDialog.results = res || [];
	} catch (err) {
		ElMessage.error('搜索配件失败');
	} finally {
		state.partDialog.loading = false;
	}
};

const resetPartFilters = () => {
	state.partDialog.filters.keyword = '';
	state.partDialog.filters.categoryPath = [];
	searchParts();
};

const onPartSelect = (val: any) => {
	state.partDialog.selectedPart = val;
};

const confirmPart = async () => {
	if (!state.partDialog.selectedPart) return;
	const part = state.partDialog.selectedPart;
	if (state.partDialog.currentItem) {
		// 更换配件
		state.partDialog.currentItem.selectedPartId = part.id;
		state.partDialog.currentItem.partName = part.name;
		state.partDialog.currentItem.category = part.category;
	} else {
		// 新增配件
		if (!state.currentPlan.items) state.currentPlan.items = [];
		state.currentPlan.items.push({
			id: 'new_' + Date.now(),
			selectedPartId: part.id,
			partName: part.name,
			category: part.category,
			requiredQty: state.partDialog.requiredQty,
			lockedQty: 0,
			outboundQty: 0,
			pendingQty: 0,
		});
	}
	state.partDialog.visible = false;
	saveItems();
};

const removeItem = (index: number) => {
	state.currentPlan.items.splice(index, 1);
	saveItems();
};

const saveItems = async () => {
	try {
		await updateSelection(state.currentPlan.id, {
			name: state.currentPlan.name,
			projectId: state.currentPlan.projectId,
			items: state.currentPlan.items,
		});
	} catch (err) {
		ElMessage.error('保存失败');
	}
};

// 出库操作
const showOutbound = (item: any) => {
	state.outboundDialog.item = item;
	state.outboundDialog.form.qty = item.lockedQty;
	state.outboundDialog.form.recipientName = '';
	state.outboundDialog.visible = true;
};

const confirmOutbound = async () => {
	state.outboundDialog.loading = true;
	try {
		await outboundSelection(state.currentPlan.id, state.outboundDialog.item.id, {
			qty: state.outboundDialog.form.qty,
			recipientName: state.outboundDialog.form.recipientName,
		});
		ElMessage.success('出库成功');
		state.outboundDialog.visible = false;
		openPlan(state.currentPlan);
	} catch (err: any) {
		ElMessage.error(err.response?.data?.message || '出库失败');
	} finally {
		state.outboundDialog.loading = false;
	}
};

// 工具函数
const formatDate = (d: string) => (d ? new Date(d).toLocaleString('zh-CN') : '-');

const statusType = (status: any) => {
	const map: any = { Draft: 'info', Submitted: 'warning', Completed: 'success', Cancelled: 'danger', 0: 'info', 1: 'warning', 2: 'success', 3: 'danger' };
	return map[status] || 'info';
};

const statusText = (status: any) => {
	const map: any = { Draft: '草稿', Submitted: '已提交', Completed: '已完成', Cancelled: '已取消', 0: '草稿', 1: '已提交', 2: '已完成', 3: '已取消' };
	return map[status] || status;
};

const itemStatusType = (item: any) => {
	if (item.requiredQty === item.outboundQty) return 'success';
	if (item.pendingQty > 0) return 'warning';
	if (item.outboundQty > 0) return 'primary';
	return 'info';
};

const itemStatusText = (item: any) => {
	if (item.requiredQty === item.outboundQty) return '已完成';
	if (item.pendingQty > 0) return '部分待采购';
	if (item.outboundQty > 0) return '部分出库';
	if (item.lockedQty > 0) return '已锁定';
	return '待处理';
};

onMounted(async () => {
	const treeRes = (await getAccessoryCategoryTree()) as any;
	state.categories = treeRes || [];
});
</script>

<style scoped lang="scss">
.selection-container {
	height: 100%;
}
.selection-layout {
	display: flex;
	height: 100%;
	background: var(--el-bg-color);
	border: 1px solid var(--el-border-color-lighter);
	border-radius: 4px;
	overflow: hidden;
}
.layout-side {
	width: 260px;
	flex-shrink: 0;
}
.layout-main {
	flex: 1;
	min-width: 0;
	display: flex;
	flex-direction: column;
	background-color: var(--el-bg-color-page);
	padding: 15px;
}
.section-card {
	height: 100%;
	display: flex;
	flex-direction: column;
	:deep(.el-card__body) {
		flex: 1;
		display: flex;
		flex-direction: column;
		overflow: hidden;
	}
}
.card-header {
	display: flex;
	align-items: center;
	justify-content: space-between;
}
.header-left {
	display: flex;
	align-items: center;
}
.header-icon-box {
	width: 32px;
	height: 32px;
	background-color: var(--el-color-primary-light-9);
	color: var(--el-color-primary);
	border-radius: 8px;
	display: flex;
	align-items: center;
	justify-content: center;
	font-size: 18px;
}
.title-text {
	font-size: 16px;
	font-weight: 600;
	color: var(--el-text-color-primary);
}
.subtitle {
	font-size: 13px;
	color: var(--el-text-color-secondary);
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
.name-cell {
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
}
.item-badge {
	:deep(.el-badge__content) {
		top: 0;
	}
}
.stats-grid {
	display: grid;
	grid-template-columns: repeat(5, 1fr);
	gap: 15px;
}
.stat-card-mini {
	display: flex;
	align-items: center;
	padding: 15px 18px;
	border-radius: 12px;
	transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
	border: 1px solid transparent;
	&:hover {
		transform: translateY(-4px);
		box-shadow: 0 8px 20px rgba(0, 0, 0, 0.1);
		filter: brightness(1.05);
	}
	&.bg-primary {
		background: linear-gradient(135deg, #e6f7ff 0%, #bae7ff 100%);
		border-color: #91d5ff;
		.stat-icon-mini { background: #1890ff; color: #fff; }
		.stat-value { color: #003a8c; }
		.stat-label { color: #096dd9; }
	}
	&.bg-warning {
		background: linear-gradient(135deg, #fffbe6 0%, #fff1b8 100%);
		border-color: #ffe58f;
		.stat-icon-mini { background: #faad14; color: #fff; }
		.stat-value { color: #874d00; }
		.stat-label { color: #d48806; }
	}
	&.bg-success {
		background: linear-gradient(135deg, #f6ffed 0%, #d9f7be 100%);
		border-color: #b7eb8f;
		.stat-icon-mini { background: #52c41a; color: #fff; }
		.stat-value { color: #135200; }
		.stat-label { color: #389e0d; }
	}
	&.bg-danger {
		background: linear-gradient(135deg, #fff1f0 0%, #ffccc7 100%);
		border-color: #ffa39e;
		.stat-icon-mini { background: #ff4d4f; color: #fff; }
		.stat-value { color: #820014; }
		.stat-label { color: #cf1322; }
	}
	&.bg-info {
		background: linear-gradient(135deg, #f9f0ff 0%, #efdbff 100%);
		border-color: #d3adf7;
		.stat-icon-mini { background: #722ed1; color: #fff; }
		.stat-value { color: #391085; }
		.stat-label { color: #531dab; }
	}
}
.stat-icon-mini {
	width: 36px;
	height: 36px;
	border-radius: 8px;
	display: flex;
	align-items: center;
	justify-content: center;
	font-size: 18px;
	margin-right: 12px;
	box-shadow: 0 2px 6px rgba(0, 0, 0, 0.1);
}
.stat-info-mini {
	flex: 1;
}
.stat-value {
	font-size: 18px;
	font-weight: 700;
	line-height: 1.2;
	color: var(--el-text-color-primary);
}
.stat-label {
	font-size: 12px;
	color: var(--el-text-color-secondary);
	margin-top: 2px;
}
.section-title {
	font-size: 14px;
	font-weight: 600;
	color: var(--el-text-color-primary);
	position: relative;
	padding-left: 10px;
	&::before {
		content: '';
		position: absolute;
		left: 0;
		top: 50%;
		transform: translateY(-50%);
		width: 3px;
		height: 14px;
		background-color: var(--el-color-primary);
		border-radius: 2px;
	}
}
.font-bold {
	font-weight: 700;
}
.flex-between {
	display: flex;
	align-items: center;
	justify-content: space-between;
}
</style>
