import request from './request'

export const getUsers = () => {
  return request.get('/users')
}

export const getUser = (id) => {
  return request.get(`/users/${id}`)
}

export const createUser = (data) => {
  return request.post('/users', data)
}

export const updateUser = (id, data) => {
  return request.put(`/users/${id}`, data)
}

export const deleteUser = (id) => {
  return request.delete(`/users/${id}`)
}

export const resetPassword = (id, data) => {
  return request.post(`/users/${id}/reset-password`, data)
}
