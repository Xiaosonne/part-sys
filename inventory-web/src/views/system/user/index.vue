<template>
	<div class="system-user-container layout-padding">
		<el-card shadow="hover" class="layout-padding-auto">
			<div class="system-user-search mb15">
				<div class="flex-between">
					<div class="table-title">用户列表</div>
					<el-button size="default" type="primary" class="ml10" @click="onOpenAddUser('add')">
						<el-icon>
							<ele-Plus />
						</el-icon>
						添加用户
					</el-button>
				</div>
			</div>
			<el-table :data="state.tableData.data" v-loading="state.tableData.loading" style="width: 100%">
				<el-table-column prop="username" label="用户名" min-width="120" show-overflow-tooltip></el-table-column>
				<el-table-column prop="displayName" label="显示名" min-width="120" show-overflow-tooltip></el-table-column>
				<el-table-column prop="email" label="邮箱" min-width="180" show-overflow-tooltip></el-table-column>
				<el-table-column prop="role" label="角色" width="100" align="center">
					<template #default="{ row }">
						<el-tag size="small" :type="row.role === 'admin' ? 'danger' : row.role === 'warehouse' ? 'warning' : 'info'">
							{{ row.role === 'admin' ? '管理员' : row.role === 'warehouse' ? '仓库' : '用户' }}
						</el-tag>
					</template>
				</el-table-column>
				<el-table-column prop="isActive" label="状态" width="80" align="center">
					<template #default="{ row }">
						<el-tag size="small" :type="row.isActive ? 'success' : 'danger'">{{ row.isActive ? '启用' : '禁用' }}</el-tag>
					</template>
				</el-table-column>
				<el-table-column label="操作" width="220" fixed="right" align="center">
					<template #default="{ row }">
						<el-button size="small" text type="primary" @click="onOpenEditUser('edit', row)">编辑</el-button>
						<el-button size="small" text type="primary" @click="onResetPassword(row)">重置密码</el-button>
						<el-button size="small" text type="danger" @click="onRowDel(row)">删除</el-button>
					</template>
				</el-table-column>
			</el-table>
		</el-card>
		<UserDialog ref="userDialogRef" @refresh="getTableData" />
		<el-dialog v-model="state.resetDialog.visible" title="重置密码" width="400px">
			<el-form :model="state.resetDialog.form" label-width="90px">
				<el-form-item label="新密码">
					<el-input v-model="state.resetDialog.form.newPassword" type="password" placeholder="请输入新密码" clearable></el-input>
				</el-form-item>
			</el-form>
			<template #footer>
				<span class="dialog-footer">
					<el-button @click="state.resetDialog.visible = false" size="default">取 消</el-button>
					<el-button type="primary" @click="onConfirmReset" size="default" :loading="state.resetDialog.loading">重 置</el-button>
				</span>
			</template>
		</el-dialog>
	</div>
</template>

<script setup lang="ts" name="systemUser">
import { defineAsyncComponent, reactive, onMounted, ref } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import { getUsers, deleteUser, resetPassword } from '/@/api/system';

// 引入组件
const UserDialog = defineAsyncComponent(() => import('/@/views/system/user/dialog.vue'));

// 定义变量内容
const userDialogRef = ref();
const state = reactive({
	tableData: {
		data: [],
		loading: false,
	},
	resetDialog: {
		visible: false,
		loading: false,
		userId: '',
		form: {
			newPassword: '',
		},
	},
});

// 初始化表格数据
const getTableData = async () => {
	state.tableData.loading = true;
	try {
		const res = await getUsers();
		state.tableData.data = res || [];
	} catch (err) {
		ElMessage.error('加载用户列表失败');
	} finally {
		state.tableData.loading = false;
	}
};

// 打开新增用户弹窗
const onOpenAddUser = (type: string) => {
	userDialogRef.value.openDialog(type);
};

// 打开修改用户弹窗
const onOpenEditUser = (type: string, row: any) => {
	userDialogRef.value.openDialog(type, row);
};

// 删除用户
const onRowDel = (row: any) => {
	ElMessageBox.confirm(`此操作将永久删除账户：“${row.username}”，是否继续?`, '提示', {
		confirmButtonText: '确认',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async () => {
			try {
				await deleteUser(row.id);
				getTableData();
				ElMessage.success('删除成功');
			} catch (err) {
				ElMessage.error('删除失败');
			}
		})
		.catch(() => {});
};

// 重置密码
const onResetPassword = (row: any) => {
	state.resetDialog.userId = row.id;
	state.resetDialog.form.newPassword = '';
	state.resetDialog.visible = true;
};

// 确认重置密码
const onConfirmReset = async () => {
	if (!state.resetDialog.form.newPassword) {
		ElMessage.warning('请输入新密码');
		return;
	}
	state.resetDialog.loading = true;
	try {
		await resetPassword(state.resetDialog.userId, state.resetDialog.form);
		ElMessage.success('密码重置成功');
		state.resetDialog.visible = false;
	} catch (err) {
		ElMessage.error('密码重置失败');
	} finally {
		state.resetDialog.loading = false;
	}
};

// 页面加载时
onMounted(() => {
	getTableData();
});
</script>

<style scoped lang="scss">
.system-user-container {
	:deep(.el-card__body) {
		display: flex;
		flex-direction: column;
		flex: 1;
		overflow: auto;
		.el-table {
			flex: 1;
		}
	}
}
.flex-between {
	display: flex;
	align-items: center;
	justify-content: space-between;
}
.table-title {
	font-size: 15px;
	font-weight: 600;
}
</style>
