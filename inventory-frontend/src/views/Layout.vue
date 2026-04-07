<template>
  <el-container style="height: 100vh;">
    <el-header style="background-color: #545c64; color: white; display: flex; align-items: stretch;">
      <div style="font-size: 20px; font-weight: bold; display: flex; align-items: center; padding: 0 40px 0 20px;">Inventory System</div>
      <el-menu :default-active="activeMenu" router mode="horizontal" style="flex: 1; background-color: transparent; border: none; display: flex; align-items: center;">
        <el-menu-item v-if="canViewParts" index="/parts/manage">配件管理</el-menu-item>
        <el-menu-item v-if="canViewParts" index="/spec-templates">规格模板</el-menu-item>
        <el-menu-item v-if="canViewStock" index="/stock">库存操作</el-menu-item>
        <el-menu-item index="/transactions">Transactions</el-menu-item>
        <el-menu-item index="/projects">Projects</el-menu-item>
        <el-menu-item index="/selections">Selections</el-menu-item>
        <el-menu-item index="/purchase-tasks">采购任务</el-menu-item>
        <el-dropdown @command="handleWorkflowCommand" trigger="hover">
          <span class="workflow-dropdown">
            Workflows<i class="el-icon-arrow-down el-icon--right"></i>
          </span>
          <template #dropdown>
            <el-dropdown-menu>
              <el-dropdown-item command="/workflows/start">Start Workflow</el-dropdown-item>
              <el-dropdown-item command="/workflows/designer" v-if="user?.role === 'admin'">Designer</el-dropdown-item>
              <el-dropdown-item command="/workflows/pending">Pending Tasks</el-dropdown-item>
              <el-dropdown-item command="/workflows/my">My Workflows</el-dropdown-item>
            </el-dropdown-menu>
          </template>
        </el-dropdown>
        <el-menu-item v-if="canViewUsers" index="/users">Users</el-menu-item>
      </el-menu>
      <div style="display: flex; align-items: center; margin-left: auto; padding: 0 20px;">
        <span style="margin-right: 20px;">{{ user?.username }} ({{ user?.role }})</span>
        <el-button type="danger" size="small" @click="handleLogout">Logout</el-button>
      </div>
    </el-header>
    <el-main>
      <router-view />
    </el-main>
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

const handleWorkflowCommand = (command) => {
  router.push(command)
}
</script>

<style scoped>
.el-header {
  padding: 0;
}
.workflow-dropdown {
  display: flex;
  align-items: center;
  padding: 0 20px;
  color: #fff;
  cursor: pointer;
  height: 60px;
  font-size: 14px;
}
.workflow-dropdown:hover {
  background-color: rgba(255, 255, 255, 0.1);
}
</style>
