<template>
	<div class="system-user-dialog-container">
		<el-dialog :title="state.dialog.title" v-model="state.dialog.isShowDialog" width="480px">
			<el-form ref="userDialogFormRef" :model="state.ruleForm" size="default" label-width="90px">
				<el-form-item label="用户名" v-if="state.dialog.type === 'add'">
					<el-input v-model="state.ruleForm.username" placeholder="请输入登录用户名" clearable></el-input>
				</el-form-item>
				<el-form-item label="显示名">
					<el-input v-model="state.ruleForm.displayName" placeholder="请输入显示名称" clearable></el-input>
				</el-form-item>
				<el-form-item label="邮箱">
					<el-input v-model="state.ruleForm.email" placeholder="请输入邮箱地址" clearable></el-input>
				</el-form-item>
				<el-form-item label="角色">
					<el-select v-model="state.ruleForm.role" placeholder="请选择角色" clearable class="w100">
						<el-option label="管理员" value="admin"></el-option>
						<el-option label="仓库" value="warehouse"></el-option>
						<el-option label="用户" value="user"></el-option>
					</el-select>
				</el-form-item>
				<el-form-item label="初始密码" v-if="state.dialog.type === 'add'">
					<el-input v-model="state.ruleForm.password" type="password" placeholder="请输入初始密码" clearable></el-input>
				</el-form-item>
				<el-form-item label="用户状态">
					<el-switch v-model="state.ruleForm.isActive" inline-prompt active-text="启" inactive-text="禁"></el-switch>
				</el-form-item>
			</el-form>
			<template #footer>
				<span class="dialog-footer">
					<el-button @click="onCancel" size="default">取 消</el-button>
					<el-button type="primary" @click="onSubmit" size="default" :loading="state.loading">{{ state.dialog.submitTxt }}</el-button>
				</span>
			</template>
		</el-dialog>
	</div>
</template>

<script setup lang="ts" name="systemUserDialog">
import { reactive, ref } from 'vue';
import { ElMessage } from 'element-plus';
import { createUser, updateUser } from '/@/api/system';

// 定义子组件向父组件传值/事件
const emit = defineEmits(['refresh']);

// 定义变量内容
const userDialogFormRef = ref();
const state = reactive({
	ruleForm: {
		id: '',
		username: '',
		displayName: '',
		email: '',
		role: 'user',
		isActive: true,
		password: '',
	},
	dialog: {
		isShowDialog: false,
		type: '',
		title: '',
		submitTxt: '',
	},
	loading: false,
});

// 打开弹窗
const openDialog = (type: string, row?: any) => {
	state.dialog.type = type;
	if (type === 'edit') {
		state.ruleForm = { ...row, password: '' };
		state.dialog.title = '修改用户';
		state.dialog.submitTxt = '修 改';
	} else {
		state.ruleForm = {
			id: '',
			username: '',
			displayName: '',
			email: '',
			role: 'user',
			isActive: true,
			password: '',
		};
		state.dialog.title = '新增用户';
		state.dialog.submitTxt = '新 增';
	}
	state.dialog.isShowDialog = true;
};
// 关闭弹窗
const closeDialog = () => {
	state.dialog.isShowDialog = false;
};
// 取消
const onCancel = () => {
	closeDialog();
};
// 提交
const onSubmit = async () => {
	state.loading = true;
	try {
		if (state.dialog.type === 'edit') {
			await updateUser(state.ruleForm.id, state.ruleForm);
			ElMessage.success('用户修改成功');
		} else {
			await createUser(state.ruleForm);
			ElMessage.success('用户新增成功');
		}
		closeDialog();
		emit('refresh');
	} catch (err: any) {
		ElMessage.error(err.message || '操作失败');
	} finally {
		state.loading = false;
	}
};

// 暴露变量
defineExpose({
	openDialog,
});
</script>
