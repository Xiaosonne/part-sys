<template>
  <div class="page-main">
    <div style="padding: 20px 24px;">
      <el-tabs>
        <el-tab-pane label="入库">
          <div class="form-section">
            <div class="form-section-title">配件入库</div>
            <el-form :model="inboundForm" label-width="80px">
              <el-form-item label="配件">
                <el-select v-model="inboundForm.partId" placeholder="选择配件" style="width: 100%;">
                  <el-option v-for="part in parts" :key="part.id" :label="part.name + ' - ' + part.model"
                    :value="part.id" />
                </el-select>
              </el-form-item>
              <el-form-item label="数量">
                <el-input-number v-model="inboundForm.quantity" :min="1" />
              </el-form-item>
              <el-form-item label="备注">
                <el-input v-model="inboundForm.remark" placeholder="可选" />
              </el-form-item>
              <el-form-item>
                <el-button type="primary" @click="handleInbound">提交入库</el-button>
              </el-form-item>
            </el-form>
          </div>
        </el-tab-pane>

        <el-tab-pane label="出库">
          <div class="form-section">
            <div class="form-section-title">配件出库</div>
            <el-form :model="outboundForm" label-width="80px">
              <el-form-item label="配件">
                <el-select v-model="outboundForm.partId" placeholder="选择配件" style="width: 100%;">
                  <el-option v-for="part in parts" :key="part.id" :label="part.name + ' - ' + part.model"
                    :value="part.id" />
                </el-select>
              </el-form-item>
              <el-form-item label="数量">
                <el-input-number v-model="outboundForm.quantity" :min="1" />
              </el-form-item>
              <el-form-item label="备注">
                <el-input v-model="outboundForm.remark" placeholder="可选" />
              </el-form-item>
              <el-form-item>
                <el-button type="primary" @click="handleOutbound">提交出库</el-button>
              </el-form-item>
            </el-form>
          </div>
        </el-tab-pane>

        <el-tab-pane label="锁定">
          <div class="form-section">
            <div class="form-section-title">锁定配件</div>
            <el-form :model="lockForm" label-width="80px">
              <el-form-item label="配件">
                <el-select v-model="lockForm.partId" placeholder="选择配件" style="width: 100%;">
                  <el-option v-for="part in parts" :key="part.id" :label="part.name + ' - ' + part.model"
                    :value="part.id" />
                </el-select>
              </el-form-item>
              <el-form-item label="数量">
                <el-input-number v-model="lockForm.quantity" :min="1" />
              </el-form-item>
              <el-form-item label="备注">
                <el-input v-model="lockForm.remark" placeholder="可选" />
              </el-form-item>
              <el-form-item>
                <el-button type="primary" @click="handleLock">提交锁定</el-button>
              </el-form-item>
            </el-form>
          </div>
        </el-tab-pane>

        <el-tab-pane label="解锁">
          <div class="form-section">
            <div class="form-section-title">解锁配件</div>
            <el-form :model="unlockForm" label-width="80px">
              <el-form-item label="配件">
                <el-select v-model="unlockForm.partId" placeholder="选择配件" style="width: 100%;">
                  <el-option v-for="part in parts" :key="part.id" :label="part.name + ' - ' + part.model"
                    :value="part.id" />
                </el-select>
              </el-form-item>
              <el-form-item label="数量">
                <el-input-number v-model="unlockForm.quantity" :min="1" />
              </el-form-item>
              <el-form-item label="备注">
                <el-input v-model="unlockForm.remark" placeholder="可选" />
              </el-form-item>
              <el-form-item>
                <el-button type="primary" @click="handleUnlock">提交解锁</el-button>
              </el-form-item>
            </el-form>
          </div>
        </el-tab-pane>
      </el-tabs>
    </div>
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
    ElMessage.error('加载配件列表失败')
  }
})

const handleInbound = async () => {
  try {
    await inbound(inboundForm.value)
    ElMessage.success('入库成功')
    inboundForm.value = { partId: '', quantity: 1, remark: '' }
  } catch (error) {
    ElMessage.error(error.response?.data?.message || '入库失败')
  }
}

const handleOutbound = async () => {
  try {
    await outbound(outboundForm.value)
    ElMessage.success('出库成功')
    outboundForm.value = { partId: '', quantity: 1, remark: '' }
  } catch (error) {
    ElMessage.error(error.response?.data?.message || '出库失败')
  }
}

const handleLock = async () => {
  try {
    await lock(lockForm.value)
    ElMessage.success('锁定成功')
    lockForm.value = { partId: '', quantity: 1, remark: '' }
  } catch (error) {
    ElMessage.error(error.response?.data?.message || '锁定失败')
  }
}

const handleUnlock = async () => {
  try {
    await unlock(unlockForm.value)
    ElMessage.success('解锁成功')
    unlockForm.value = { partId: '', quantity: 1, remark: '' }
  } catch (error) {
    ElMessage.error(error.response?.data?.message || '解锁失败')
  }
}
</script>
