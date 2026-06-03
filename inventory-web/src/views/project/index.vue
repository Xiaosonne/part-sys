<template>
	<div class="project-container layout-padding">
		<div class="project-layout">
			<!-- 左侧项目列表 -->
			<div class="layout-side">
				<ProjectSide ref="projectSideRef" @node-click="onProjectClick" />
			</div>

			<!-- 右侧内容区 -->
			<div class="layout-main">
				<el-scrollbar v-if="state.selectedNode">
					<div class="content-wrapper">
						<!-- 项目/文件夹标题 -->
						<div class="content-header">
							<div class="header-title">
								<el-icon v-if="state.selectedNode.type === 'folder'"><ele-Folder /></el-icon>
								<el-icon v-else><ele-Memo /></el-icon>
								<span class="ml10">{{ state.selectedNode.name }}</span>
							</div>
							<div class="header-actions">
								<el-button v-if="state.selectedNode.type === 'project'" type="warning" plain size="small" @click="onReinitialize" :loading="state.reinitializing">
									重新初始化工作区
								</el-button>
								<el-button type="danger" plain size="small" @click="onDeleteNode">删除{{ state.selectedNode.type === 'project' ? '项目' : '文件夹' }}</el-button>
							</div>
						</div>

						<!-- 文件管理 (仅项目可见) -->
						<template v-if="state.selectedNode.type === 'project'">
							<el-card shadow="hover" class="mt15 section-card">
								<template #header>
									<div class="card-header">
										<span><el-icon><ele-FolderOpened /></el-icon> 文件管理</span>
									</div>
								</template>

								<div class="file-manager">
									<!-- 文件树导航 -->
									<div class="file-tree-nav">
										<div class="nav-header">
											<el-icon><ele-Memo /></el-icon>
											<span class="ml5">文件夹结构</span>
										</div>
										<el-scrollbar>
											<el-tree
												:data="state.fileTree"
												:props="{ label: 'displayName', children: 'children' }"
												node-key="path"
												highlight-current
												@node-click="onFileTreeClick"
												class="custom-file-tree"
											>
												<template #default="{ data }">
													<span class="custom-tree-node">
														<el-icon class="folder-icon"><ele-Folder /></el-icon>
														<span class="ml5">{{ data.displayName }}</span>
													</span>
												</template>
											</el-tree>
										</el-scrollbar>
									</div>

									<!-- 文件内容列表 -->
									<div class="file-list-area">
										<!-- 面包屑 -->
										<div class="breadcrumb-bar">
											<el-breadcrumb separator-icon="ArrowRight">
												<el-breadcrumb-item @click="navigateToPath(-1)" class="cursor-pointer">
													<el-icon><ele-HomeFilled /></el-icon>
													<span class="ml5">根目录</span>
												</el-breadcrumb-item>
												<el-breadcrumb-item v-for="(seg, idx) in pathSegments" :key="idx" @click="navigateToPath(idx)" class="cursor-pointer">
													{{ seg }}
												</el-breadcrumb-item>
											</el-breadcrumb>
										</div>

										<!-- 工具栏 -->
										<div class="toolbar mt15">
											<div class="toolbar-left">
												<el-upload ref="uploadRef" action="#" :auto-upload="false" :show-file-list="false" @change="onFileChange">
													<el-button type="primary">
														<el-icon><ele-Upload /></el-icon>
														<span class="ml5">上传文件</span>
													</el-button>
												</el-upload>
												<el-button class="ml10" @click="state.folderDialog.visible = true">
													<el-icon><ele-FolderAdd /></el-icon>
													<span class="ml5">新建文件夹</span>
												</el-button>
											</div>
											<div class="toolbar-right" v-if="state.selectedFile">
												<el-tag closable @close="state.selectedFile = null" class="mr10">{{ state.selectedFile.name }}</el-tag>
												<el-button type="success" :loading="state.uploading" @click="doUpload">
													<el-icon><ele-Check /></el-icon>
													<span class="ml5">确认上传</span>
												</el-button>
											</div>
										</div>

										<!-- 文件列表表格 -->
										<el-table :data="state.files" stripe class="mt15 custom-file-table">
											<template #empty>
												<el-empty description="该目录下暂无文件" :image-size="100" />
											</template>
											<el-table-column label="名称" min-width="250">
												<template #default="{ row }">
													<div class="file-item-name" @click="row.isFolder ? enterFolder(row.name) : null" :class="{ 'is-folder': row.isFolder }">
														<div class="icon-box" :class="row.isFolder ? 'folder' : 'file'">
															<el-icon v-if="row.isFolder"><ele-Folder /></el-icon>
															<el-icon v-else><ele-Document /></el-icon>
														</div>
														<span class="ml10">{{ row.displayName || row.name }}</span>
													</div>
												</template>
											</el-table-column>
											<el-table-column prop="size" label="大小" width="100">
												<template #default="{ row }">
													{{ row.isFolder ? '-' : formatSize(row.size) }}
												</template>
											</el-table-column>
											<el-table-column label="修改时间" width="160">
												<template #default="{ row }">
													{{ formatDate(row.modified) }}
												</template>
											</el-table-column>
											<el-table-column label="操作" width="120" align="center">
												<template #default="{ row }">
													<el-button v-if="!row.isFolder" size="small" text type="primary" @click="onDownload(row)">下载</el-button>
													<el-button size="small" text type="danger" @click="onDeleteFile(row)">删除</el-button>
												</template>
											</el-table-column>
										</el-table>
									</div>
								</div>
							</el-card>

							<!-- 选型管理 -->
							<el-card shadow="hover" class="mt15 section-card">
								<template #header>
									<div class="card-header">
										<div class="header-left">
											<div class="header-icon-box">
												<el-icon><ele-Memo /></el-icon>
											</div>
											<span class="ml10">选型管理</span>
										</div>
										<el-button type="primary" link @click="goToSelections">
											<span>在选型中心查看</span>
											<el-icon class="ml5"><ele-ArrowRight /></el-icon>
										</el-button>
									</div>
								</template>

								<div class="selection-content">
									<el-table v-if="state.selections.length > 0" :data="state.selections" stripe class="custom-selection-table">
										<el-table-column prop="name" label="选型单名称" min-width="200">
											<template #default="{ row }">
												<div class="selection-name-cell">
													<el-icon class="mr10 name-icon"><ele-DocumentCopy /></el-icon>
													<span class="name-text">{{ row.name }}</span>
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
										<el-table-column label="配件项" width="100" align="center">
											<template #default="{ row }">
												<div class="items-count-badge">
													<span class="count-num">{{ row.items?.length || 0 }}</span>
													<span class="count-unit">项</span>
												</div>
											</template>
										</el-table-column>
										<el-table-column label="操作" width="120" align="center" fixed="right">
											<template #default="{ row }">
												<el-button size="small" type="primary" link @click="onOpenSelection(row)">
													<el-icon class="mr5"><ele-View /></el-icon>
													查看详情
												</el-button>
											</template>
										</el-table-column>
									</el-table>
									<el-empty v-else description="暂无选型单据" :image-size="80" />
								</div>
							</el-card>
						</template>
					</div>
				</el-scrollbar>
				<el-empty v-else description="请从左侧选择一个项目或文件夹" />
			</div>
		</div>

		<!-- 新建文件夹弹窗 -->
		<el-dialog v-model="state.folderDialog.visible" title="新建文件夹" width="360px">
			<el-form :model="state.folderDialog.form" label-width="80px">
				<el-form-item label="名称">
					<el-input v-model="state.folderDialog.form.name" placeholder="文件夹名称" clearable></el-input>
				</el-form-item>
			</el-form>
			<template #footer>
				<span class="dialog-footer">
					<el-button @click="state.folderDialog.visible = false" size="default">取 消</el-button>
					<el-button type="primary" @click="onCreateFolder" size="default" :loading="state.folderDialog.loading">确 定</el-button>
				</span>
			</template>
		</el-dialog>
	</div>
