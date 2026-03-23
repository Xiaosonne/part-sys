import request from './request'

export function getSelections(projectId) {
  return request.get('/selections', { params: { projectId } })
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

export function matchParts(id, itemId) {
  return request.get(`/selections/${id}/match`, { params: { itemId } })
}
