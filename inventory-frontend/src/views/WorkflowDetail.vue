<template>
  <div class="page-main">
    <div style="padding: 20px 24px;">
      <div class="panel-card" style="margin-bottom: 16px;">
        <div style="padding: 12px 16px; display: flex; align-items: center; gap: 16px;">
          <el-page-header @back="goBack" content="流程详情" />
        </div>
      </div>

      <div class="panel-card" style="margin-bottom: 16px;">
        <el-descriptions :column="2" border>
          <el-descriptions-item label="流程名称">{{ instance.name }}</el-descriptions-item>
          <el-descriptions-item label="状态">
            <el-tag :type="getStatusType(instance.status)">{{ instance.status }}</el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="发起人">{{ instance.startedByName || instance.startedBy }}</el-descriptions-item>
          <el-descriptions-item label="发起时间">{{ formatDate(instance.startedAt) }}</el-descriptions-item>
          <el-descriptions-item label="当前节点">{{ instance.currentNodeName }}</el-descriptions-item>
          <el-descriptions-item label="完成时间">{{ formatDate(instance.completedAt) }}</el-descriptions-item>
        </el-descriptions>
      </div>

      <div class="panel-card" style="margin-bottom: 16px;">
        <div
          style="padding: 12px 16px; font-size: 14px; font-weight: 600; border-bottom: 1px solid var(--color-border);">
          流程进度</div>
        <div style="padding: 16px;">
          <el-steps :active="currentStepIndex" finish-status="success">
            <el-step v-for="node in nodes" :key="node.id" :title="node.name" />
          </el-steps>
        </div>
      </div>

      <div class="panel-card" style="margin-bottom: 16px;">
        <div
          style="padding: 12px 16px; font-size: 14px; font-weight: 600; border-bottom: 1px solid var(--color-border);">
          审批历史</div>
        <el-timeline v-if="history.length > 0">
          <el-timeline-item v-for="item in history" :key="item.id" :timestamp="formatDate(item.createdAt)"
            :type="getActionType(item.action)">
            <p><strong>{{ item.nodeName }}</strong> - {{ item.action }}</p>
            <p>操作人: {{ item.operatorName || item.operatorId }}</p>
            <p v-if="item.comment">意见: {{ item.comment }}</p>
            <!-- 显示表单数据 -->
            <div v-if="item.formData && Object.keys(item.formData).length > 0"
              style="margin-top: 10px; padding: 10px; background-color: #f5f7fa; border-radius: 4px;">
              <p style="font-weight: bold; margin-bottom: 8px;">提交信息:</p>
              <div v-for="(value, key) in item.formData" :key="key" style="margin-bottom: 5px;">
                <span style="color: #666;">{{ key }}: </span>
                <span style="color: #333;">{{ value }}</span>
              </div>
            </div>
          </el-timeline-item>
        </el-timeline>
        <div v-else style="color: var(--color-text-muted); padding: 20px; text-align: center;">暂无审批记录</div>
      </div>

      <div v-if="relatedEntity" class="panel-card" style="margin-bottom: 16px;">
        <div
          style="padding: 12px 16px; font-size: 14px; font-weight: 600; border-bottom: 1px solid var(--color-border);">
          关联{{ instance.entityType === 'Project' ? '项目' : instance.entityType }}信息
        </div>
        <div style="padding: 16px;">
          <el-descriptions :column="2" border>
            <el-descriptions-item label="名称">{{ relatedEntity.name }}</el-descriptions-item>
            <el-descriptions-item label="类型">{{ relatedEntity.type === 'project' ? '项目' : '文件夹'
              }}</el-descriptions-item>
            <el-descriptions-item label="编号">{{ relatedEntity.code || '-' }}</el-descriptions-item>
            <el-descriptions-item label="描述">{{ relatedEntity.description || '-' }}</el-descriptions-item>
          </el-descriptions>
        </div>
      </div>

      <div v-if="instance.selectedFileIds?.length" class="panel-card" style="margin-bottom: 16px;">
        <div
          style="padding: 12px 16px; font-size: 14px; font-weight: 600; border-bottom: 1px solid var(--color-border);">
          关联文件 ({{ instance.selectedFileIds.length }})
        </div>
        <div
          style="padding: 16px; display: grid; grid-template-columns: repeat(auto-fill, minmax(120px, 1fr)); gap: 15px;">
          <div v-for="fileId in instance.selectedFileIds" :key="fileId"
            style="border: 1px solid #ddd; border-radius: 4px; padding: 12px; cursor: pointer; text-align: center; transition: all 0.3s;"
            @click="viewFileDetail(fileId)"
            @mouseenter="$event.currentTarget.style.boxShadow = '0 2px 8px rgba(0,0,0,0.1)'"
            @mouseleave="$event.currentTarget.style.boxShadow = 'none'">
            <div style="font-size: 24px; margin-bottom: 8px;">{{ getFileIcon(fileMetadataMap[fileId]?.fileName || '') }}
            </div>
            <div
              style="font-size: 12px; color: #666; word-break: break-all; overflow: hidden; text-overflow: ellipsis; display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical;">
              {{ fileMetadataMap[fileId]?.fileName || fileId }}
            </div>
          </div>
        </div>

        <!-- File Detail Dialog -->

        <el-dialog v-model="showFileDetailDialog" title="文件详情" width="500px">
          <el-descriptions v-if="selectedFileDetail" :column="1" border>
            <el-descriptions-item label="文件名">{{ selectedFileDetail.fileName }}</el-descriptions-item>
            <el-descriptions-item label="文件大小">{{ formatFileSize(selectedFileDetail.fileSize) }}</el-descriptions-item>
            <el-descriptions-item label="上传时间">{{ formatDate(selectedFileDetail.uploadedAt) }}</el-descriptions-item>
            <el-descriptions-item label="上传人">{{ selectedFileDetail.uploadedBy }}</el-descriptions-item>
          </el-descriptions>
          <template #footer>
            <el-button @click="showFileDetailDialog = false">关闭</el-button>
          </template>
        </el-dialog>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { getWorkflowInstance, getWorkflowHistory } from '../services/workflowApi'