</template>

<script setup lang="ts">
import { reactive, ref, computed, defineAsyncComponent } from 'vue';
import { useRouter } from 'vue-router';
import { ElMessage, ElMessageBox } from 'element-plus';
import { listFiles, uploadFile, createFolder, deleteFile, deleteFolder, reinitializeProjectWorkspace, deleteProject } from '/@/api/project';
import { getSelections } from '/@/api/inventory';

// 引入组件
const ProjectSide = defineAsyncComponent(() => import('./components/ProjectSide.vue'));

const router = useRouter();
const uploadRef = ref();
const projectSideRef = ref();

const state = reactive({
	selectedNode: null as any,
	selections: [] as any[],
	fileTree: [] as any[],
	files: [] as any[],
	currentPath: '',
	selectedFile: null as File | null,
	uploading: false,
	reinitializing: false,
	folderDialog: {
		visible: false,
		loading: false,
		form: {
			name: '',
		},
	},
});

const pathSegments = computed(() => state.currentPath.split('/').filter((s) => s));

const onProjectClick = async (data: any) => {
	state.selectedNode = data;
	state.currentPath = '';
	state.selectedFile = null;
	if (data.type === 'project') {
		await Promise.all([loadSelections(data.id), loadFileTree(), loadFiles()]);
	} else {
		state.selections = [];
		state.fileTree = [];
		state.files = [];
	}
};

