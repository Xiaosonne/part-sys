import request from './request'

export function getPurchaseTasks(params) {
  return request.get('/purchase-tasks', { params })
}

export function getPurchaseTask(id) {
  return request.get(`/purchase-tasks/${id}`)
}

export function getPurchaseTasksBySelection(selectionPlanId) {
  return request.get(`/purchase-tasks/selection/${selectionPlanId}`)
}

export function getPendingPurchaseTasks() {
  return request.get('/purchase-tasks/pending')
}

export function updatePurchaseTaskStatus(id, status) {
  return request.put(`/purchase-tasks/${id}/status`, { status })
}

export function receivePurchaseTask(id, data) {
  return request.post(`/purchase-tasks/${id}/receive`, data)
}

export function startPurchaseTask(id) {
  return request.post(`/purchase-tasks/${id}/start`)
}

export function cancelPurchaseTask(id) {
  return request.post(`/purchase-tasks/${id}/cancel`)
}
