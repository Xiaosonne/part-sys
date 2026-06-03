import request from '/@/utils/request';

export function getProjects(parentId?: string | null) {
	return request({
		url: '/api/projects',
		method: 'get',
		params: parentId ? { parentId } : undefined,
	});
}

export function getProject(id: string) {
	return request({
		url: `/api/projects/${id}`,
		method: 'get',
	});
}

export function createProject(data: any) {
	return request({
		url: '/api/projects',
		method: 'post',
		data,
	});
}

export function updateProject(id: string, data: any) {
	return request({
		url: `/api/projects/${id}`,
		method: 'put',
		data,
	});
}

export function deleteProject(id: string) {
	return request({
		url: `/api/projects/${id}`,
		method: 'delete',
	});
}

// 文件相关
export function listFiles(bucket: string, path: string | null = null, relatedId: string | null = null) {
	return request({
		url: '/api/files/list',
		method: 'get',
		params: { bucket, path, relatedId },
	});
}

export function uploadFile(formData: FormData) {
	return request({
		url: '/api/files/upload',
		method: 'post',
		data: formData,
	});
}

export function createFolder(bucket: string, relatedId: string, folderPath: string) {
	return request({
		url: '/api/files/folder',
		method: 'post',
		params: { bucket, relatedId, folderPath },
	});
}

export function deleteFile(fileId: string) {
	return request({
		url: `/api/files/${fileId}`,
		method: 'delete',
	});
}

export function deleteFolder(bucket: string, relatedId: string, folderPath: string) {
	return request({
		url: '/api/files/folder',
		method: 'delete',
		params: { bucket, relatedId, folderPath },
	});
}

export function reinitializeProjectWorkspace(projectId: string) {
	return request({
		url: `/api/projects/${projectId}/reinitialize-workspace`,
		method: 'post',
	});
}
