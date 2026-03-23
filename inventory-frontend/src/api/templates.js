import request from './request'

export function getTemplates() {
  return request.get('/templates')
}

export function getTemplate(id) {
  return request.get(`/templates/${id}`)
}

export function getTemplateByCategory(category) {
  return request.get(`/templates/category/${category}`)
}

export function createTemplate(data) {
  return request.post('/templates', data)
}

export function updateTemplate(id, data) {
  return request.put(`/templates/${id}`, data)
}

export function deleteTemplate(id) {
  return request.delete(`/templates/${id}`)
}
