<template>
  <div>
    <el-button type="primary" @click="showDialog = true" style="margin-bottom: 20px;">Add User</el-button>
    <el-table :data="users">
      <el-table-column prop="username" label="Username" />
      <el-table-column prop="displayName" label="Display Name" />
      <el-table-column prop="email" label="Email" />
      <el-table-column prop="role" label="Role" width="100" />
      <el-table-column prop="isActive" label="Status" width="80">
        <template #default="{ row }">
          <el-tag :type="row.isActive ? 'success' : 'danger'">{{ row.isActive ? 'Active' : 'Inactive' }}</el-tag>
        </template>
      </el-table-column>
      <el-table-column label="Actions" width="200">
        <template #default="{ row }">
          <el-button size="small" @click="editUser(row)">Edit</el-button>
          <el-button size="small" @click="resetPassword(row)">Reset Pwd</el-button>
          <el-button size="small" type="danger" @click="deleteUser(row.id)">Delete</el-button>
        </template>
      </el-table-column>
    </el-table>

    <!-- Add/Edit Dialog -->
    <el-dialog v-model="showDialog" :title="editingId ? 'Edit User' : 'Add User'" width="500px">
      <el-form :model="form">
        <el-form-item label="Username" v-if="!editingId">
          <el-input v-model="form.username" />
        </el-form-item>
        <el-form-item label="Display Name">
          <el-input v-model="form.displayName" />
        </el-form-item>
        <el-form-item label="Email">
          <el-input v-model="form.email" />
        </el-form-item>
        <el-form-item label="Role">
          <el-select v-model="form.role">
            <el-option label="admin" value="admin" />
            <el-option label="warehouse" value="warehouse" />
            <el-option label="user" value="user" />
          </el-select>
        </el-form-item>
        <el-form-item label="Status">
          <el-switch v-model="form.isActive" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showDialog = false">Cancel</el-button>
        <el-button type="primary" @click="saveUser">Save</el-button>
      </template>
    </el-dialog>

    <!-- Reset Password Dialog -->
    <el-dialog v-model="showResetDialog" title="Reset Password" width="400px">
      <el-form :model="resetForm">
        <el-form-item label="New Password">
          <el-input v-model="resetForm.newPassword" type="password" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showResetDialog = false">Cancel</el-button>
        <el-button type="primary" @click="confirmReset">Reset</el-button>
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
const form = ref({ username: '', displayName: '', email: '', role: 'user', isActive: true })
const resetForm = ref({ newPassword: '' })

onMounted(async () => {
  await loadUsers()
})

const loadUsers = async () => {
  try {
    const res = await getUsers()
    users.value = res.data
  } catch (error) {
    ElMessage.error('Failed to load users')
  }
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
      ElMessage.success('User updated')
    } else {
      await createUser(form.value)
      ElMessage.success('User created')
    }
    showDialog.value = false
    editingId.value = null
    form.value = { username: '', displayName: '', email: '', role: 'user', isActive: true }
    await loadUsers()
  } catch (error) {
    ElMessage.error('Save failed')
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
    ElMessage.success('Password reset')
    showResetDialog.value = false
    await loadUsers()
  } catch (error) {
    ElMessage.error('Reset failed')
  }
}

const deleteUser = async (id) => {
  try {
    await ElMessageBox.confirm('Delete this user?', 'Warning', { type: 'warning' })
    await deleteUserApi(id)
    ElMessage.success('User deleted')
    await loadUsers()
  } catch (error) {
    if (error.message !== 'cancel') {
      ElMessage.error('Delete failed')
    }
  }
}
</script>
