import request from '/@/utils/request';

export type AccessoryCategoryTreeItem = {
	id: string;
	name: string;
	path: string;
	parentId?: string | null;
	specTemplateId?: string | null;
	sortOrder?: number;
	children?: AccessoryCategoryTreeItem[];
};

export type AccessorySpecValue = {
	key: string;
	label: string;
	value: string;
	unit: string;
};

export type AccessoryParamDef = {
	key: string;
	label: string;
	unit: string;
	dataType: string;
	options?: string[] | null;
	required?: boolean;
};

export type AccessorySpecTemplate = {
	id: string;
	category: string;
	paramDefs: AccessoryParamDef[];
};

export type AccessoryItem = {
	id: string;
	name: string;
	model: string;
	description?: string;
	manufacturer?: string;
	brand: string;
	category: string;
	tags?: string[];
	specTemplateId?: string | null;
	specs?: AccessorySpecValue[];
	totalQty: number;
	availableQty: number;
	lockedQty?: number;
	createdAt?: string;
	updatedAt?: string;
};

export type AccessoryCategory = {
	id: string;
	name: string;
	path: string;
	parentId?: string | null;
	specTemplateId?: string | null;
	sortOrder?: number;
	specParams?: AccessoryParamDef[] | null;
	createdAt?: string;
};

export type AccessorySearchParams = {
	categoryPath?: string | null;
	category?: string | null;
	keyword?: string | null;
	specFilters?: Array<{ key: string; op: string; value: string }> | null;
	minAvailableQty?: number | null;
	maxAvailableQty?: number | null;
};

export type AccessoryFile = {
	id: string;
	fileName: string;
	fileSize: number;
	uploadedBy: string;
	uploadedAt: string;
};

export function getAccessoryCategoryTree() {
	return request({
		url: '/api/part-categories/tree',
		method: 'get',
	});
}

export function getAccessoryCategories() {
	return request({
		url: '/api/part-categories',
		method: 'get',
	});
}

export function getAccessoryCategoryById(id: string) {
	return request({
		url: `/api/part-categories/${id}`,
		method: 'get',
	});
}

export function getAccessoryCategoriesByParent(parentId: string | null) {
	return request({
		url: `/api/part-categories/parent/${parentId || 'root'}`,
		method: 'get',
	});
}

export function createAccessoryCategory(data: Partial<AccessoryCategory>) {
	return request({
		url: '/api/part-categories',
		method: 'post',
		data,
	});
}

export function updateAccessoryCategory(id: string, data: Partial<AccessoryCategory>) {
	return request({
		url: `/api/part-categories/${id}`,
		method: 'put',
		data,
	});
}

export function deleteAccessoryCategory(id: string) {
	return request({
		url: `/api/part-categories/${id}`,
		method: 'delete',
	});
}

export function getAccessorySpecTemplates() {
	return request({
		url: '/api/templates',
		method: 'get',
	});
}

export function getAccessorySpecTemplateById(id: string) {
	return request({
		url: `/api/templates/${id}`,
		method: 'get',
	});
}

export function createAccessorySpecTemplate(data: Partial<AccessorySpecTemplate>) {
	return request({
		url: '/api/templates',
		method: 'post',
		data,
	});
}

export function updateAccessorySpecTemplate(id: string, data: Partial<AccessorySpecTemplate>) {
	return request({
		url: `/api/templates/${id}`,
		method: 'put',
		data,
	});
}

export function deleteAccessorySpecTemplate(id: string) {
	return request({
		url: `/api/templates/${id}`,
		method: 'delete',
	});
}

export function searchAccessories(data: AccessorySearchParams) {
	return request({
		url: '/api/parts/search',
		method: 'post',
		data,
	});
}

export function createAccessory(data: Partial<AccessoryItem>) {
	return request({
		url: '/api/parts',
		method: 'post',
		data,
	});
}

export function updateAccessory(id: string, data: Partial<AccessoryItem>) {
	return request({
		url: `/api/parts/${id}`,
		method: 'put',
		data,
	});
}

export function deleteAccessory(id: string) {
	return request({
		url: `/api/parts/${id}`,
		method: 'delete',
	});
}

// Files API
export function uploadFile(formData: FormData) {
	return request({
		url: '/api/files/upload',
		method: 'post',
		data: formData,
		headers: { 'Content-Type': 'multipart/form-data' },
	});
}

export function getPartFiles(partId: string) {
	return request({
		url: `/api/files/part/${partId}`,
		method: 'get',
	});
}

export function deleteFile(fileId: string) {
	return request({
		url: `/api/files/${fileId}`,
		method: 'delete',
	});
}