import { getFileMetadata } from '../api/files'
import { getProject } from '../api/projects'

const route = useRoute()
const router = useRouter()

const instance = ref({})
const history = ref([])
const fileMetadataMap = ref({})
const showFileDetailDialog = ref(false)
const selectedFileDetail = ref(null)
const relatedEntity = ref(null) // 关联的业务实体信息
const nodes = computed(() => instance.value.definition?.nodes || [])
const currentStepIndex = computed(() => {
  if (instance.value.status === 'Completed') {
    return nodes.value.length  // 超出最后一个节点，所有节点都变成 finish 状态
  }
  return nodes.value.findIndex(n => n.id === instance.value.currentNodeId)
})

onMounted(async () => {
  await loadDetail()
  await loadHistory()
  await loadFileMetadata()
  await loadRelatedEntity()
})

const loadDetail = async () => {
  try {
    instance.value = (await getWorkflowInstance(route.params.id)).data
  } catch (error) {
    ElMessage.error('加载流程详情失败: ' + error.message)
  }
}

// 加载关联的业务实体信息
const loadRelatedEntity = async () => {
  if (!instance.value.entityType || !instance.value.entityId) {
    relatedEntity.value = null
    return
  }

  try {
    if (instance.value.entityType === 'Project') {
      const entity = (await getProject(instance.value.entityId)).data
      // 确保返回的是项目而不是文件夹
      if (entity && entity.type === 'project') {
        relatedEntity.value = entity
      } else {
        console.warn('[loadRelatedEntity] entityId does not point to a project:', entity)
        relatedEntity.value = null
      }
    } else {
      relatedEntity.value = null
    }
    // 未来可以扩展其他实体类型
  } catch (error) {
    console.error('加载关联实体失败:', error)
    relatedEntity.value = null
  }
}

const loadHistory = async () => {
  try {
    history.value = (await getWorkflowHistory(route.params.id)).data || []
  } catch (error) {
    ElMessage.error('加载审批历史失败: ' + error.message)
  }
}

const loadFileMetadata = async () => {
  if (!instance.value.selectedFileIds?.length) return
  try {
    for (const fileId of instance.value.selectedFileIds) {
      fileMetadataMap.value[fileId] = (await getFileMetadata(fileId)).data
    }
  } catch (error) {
    console.error('加载文件元数据失败:', error)
  }
}

const goBack = () => router.back()

const getStatusType = (status) => {
  const types = { Running: 'info', Completed: 'success', Rejected: 'danger', Cancelled: 'warning' }
  return types[status] || 'info'
}

const getActionType = (action) => {
  const types = { Start: 'primary', Approve: 'success', Reject: 'danger' }
  return types[action] || 'info'
}

const formatDate = (date) => {
  if (!date) return '-'
  return new Date(date).toLocaleString('zh-CN')
}

const getFileIcon = (fileName) => {
  const ext = fileName.split('.').pop()?.toLowerCase() || ''
  const iconMap = {
    pdf: '📄',
    dwg: '📐', dxf: '📐',
    doc: '📝', docx: '📝',
    xls: '📊', xlsx: '📊',
    ppt: '📊', pptx: '📊',
    jpg: '🖼️', png: '🖼️', gif: '🖼️',
    zip: '📦', rar: '📦'
  }
  return iconMap[ext] || '📄'
}

const formatFileSize = (bytes) => {
  if (!bytes) return '-'
  if (bytes < 1024) return bytes + ' B'
  if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(2) + ' KB'
  return (bytes / (1024 * 1024)).toFixed(2) + ' MB'
}

const viewFileDetail = (fileId) => {
  selectedFileDetail.value = fileMetadataMap.value[fileId]
  showFileDetailDialog.value = true
}
</script>

<style scoped></style>
