import request from '@/api/request'

const API_BASE = '/workflows'

export const createWorkflowDefinition = async (definition) => {
  return request.post(`${API_BASE}/definitions`, definition)
}

export const getWorkflowDefinitions = async (category) => {
  return request.get(`${API_BASE}/definitions`, { params: { category } })
}

export const getWorkflowDefinition = async (id) => {
  return request.get(`${API_BASE}/definitions/${id}`)
}

export const updateWorkflowDefinition = async (id, definition) => {
  return request.put(`${API_BASE}/definitions/${id}`, definition)
}

export const deleteWorkflowDefinition = async (id) => {
  return request.delete(`${API_BASE}/definitions/${id}`)
}

export const startWorkflowInstance = async (definitionId, entityType, entityId, selectedFileIds, name, formData) => {
  return request.post(`${API_BASE}/instances`, {
    definitionId,
    entityType,
    entityId,
    selectedFileIds,
    name,
    formData
  })
}

export const getWorkflowInstance = async (id) => {
  return request.get(`${API_BASE}/instances/${id}`)
}

export const getMyWorkflowInstances = async () => {
  return request.get(`${API_BASE}/instances`)
}

export const cancelWorkflowInstance = async (id) => {
  return request.post(`${API_BASE}/instances/${id}/cancel`)
}

export const getPendingTasks = async () => {
  return request.get(`${API_BASE}/tasks/pending`)
}

export const getHistoryTasks = async () => {
  return request.get(`${API_BASE}/tasks/history`)
}

export const approveTask = async (taskId, comment, formData) => {
  return request.post(`${API_BASE}/tasks/${taskId}/approve`, {
    comment,
    formData
  })
}

export const rejectTask = async (taskId, comment) => {
  return request.post(`${API_BASE}/tasks/${taskId}/reject`, {
    comment
  })
}

export const getProjectFileTree = async (projectId) => {
  return request.get(`${API_BASE}/projects/${projectId}/files`)
}

export const getWorkflowHistory = async (instanceId) => {
  return request.get(`${API_BASE}/instances/${instanceId}/history`)
}
