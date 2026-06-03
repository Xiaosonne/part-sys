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
							<el-icon v-if="data.type === 'folder'" class="mr5"><ele-Folder /></el-icon>
							<el-icon v-else class="mr5"><ele-Memo /></el-icon>
							<span>{{ node.label }}</span>
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
import { getProjects, createProject } from '/@/api/project';

const emit = defineEmits(['node-click']);

const treeRef = ref();
const state = reactive({
	search: '',
	projects: [] as any[],
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
		return state.projects
			.filter((p) => p.parentId === parentId)
			.map((p) => ({
				...p,
				children: build(p.id),
			}));
	};
	return build(null);
});

const loadData = async () => {
	try {
		const res = (await getProjects()) as any;
		state.projects = res || [];
	} catch (err) {
		ElMessage.error('加载项目列表失败');
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
}
.mr5 {
	margin-right: 5px;
}
.mt10 {
	margin-top: 10px;
}
.w100 {
	width: 100%;
}
</style>
