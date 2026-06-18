<template>
	<div class="project-side">
		<div class="side-header">
			<el-input v-model="state.search" placeholder="搜索项目/文件夹" clearable @input="onSearch">
				<template #prefix>
					<el-icon><ele-Search /></el-icon>
				</template>
			</el-input>
			<el-button type="primary" class="mt10 w100" @click="onOpenAddDialog">
				<el-icon><ele-Plus /></el-icon>
				新建
			</el-button>
		</div>
		<div class="side-content">
			<el-scrollbar>
				<el-tree
					ref="treeRef"
					:data="treeData"
					:props="{ label: 'name', children: 'children' }"
					node-key="id"
					highlight-current
					default-expand-all
					:filter-node-method="filterNode"
					@node-click="onNodeClick"
				>
					<template #default="{ node, data }">
						<span class="custom-tree-node">
							<template v-if="data.type === 'folder'">
								<el-icon class="mr5 folder-icon"><ele-Folder /></el-icon>
							</template>
							<template v-else-if="data.type === 'project'">
								<el-icon class="mr5 project-icon"><ele-Memo /></el-icon>
							</template>
							<template v-else-if="data.type === 'selection'">
								<el-icon class="mr5 selection-icon"><ele-DocumentCopy /></el-icon>
							</template>
							<span class="node-label">{{ node.label }}</span>
							<el-tag v-if="data.type === 'selection'" size="small" :type="statusType(data.status)" class="ml5 status-tag">
								{{ statusText(data.status) }}
							</el-tag>
						</span>
					</template>
				</el-tree>
			</el-scrollbar>
		</div>

		<!-- 新建/编辑弹窗 -->
		<el-dialog v-model="state.dialog.visible" :title="state.dialog.title" width="400px">
			<el-form :model="state.dialog.form" label-width="80px">
				<el-form-item label="类型">
					<el-radio-group v-model="state.dialog.form.type">
						<el-radio label="project">项目</el-radio>
						<el-radio label="folder">文件夹</el-radio>
					</el-radio-group>
				</el-form-item>
				<el-form-item label="名称">
					<el-input v-model="state.dialog.form.name" placeholder="请输入名称" clearable></el-input>
				</el-form-item>
				<el-form-item label="父级">
					<el-tree-select
						v-model="state.dialog.form.parentId"
						:data="treeData"
						:props="{ label: 'name', value: 'id', children: 'children' }"
						check-strictly
						placeholder="请选择父级 (根级则不选)"
						clearable
						class="w100"
					/>
				</el-form-item>
			</el-form>
			<template #footer>
				<span class="dialog-footer">
					<el-button @click="state.dialog.visible = false" size="default">取 消</el-button>
					<el-button type="primary" @click="onSubmit" size="default" :loading="state.dialog.loading">确 定</el-button>
				</span>
			</template>
		</el-dialog>
	</div>
</template>

<script setup lang="ts">
import { reactive, ref, onMounted, computed } from 'vue';
import { ElMessage } from 'element-plus';
import { getProjects, createProject, getSelections } from '/@/api/project';

const props = defineProps({
	showSelections: {
		type: Boolean,
		default: false,
	},
});

const emit = defineEmits(['node-click']);

const treeRef = ref();
const state = reactive({
	search: '',
	projects: [] as any[],
	selections: [] as any[],
	dialog: {
		visible: false,
		title: '新建项目/文件夹',
		loading: false,
		form: {
			name: '',
			type: 'project',
			parentId: null as string | null,
		},
	},
});

// 构建树形结构
const treeData = computed(() => {
	const build = (parentId: string | null = null): any[] => {
		const nodes = state.projects
			.filter((p) => p.parentId === parentId)
			.map((p) => {
				const children = build(p.id);
				// 如果开启了显示选型单，且当前节点是项目，则将该项目的选型单加入子节点
				if (props.showSelections && p.type === 'project') {
					const projectSelections = state.selections
						.filter((s) => s.projectId === p.id)
						.map((s) => ({
							...s,
							type: 'selection',
							id: `sel_${s.id}`, // 避免与项目 ID 冲突
							_selection: s, // 保留原始对象
						}));
					children.push(...projectSelections);
				}
				return {
					...p,
					children,
				};
			});
		return nodes;
	};
	return build(null);
});

const loadData = async () => {
	try {
		const [projRes, selRes] = await Promise.all([
			getProjects(),
			props.showSelections ? getSelections() : Promise.resolve([]),
		]);
		state.projects = (projRes as any) || [];
		state.selections = (selRes as any) || [];
	} catch (err) {
		ElMessage.error('加载列表失败');
	}
};

const onSearch = (val: string) => {
	treeRef.value!.filter(val);
};

const filterNode = (value: string, data: any) => {
	if (!value) return true;
	return data.name.includes(value);
};

const onNodeClick = (data: any) => {
	emit('node-click', data);
};

const onOpenAddDialog = () => {
	state.dialog.form = {
		name: '',
		type: 'project',
		parentId: null,
	};
	state.dialog.visible = true;
};

const onSubmit = async () => {
	if (!state.dialog.form.name) {
		ElMessage.warning('请输入名称');
		return;
	}
	state.dialog.loading = true;
	try {
		await createProject(state.dialog.form);
		ElMessage.success('创建成功');
		state.dialog.visible = false;
		await loadData();
	} catch (err: any) {
		ElMessage.error(err.message || '创建失败');
	} finally {
		state.dialog.loading = false;
	}
};

const formatDate = (d: string) => (d ? new Date(d).toLocaleString('zh-CN') : '-');

const statusType = (status: any) => {
	const map: any = { Draft: 'info', Submitted: 'warning', Completed: 'success', Cancelled: 'danger', 0: 'info', 1: 'warning', 2: 'success', 3: 'danger' };
	return map[status] || 'info';
};

const statusText = (status: any) => {
	const map: any = { Draft: '草稿', Submitted: '已提交', Completed: '已完成', Cancelled: '已取消', 0: '草稿', 1: '已提交', 2: '已完成', 3: '已取消' };
	return map[status] || status;
};

onMounted(() => {
	loadData();
});

defineExpose({
	loadData,
});
</script>

<style scoped lang="scss">
.project-side {
	display: flex;
	flex-direction: column;
	height: 100%;
	padding: 15px;
	border-right: 1px solid var(--el-border-color-lighter);
	background: var(--el-bg-color);
}
.side-header {
	margin-bottom: 15px;
}
.side-content {
	flex: 1;
	overflow: hidden;
}
.custom-tree-node {
	display: flex;
	align-items: center;
	font-size: 14px;
	width: 100%;
	overflow: hidden;
}
.node-label {
	flex: 1;
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}
.folder-icon { color: #e6a23c; }
.project-icon { color: #409eff; }
.selection-icon { color: #67c23a; }
.status-tag {
	flex-shrink: 0;
	transform: scale(0.8);
	margin-left: -5px;
}
.mr5 {
	margin-right: 5px;
}
.ml5 {
	margin-left: 5px;
}
.mt10 {
	margin-top: 10px;
}
.w100 {
	width: 100%;
}
</style>
