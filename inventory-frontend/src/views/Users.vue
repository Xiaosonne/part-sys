<template>
  <div class="page-main">
    <div style="padding: 20px 24px;">
      <div class="panel-card">
        <div
          style="padding: 16px 20px; display: flex; align-items: center; justify-content: space-between; border-bottom: 1px solid var(--color-border);">
          <span style="font-size: 14px; font-weight: 600; color: var(--color-text-primary);">用户列表</span>
          <el-button type="primary" size="small" @click="openAddDialog">+ 添加用户</el-button>
        </div>
        <el-table :data="users" stripe style="width: 100%;">
          <el-table-column prop="username" label="用户名" min-width="120" />
          <el-table-column prop="displayName" label="显示名" min-width="120" />
          <el-table-column prop="email" label="邮箱" min-width="180" />
          <el-table-column prop="role" label="角色" width="100">
            <template #default="{ row }">
              <el-tag size="small"
                :type="row.role === 'admin' ? 'danger' : row.role === 'warehouse' ? 'warning' : 'info'">
                {{ row.role === 'admin' ? '管理员' : row.role === 'warehouse' ? '仓库' : '用户' }}
              </el-tag>
            </template>
          </el-table-column>
          <el-table-column prop="isActive" label="状态" width="80">
            <template #default="{ row }">
              <el-tag size="small" :type="row.isActive ? 'success' : 'danger'">{{ row.isActive ? '启用' : '禁用' }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column label="操作" width="200" fixed="right">
            <template #default="{ row }">
              <el-button size="small" @click="editUser(row)">编辑</el-button>
              <el-button size="small" @click="resetPassword(row)">重置密码</el-button>
              <el-button size="small" type="danger" plain @click="deleteUser(row.id)">删除</el-button>
            </template>
          </el-table-column>
        </el-table>
      </div>
    </div>

    <!-- Add/Edit Dialog -->
    <el-dialog v-model="showDialog" :title="editingId ? '编辑用户' : '添加用户'" width="480px">
      <el-form :model="form" label-width="80px">
        <el-form-item label="用户名" v-if="!editingId">
          <el-input v-model="form.username" placeholder="登录用户名" />
        </el-form-item>
        <el-form-item label="密码" v-if="!editingId">
          <el-input v-model="form.password" type="password" placeholder="默认 123456" />
        </el-form-item>
        <el-form-item label="显示名">
          <el-input v-model="form.displayName" placeholder="显示名称" />
        </el-form-item>
        <el-form-item label="邮箱">
          <el-input v-model="form.email" placeholder="email@example.com" />
        </el-form-item>
        <el-form-item label="角色">
          <el-select v-model="form.role" style="width: 100%;">
            <el-option label="管理员" value="admin" />
            <el-option label="仓库" value="warehouse" />
            <el-option label="用户" value="user" />
          </el-select>
        </el-form-item>
        <el-form-item label="状态">
          <el-switch v-model="form.isActive" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showDialog = false">取消</el-button>
        <el-button type="primary" @click="saveUser">保存</el-button>
      </template>
    </el-dialog>

    <!-- Reset Password Dialog -->
    <el-dialog v-model="showResetDialog" title="重置密码" width="400px">
      <el-form :model="resetForm" label-width="90px">
        <el-form-item label="新密码">
          <el-input v-model="resetForm.newPassword" type="password" placeholder="请输入新密码" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showResetDialog = false">取消</el-button>
        <el-button type="primary" @click="confirmReset">重置</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { getUsers, createUser, updateUser, deleteUser as deleteUserApi, resetPassword as resetPasswordApi } from '@/api/users'

const users = ref([])
const showDialog = ref(false)
const showResetDialog = ref(false)
const editingId = ref(null)
const resetingUserId = ref(null)
const form = ref({ username: '', password: '', displayName: '', email: '', role: 'user', isActive: true })
const resetForm = ref({ newPassword: '' })

onMounted(async () => {
  await loadUsers()
})

const loadUsers = async () => {
  try {
    const res = await getUsers()
    users.value = res.data || []
  } catch (error) {
    ElMessage.error('加载用户列表失败')
  }
}

const openAddDialog = () => {
  editingId.value = null
  form.value = { username: '', password: '', displayName: '', email: '', role: 'user', isActive: true }
  showDialog.value = true
}

const editUser = (user) => {
  editingId.value = user.id
  form.value = { ...user }
  showDialog.value = true
}

const saveUser = async () => {
  try {
    if (editingId.value) {
      await updateUser(editingId.value, form.value)
      ElMessage.success('用户已更新')
    } else {
      await createUser(form.value)
      ElMessage.success('用户已添加')
    }
    showDialog.value = false
    editingId.value = null
    form.value = { username: '', password: '', displayName: '', email: '', role: 'user', isActive: true }
    await loadUsers()
  } catch (error) {
    ElMessage.error('保存失败')
  }
}

const resetPassword = (user) => {
  resetingUserId.value = user.id
  resetForm.value = { newPassword: '' }
  showResetDialog.value = true
}

const confirmReset = async () => {
  try {
    await resetPasswordApi(resetingUserId.value, resetForm.value)
    ElMessage.success('密码已重置')
    showResetDialog.value = false
    await loadUsers()
  } catch (error) {
    ElMessage.error('重置失败')
  }
}

const deleteUser = async (id) => {
  try {
    await ElMessageBox.confirm('确定删除该用户？', '警告', { type: 'warning' })
    await deleteUserApi(id)
    ElMessage.success('用户已删除')
    await loadUsers()
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error('删除失败')
    }
  }
}
</script>
