import request from '/@/utils/request';

export type UserItem = {
	id: string;
	username: string;
	displayName: string;
	email: string;
	role: string;
	isActive: boolean;
};

export function getUsers() {
	return request({
		url: '/api/users',
		method: 'get',
	});
}

export function getUser(id: string) {
	return request({
		url: `/api/users/${id}`,
		method: 'get',
	});
}

export function createUser(data: any) {
	return request({
		url: '/api/auth/register',
		method: 'post',
		data,
	});
}

export function updateUser(id: string, data: any) {
	return request({
		url: `/api/users/${id}`,
		method: 'put',
		data,
	});
}

export function deleteUser(id: string) {
	return request({
		url: `/api/users/${id}`,
		method: 'delete',
	});
}

export function resetPassword(id: string, data: any) {
	return request({
		url: `/api/users/${id}/reset-password`,
		method: 'post',
		data,
	});
}
