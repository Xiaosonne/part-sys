import request from './request'

export const uploadFile = (formData) => {
  return request.post('/files/upload', formData, {
    headers: { 'Content-Type': 'multipart/form-data' }
  })
}

export const getPartFiles = (partId) => {
  return request.get(`/files/part/${partId}`)
}

export const getProjectFiles = (projectId) => {
  return request.get(`/files/project/${projectId}`)
}

export const deleteFile = (fileId) => {
  return request.delete(`/files/${fileId}`)
}

export const listFiles = (bucket, path = null, relatedId = null) => {
  const params = { bucket }
  if (path) params.path = path
  if (relatedId) params.relatedId = relatedId
  return request.get('/files/list', { params })
}

export const createFolder = (bucket, relatedId, folderPath) => {
  return request.post('/files/folder', null, {
    params: { bucket, relatedId, folderPath }
  })
}

export const deleteFolder = (bucket, relatedId, folderPath) => {
  return request.delete('/files/folder', {
    params: { bucket, relatedId, folderPath }
  })
}

export const reinitializeProjectWorkspace = (projectId) => {
  return request.post(`/projects/${projectId}/reinitialize-workspace`)
}

export const getFileMetadata = (fileId) => {
  return request.get(`/files/${fileId}`)
}

