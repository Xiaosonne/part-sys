import request from './request'

export function getSelections(projectId) {
  return request.get('/selections', { params: { projectId } })
}

export function getSelection(id) {
  return request.get(`/selections/${id}`)
}

export function createSelection(data) {
  return request.post('/selections', data)
}

export function updateSelection(id, data) {
  return request.put(`/selections/${id}`, data)
}

export function deleteSelection(id) {
  return request.delete(`/selections/${id}`)
}

export function submitSelection(id) {
  return request.post(`/selections/${id}/submit`)
}

export function outboundSelection(planId, itemId, qty, recipientId, recipientName) {
  return request.post(`/selections/${planId}/items/${itemId}/outbound`, null, {
    params: { qty, recipientId, recipientName }
  })
}

export function cancelSelection(id) {
  return request.post(`/selections/${id}/cancel`)
}

export function matchParts(id, itemId) {
  return request.get(`/selections/${id}/match`, { params: { itemId } })
}