const loadSelections = async (projectId: string) => {
	try {
		const res = (await getSelections(projectId)) as any;
		state.selections = res || [];
	} catch (err) {
		state.selections = [];
	}
};

const loadFileTree = async () => {
	if (!state.selectedNode || state.selectedNode.type !== 'project') return;
	try {
		const res = (await listFiles('projects', null, state.selectedNode.id)) as any;
		state.fileTree = res?.filter((f: any) => f.isFolder) || [];
	} catch (err) {
		state.fileTree = [];
	}
};

const loadFiles = async () => {
	if (!state.selectedNode) return;
	try {
		const res = (await listFiles('projects', state.currentPath || null, state.selectedNode.id)) as any;
		state.files = res || [];
	} catch (err) {
		state.files = [];
	}
};

const onFileTreeClick = (data: any) => {
	state.currentPath = data.path || '';
	loadFiles();
};

const enterFolder = (folderName: string) => {
	state.currentPath = state.currentPath ? `${state.currentPath}/${folderName}` : folderName;
	loadFiles();
};

const navigateToPath = (idx: number) => {
	if (idx === -1) {
		state.currentPath = '';
	} else {
		const segs = state.currentPath.split('/').filter((s) => s);
		state.currentPath = segs.slice(0, idx + 1).join('/');
	}
	loadFiles();
};

const onFileChange = (file: any) => {
	state.selectedFile = file.raw;
};

const doUpload = async () => {
	if (!state.selectedFile) return;
	state.uploading = true;
	try {
		const formData = new FormData();
		formData.append('file', state.selectedFile);
		formData.append('bucket', 'projects');
		formData.append('relatedId', state.selectedNode.id);
		formData.append('fileType', 'PROJECT');
		if (state.currentPath) formData.append('path', state.currentPath);
		await uploadFile(formData);
		ElMessage.success('上传成功');
		state.selectedFile = null;
		loadFiles();
		loadFileTree();
	} catch (err) {
		ElMessage.error('上传失败');
	} finally {
		state.uploading = false;
	}
};

const onCreateFolder = async () => {
	if (!state.folderDialog.form.name) return;
	state.folderDialog.loading = true;
	try {
		const path = state.currentPath ? `${state.currentPath}/${state.folderDialog.form.name}` : state.folderDialog.form.name;
		await createFolder('projects', state.selectedNode.id, path);
		ElMessage.success('文件夹创建成功');
		state.folderDialog.visible = false;
		state.folderDialog.form.name = '';
		loadFiles();
		loadFileTree();
	} catch (err) {
		ElMessage.error('创建失败');
	} finally {
		state.folderDialog.loading = false;
	}
};

