import request from './request'

export function getPartCategories() {
  return request.get('/part-categories')
}

export function getPartCategoryTree() {
  return request.get('/part-categories/tree')
}

export function getPartCategoryById(id) {
  return request.get(`/part-categories/${id}`)
}

export function getPartCategoriesByParent(parentId) {
  return request.get(`/part-categories/parent/${parentId}`)
}

export function createPartCategory(data) {
  return request.post('/part-categories', data)
}

export function updatePartCategory(id, data) {
  return request.put(`/part-categories/${id}`, data)
}

export function deletePartCategory(id) {
  return request.delete(`/part-categories/${id}`)
}
