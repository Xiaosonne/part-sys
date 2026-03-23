import request from './request'

export function getParts() {
  return request.get('/parts')
}

export function searchParts(params) {
  return request.post('/parts/search', params)
}

export function createPart(data) {
  return request.post('/parts', data)
}

export function updatePart(id, data) {
  return request.put(`/parts/${id}`, data)
}

export function deletePart(id) {
  return request.delete(`/parts/${id}`)
}