const onDeleteFile = (item: any) => {
	ElMessageBox.confirm(`确定删除${item.isFolder ? '文件夹' : '文件'} "${item.displayName || item.name}" 吗？`, '警告', {
		type: 'warning',
	})
		.then(async () => {
			try {
				if (item.isFolder) {
					await deleteFolder('projects', state.selectedNode.id, item.path || item.name);
				} else {
					await deleteFile(item.id);
				}
				ElMessage.success('删除成功');
				loadFiles();
				loadFileTree();
			} catch (err) {
				ElMessage.error('删除失败');
			}
		})
		.catch(() => {});
};

const onDownload = (file: any) => {
	const url = `${import.meta.env.VITE_API_URL}/api/files/${file.id}/download`;
	window.open(url);
};

const onReinitialize = () => {
	ElMessageBox.confirm('此操作将删除所有文件并重新初始化工作区结构，是否继续？', '警告', {
		type: 'warning',
	})
		.then(async () => {
			state.reinitializing = true;
			try {
				await reinitializeProjectWorkspace(state.selectedNode.id);
				ElMessage.success('工作区已重新初始化');
				loadFiles();
				loadFileTree();
			} catch (err) {
				ElMessage.error('操作失败');
			} finally {
				state.reinitializing = false;
			}
		})
		.catch(() => {});
};

const onDeleteNode = () => {
	const node = state.selectedNode;
	ElMessageBox.confirm(`确定删除"${node.name}"？${node.type === 'folder' ? '子级也会被删除。' : ''}`, '警告', {
		type: 'warning',
	})
		.then(async () => {
			try {
				await deleteProject(node.id);
				ElMessage.success('删除成功');
				state.selectedNode = null;
				projectSideRef.value?.loadData();
			} catch (err) {
				ElMessage.error('删除失败');
			}
		})
		.catch(() => {});
};

const goToSelections = () => {
	router.push({ path: '/inventory/inventoryList', query: { projectId: state.selectedNode.id } });
};

const onOpenSelection = (row: any) => {
	router.push({ path: '/inventory/inventoryList', query: { selectionId: row.id } });
};

const formatSize = (s: number) => {
	if (!s) return '-';
	if (s < 1024) return s + ' B';
	if (s < 1024 * 1024) return (s / 1024).toFixed(1) + ' KB';
	return (s / 1024 / 1024).toFixed(1) + ' MB';
};

const formatDate = (d: string) => {
	return d ? new Date(d).toLocaleString('zh-CN') : '-';
};

const statusType = (status: any) => {
	const map: any = { Draft: 'info', Submitted: 'warning', Completed: 'success', Cancelled: 'danger', 0: 'info', 1: 'warning', 2: 'success', 3: 'danger' };
	return map[status] || 'info';
};

const statusText = (status: any) => {
	const map: any = { Draft: '草稿', Submitted: '已提交', Completed: '已完成', Cancelled: '已取消', 0: '草稿', 1: '已提交', 2: '已完成', 3: '已取消' };
	return map[status] || status;
};
</script>

