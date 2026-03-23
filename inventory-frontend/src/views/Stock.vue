<template>
  <div>
    <el-tabs>
      <el-tab-pane label="Inbound">
        <el-form :model="inboundForm" style="max-width: 400px; margin-top: 20px;">
          <el-form-item label="Part">
            <el-select v-model="inboundForm.partId">
              <el-option v-for="part in parts" :key="part.id" :label="part.name" :value="part.id" />
            </el-select>
          </el-form-item>
          <el-form-item label="Quantity">
            <el-input-number v-model="inboundForm.quantity" :min="1" />
          </el-form-item>
          <el-form-item label="Remark">
            <el-input v-model="inboundForm.remark" />
          </el-form-item>
          <el-button type="primary" @click="handleInbound">Submit</el-button>
        </el-form>
      </el-tab-pane>

      <el-tab-pane label="Outbound">
        <el-form :model="outboundForm" style="max-width: 400px; margin-top: 20px;">
          <el-form-item label="Part">
            <el-select v-model="outboundForm.partId">
              <el-option v-for="part in parts" :key="part.id" :label="part.name" :value="part.id" />
            </el-select>
          </el-form-item>
          <el-form-item label="Quantity">
            <el-input-number v-model="outboundForm.quantity" :min="1" />
          </el-form-item>
          <el-form-item label="Remark">
            <el-input v-model="outboundForm.remark" />
          </el-form-item>
          <el-button type="primary" @click="handleOutbound">Submit</el-button>
        </el-form>
      </el-tab-pane>

      <el-tab-pane label="Lock">
        <el-form :model="lockForm" style="max-width: 400px; margin-top: 20px;">
          <el-form-item label="Part">
            <el-select v-model="lockForm.partId">
              <el-option v-for="part in parts" :key="part.id" :label="part.name" :value="part.id" />
            </el-select>
          </el-form-item>
          <el-form-item label="Quantity">
            <el-input-number v-model="lockForm.quantity" :min="1" />
          </el-form-item>
          <el-form-item label="Remark">
            <el-input v-model="lockForm.remark" />
          </el-form-item>
          <el-button type="primary" @click="handleLock">Submit</el-button>
        </el-form>
      </el-tab-pane>

      <el-tab-pane label="Unlock">
        <el-form :model="unlockForm" style="max-width: 400px; margin-top: 20px;">
          <el-form-item label="Part">
            <el-select v-model="unlockForm.partId">
              <el-option v-for="part in parts" :key="part.id" :label="part.name" :value="part.id" />
            </el-select>
          </el-form-item>
          <el-form-item label="Quantity">
            <el-input-number v-model="unlockForm.quantity" :min="1" />
          </el-form-item>
          <el-form-item label="Remark">
            <el-input v-model="unlockForm.remark" />
          </el-form-item>
          <el-button type="primary" @click="handleUnlock">Submit</el-button>
        </el-form>
      </el-tab-pane>
    </el-tabs>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { getParts } from '@/api/parts'
import { inbound, outbound, lock, unlock } from '@/api/stock'

const parts = ref([])
const inboundForm = ref({ partId: '', quantity: 1, remark: '' })
const outboundForm = ref({ partId: '', quantity: 1, remark: '' })
const lockForm = ref({ partId: '', quantity: 1, remark: '' })
const unlockForm = ref({ partId: '', quantity: 1, remark: '' })

onMounted(async () => {
  try {
    const res = await getParts()
    parts.value = res.data
  } catch (error) {
    ElMessage.error('Failed to load parts')
  }
})

const handleInbound = async () => {
  try {
    await inbound(inboundForm.value)
    ElMessage.success('Inbound successful')
    inboundForm.value = { partId: '', quantity: 1, remark: '' }
  } catch (error) {
    ElMessage.error(error.response?.data?.message || 'Inbound failed')
  }
}

const handleOutbound = async () => {
  try {
    await outbound(outboundForm.value)
    ElMessage.success('Outbound successful')
    outboundForm.value = { partId: '', quantity: 1, remark: '' }
  } catch (error) {
    ElMessage.error(error.response?.data?.message || 'Outbound failed')
  }
}

const handleLock = async () => {
  try {
    await lock(lockForm.value)
    ElMessage.success('Lock successful')
    lockForm.value = { partId: '', quantity: 1, remark: '' }
  } catch (error) {
    ElMessage.error(error.response?.data?.message || 'Lock failed')
  }
}

const handleUnlock = async () => {
  try {
    await unlock(unlockForm.value)
    ElMessage.success('Unlock successful')
    unlockForm.value = { partId: '', quantity: 1, remark: '' }
  } catch (error) {
    ElMessage.error(error.response?.data?.message || 'Unlock failed')
  }
}
</script>
