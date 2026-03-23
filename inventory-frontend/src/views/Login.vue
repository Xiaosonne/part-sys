<template>
  <div style="display: flex; justify-content: center; align-items: center; height: 100vh;">
    <el-card style="width: 400px;">
      <template #header>
        <div style="font-size: 20px; font-weight: bold;">Login</div>
      </template>
      <el-form :model="form" @submit.prevent="handleLogin">
        <el-form-item label="Username">
          <el-input v-model="form.username" />
        </el-form-item>
        <el-form-item label="Password">
          <el-input v-model="form.password" type="password" />
        </el-form-item>
        <el-button type="primary" @click="handleLogin" style="width: 100%;">Login</el-button>
      </el-form>
    </el-card>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { login } from '@/api/auth'
import { setToken, setUser } from '@/utils/auth'

const router = useRouter()
const form = ref({ username: '', password: '' })

const handleLogin = async () => {
  try {
    const res = await login(form.value.username, form.value.password)
    setToken(res.data.token)
    setUser(res.data.user)
    ElMessage.success('Login successful')
    router.push('/parts')
  } catch (error) {
    ElMessage.error(error.response?.data?.message || 'Login failed')
  }
}
</script>
