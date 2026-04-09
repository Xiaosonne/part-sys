<template>
  <div class="stock-layout">
    <el-tabs v-model="activeTab" class="stock-tabs" tab-position="left">
      <el-tab-pane label="库存总览" name="overview">
        <StockOverview v-if="activeTab === 'overview'" />
      </el-tab-pane>
      <el-tab-pane label="库存流水" name="history">
        <StockHistory v-if="activeTab === 'history'" />
      </el-tab-pane>
      <el-tab-pane label="锁定状态" name="locks">
        <StockLocks v-if="activeTab === 'locks'" />
      </el-tab-pane>
      <el-tab-pane label="入库" name="inbound">
        <InboundForm v-if="activeTab === 'inbound'" />
      </el-tab-pane>
      <el-tab-pane label="出库" name="outbound">
        <OutboundForm v-if="activeTab === 'outbound'" />
      </el-tab-pane>
      <el-tab-pane label="锁定" name="lock">
        <LockForm v-if="activeTab === 'lock'" />
      </el-tab-pane>
      <el-tab-pane label="解锁" name="unlock">
        <UnlockForm v-if="activeTab === 'unlock'" />
      </el-tab-pane>
    </el-tabs>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import StockOverview from './stock/StockOverview.vue'
import StockHistory from './stock/StockHistory.vue'
import StockLocks from './stock/StockLocks.vue'
import InboundForm from './stock/InboundForm.vue'
import OutboundForm from './stock/OutboundForm.vue'
import LockForm from './stock/LockForm.vue'
import UnlockForm from './stock/UnlockForm.vue'

const activeTab = ref('overview')
</script>

<style scoped>
.stock-layout {
  height: 100%;
  display: flex;
}

.stock-tabs {
  height: 100%;
  flex: 1;
  min-width: 0;
}

.stock-tabs :deep(.el-tabs__header) {
  width: 120px;
  height: fit-content;
  margin: 16px 0 16px 16px;
  background: var(--color-bg-card);
  border-right: none;
  border-radius: 12px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.06);
}

.stock-tabs :deep(.el-tabs__nav-wrap) {
  height: auto;
  display: flex;
  flex-direction: column;
}

.stock-tabs :deep(.el-tabs__nav-wrap::after) {
  display: none;
}

.stock-tabs :deep(.el-tabs__nav) {
  display: flex;
  flex-direction: column;
  padding: 12px 0;
}

.stock-tabs :deep(.el-tabs__item) {
  height: 48px;
  line-height: 48px;
  font-size: 14px;
  padding: 0 16px !important;
  border-radius: 8px;
  margin: 0 8px;
}

.stock-tabs :deep(.el-tabs__item.is-active) {
  color: var(--color-primary);
  background: rgba(24, 144, 255, 0.1);
}

.stock-tabs :deep(.el-tabs__active-bar) {
  display: none;
}

.stock-tabs :deep(.el-tabs__content) {
  flex: 1;
  overflow: hidden;
  padding: 16px 24px;
}

.stock-tabs :deep(.el-tab-pane) {
  height: 100%;
  overflow: hidden;
}
</style>
