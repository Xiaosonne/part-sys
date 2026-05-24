<template>
	<div class="accessory-category-tree">
		<div class="tree-header">
			<div class="tree-title">配件分类</div>
			<el-button size="small" type="primary" text @click="loadTree">
				<el-icon><ele-Refresh /></el-icon>
			</el-button>
		</div>

		<el-tree
			ref="treeRef"
			:data="treeData"
			:props="{ label: 'name', children: 'children' }"
			node-key="id"
			:current-node-key="selectedId"
			highlight-current
			default-expand-all
			:expand-on-click-node="false"
			@node-click="onNodeClick"
		>
			<template #default="{ node, data }">
				<div class="tree-node">
					<div class="node-left">
						<span class="node-label">{{ node.label }}</span>
						<el-tag v-if="data.specTemplateId" size="small" type="success" class="ml6">锁定</el-tag>
					</div>
					<!-- <el-button size="small" type="primary" text class="node-action" @click.stop="emit('edit', data)">
						<el-icon><ele-EditPen /></el-icon>
					</el-button> -->
				</div>
			</template>
		</el-tree>
	</div>
</template>

<script setup lang="ts">
import { nextTick, onMounted, ref, watch } from 'vue';
import { ElMessage } from 'element-plus';
import { getAccessoryCategoryTree, type AccessoryCategoryTreeItem } from '/@/api/accessory';

const props = defineProps<{
	selectedId?: string | null;
}>();

const emit = defineEmits<{
	(e: 'select', val: AccessoryCategoryTreeItem): void;
	(e: 'edit', val: AccessoryCategoryTreeItem): void;
}>();

const treeRef = ref<any>(null);
const treeData = ref<AccessoryCategoryTreeItem[]>([]);

// 将后端扁平分类列表按 parentId 组装为树形 children 结构
const buildTree = (list: AccessoryCategoryTreeItem[]) => {
	const map = new Map<string, AccessoryCategoryTreeItem>();
	const roots: AccessoryCategoryTreeItem[] = [];

	list.forEach((n) => {
		map.set(n.id, { ...n, children: [] });
	});

	list.forEach((n) => {
		const node = map.get(n.id)!;
		const parentId = node.parentId ?? null;
		if (parentId && map.has(parentId)) {
			map.get(parentId)!.children!.push(node);
		} else {
			roots.push(node);
		}
	});

	const sortNodes = (nodes: AccessoryCategoryTreeItem[]) => {
		nodes.sort((a, b) => {
			const sa = a.sortOrder ?? 0;
			const sb = b.sortOrder ?? 0;
			if (sa !== sb) return sa - sb;
			return String(a.name || '').localeCompare(String(b.name || ''), 'zh-Hans-CN');
		});
		nodes.forEach((n) => {
			if (n.children?.length) sortNodes(n.children);
		});
	};

	sortNodes(roots);
	return roots;
};

// 加载分类树数据（加载完成后默认选中第一项）
const loadTree = async () => {
	try {
		const res = (await getAccessoryCategoryTree()) as AccessoryCategoryTreeItem[];
		const flat = Array.isArray(res) ? res : [];
		treeData.value = buildTree(flat);
		await nextTick();
		if (props.selectedId) {
			treeRef.value?.setCurrentKey?.(props.selectedId);
			return;
		}
		const first = treeData.value[0];
		if (first?.id) {
			treeRef.value?.setCurrentKey?.(first.id);
			emit('select', first);
		}
	} catch (e) {
		ElMessage.error('加载分类失败');
		treeData.value = [];
	}
};

const setCurrentKey = async (id: string | null | undefined) => {
	await nextTick();
	if (!id) return;
	treeRef.value?.setCurrentKey?.(id);
};

// 点击节点：通知父页面切换分类
const onNodeClick = (data: AccessoryCategoryTreeItem) => {
	emit('select', data);
};

defineExpose({
	loadTree,
	setCurrentKey,
});

watch(
	() => props.selectedId,
	(val) => {
		if (!val) return;
		setCurrentKey(val);
	},
	{ immediate: true }
);

onMounted(() => {
	loadTree();
});
</script>

<style scoped lang="scss">
.accessory-category-tree {
	height: 100%;
	display: flex;
	flex-direction: column;
}

.tree-header {
	display: flex;
	align-items: center;
	justify-content: space-between;
	margin-bottom: 8px;
}

.tree-title {
	font-size: 14px;
	font-weight: 600;
	color: var(--el-text-color-primary);
}

.accessory-category-tree :deep(.el-tree) {
	flex: 1;
	overflow: auto;
	overflow-x: hidden;
}

.accessory-category-tree :deep(.el-tree-node__content) {
	width: 100%;
	overflow: hidden;
}

.tree-node {
	width: 100%;
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 8px;
	padding-right: 6px;
}

.node-left {
	min-width: 0;
	display: flex;
	align-items: center;
	gap: 6px;
}

.node-label {
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.node-action {
	flex: none;
}
</style>
