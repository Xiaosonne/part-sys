<template>
  <el-container style="height: 100vh;">
    <el-header style="background-color: #545c64; color: white; display: flex; justify-content: space-between; align-items: center;">
      <div style="font-size: 20px; font-weight: bold;">Inventory System</div>
      <div>
        <span style="margin-right: 20px;">{{ user?.username }} ({{ user?.role }})</span>
        <el-button type="danger" size="small" @click="handleLogout">Logout</el-button>
      </div>
    </el-header>
    <el-container>
      <el-aside width="200px" style="background-color: #f5f7fa;">
        <el-menu :default-active="activeMenu" router>
          <el-menu-item v-if="canViewParts" index="/parts">Parts</el-menu-item>
          <el-menu-item v-if="canViewParts" index="/parts/search">Part Search</el-menu-item>
          <el-menu-item v-if="canViewParts" index="/spec-templates">Spec Templates</el-menu-item>
          <el-menu-item v-if="canViewStock" index="/stock">Stock Operations</el-menu-item>
          <el-menu-item index="/transactions">Transactions</el-menu-item>
          <el-menu-item index="/projects">Projects</el-menu-item>
          <el-menu-item index="/selections">Selections</el-menu-item>
          <el-submenu index="/workflows" title="Workflows">
            <el-menu-item index="/workflows/start">Start Workflow</el-menu-item>
            <el-menu-item index="/workflows/designer" v-if="user?.role === 'admin'">Designer</el-menu-item>
            <el-menu-item index="/workflows/pending">Pending Tasks</el-menu-item>
            <el-menu-item index="/workflows/my">My Workflows</el-menu-item>
          </el-submenu>
          <el-menu-item v-if="canViewUsers" index="/users">Users</el-menu-item>
        </el-menu>
      </el-aside>
      <el-main>
        <router-view />
      </el-main>
    </el-container>
  </el-container>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { ElMessage } from 'element-plus'
import { getUser, clearAuth } from '@/utils/auth'

const router = useRouter()
const route = useRoute()
const user = ref(null)
const activeMenu = ref('/parts')

const canViewParts = computed(() => ['admin', 'warehouse'].includes(user.value?.role))
const canViewStock = computed(() => ['admin', 'warehouse'].includes(user.value?.role))
const canViewUsers = computed(() => user.value?.role === 'admin')

onMounted(() => {
  user.value = getUser()
  activeMenu.value = route.path
})

const handleLogout = () => {
  clearAuth()
  ElMessage.success('Logged out')
  router.push('/login')
}
</script>