<style scoped lang="scss">
.project-container {
	height: 100%;
}
.project-layout {
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
}
.content-wrapper {
	padding: 20px;
}
.content-header {
	display: flex;
	align-items: center;
	justify-content: space-between;
	padding-bottom: 15px;
	border-bottom: 1px solid var(--el-border-color-lighter);
}
.header-title {
	display: flex;
	align-items: center;
	font-size: 18px;
	font-weight: 600;
  color: #2299dd;
}
.section-card {
	:deep(.el-card__header) {
		padding: 10px 15px;
		font-weight: 600;
	}
}
.card-header {
	display: flex;
	align-items: center;
	justify-content: space-between;
}
.toolbar{
  display: flex;
  align-items: center;
}
.file-manager {
	display: flex;
	height: 400px;
	gap: 20px;
}
.file-tree-nav {
	width: 220px;
	min-width: 220px;
	border-right: 1px solid var(--el-border-color-lighter);
	display: flex;
	flex-direction: column;
	background-color: var(--el-bg-color);
}
.nav-header {
	padding: 12px 16px;
	font-size: 14px;
	font-weight: 600;
	color: var(--el-text-color-primary);
	border-bottom: 1px solid var(--el-border-color-lighter);
	background-color: var(--el-fill-color-light);
	display: flex;
	align-items: center;
	white-space: nowrap;
}
.custom-file-tree {
	padding: 8px;
	background: transparent;
	:deep(.el-tree-node__content) {
		height: 36px;
		border-radius: 4px;
		margin: 2px 0;
		&:hover {
			background-color: var(--el-fill-color-light);
		}
	}
	:deep(.el-tree-node.is-current > .el-tree-node__content) {
		background-color: var(--el-color-primary-light-9);
		color: var(--el-color-primary);
		font-weight: 600;
		.folder-icon {
			color: var(--el-color-primary);
		}
	}
}
.folder-icon {
	color: #e6a23c;
	font-size: 16px;
}
.file-list-area {
	flex: 1;
	display: flex;
	flex-direction: column;
	padding: 15px;
	background-color: #fff;
	min-width: 0;
	overflow: hidden;
}
.breadcrumb-bar {
	padding: 10px 15px;
	background-color: var(--el-fill-color-blank);
	border-radius: 4px;
	border: 1px solid var(--el-border-color-light);
	:deep(.el-breadcrumb__item) {
		.el-breadcrumb__inner {
			display: flex;
			align-items: center;
			color: var(--el-text-color-regular);
			&.is-link:hover {
				color: var(--el-color-primary);
			}
		}
	}
}
.toolbar {
	display: flex;
	align-items: center;
	justify-content: space-between;
}
.toolbar-left, .toolbar-right {
	display: flex;
	align-items: center;
}
.custom-file-table {
	border-radius: 8px;
	overflow: hidden;
	border: 1px solid var(--el-border-color-lighter);
	:deep(.el-table__header-wrapper) th {
		background-color: var(--el-fill-color-light);
		color: var(--el-text-color-primary);
		font-weight: 600;
	}
}
.file-item-name {
	display: flex;
	align-items: center;
	padding: 4px 0;
	.icon-box {
		width: 32px;
		height: 32px;
		border-radius: 6px;
		display: flex;
		align-items: center;
		justify-content: center;
		font-size: 18px;
		&.folder {
			background-color: #fef3e7;
			color: #e6a23c;
		}
		&.file {
			background-color: #e6f4ff;
			color: #1677ff;
		}
	}
	&.is-folder {
		color: var(--el-text-color-primary);
		font-weight: 500;
		cursor: pointer;
		&:hover {
			color: var(--el-color-primary);
			.icon-box.folder {
				background-color: var(--el-color-primary-light-9);
				color: var(--el-color-primary);
			}
		}
	}
}
.header-left {
	display: flex;
	align-items: center;
}
.header-icon-box {
	width: 28px;
	height: 28px;
	background-color: var(--el-color-primary-light-9);
	color: var(--el-color-primary);
	border-radius: 6px;
	display: flex;
	align-items: center;
	justify-content: center;
	font-size: 16px;
}
.selection-content {
	padding: 15px;
}
.custom-selection-table {
	border-radius: 8px;
	border: 1px solid var(--el-border-color-lighter);
	:deep(.el-table__header-wrapper) th {
		background-color: var(--el-fill-color-light);
		color: var(--el-text-color-primary);
		font-weight: 600;
	}
}
.selection-name-cell {
	display: flex;
	align-items: center;
	.name-icon {
		color: var(--el-color-primary);
		font-size: 16px;
	}
	.name-text {
		font-weight: 500;
		color: var(--el-text-color-primary);
	}
}
.items-count-badge {
	display: inline-flex;
	align-items: baseline;
	padding: 2px 10px;
	background-color: var(--el-fill-color-light);
	border-radius: 12px;
	.count-num {
		font-weight: 700;
		color: var(--el-color-primary);
		font-size: 14px;
	}
	.count-unit {
		font-size: 12px;
		color: var(--el-text-color-secondary);
		margin-left: 2px;
	}
}
.selected-file {
	font-size: 12px;
	color: var(--el-text-color-secondary);
}
.cursor-pointer {
	cursor: pointer;
}
.ml10 {
	margin-left: 10px;
}
.ml5 {
	margin-left: 5px;
}
.mt15 {
	margin-top: 15px;
}
.mt10 {
	margin-top: 10px;
}
.w100 {
	width: 100%;
}
</style>
