import request from '/@/utils/request';

/**
 * 获取采购任务列表
 */
export function getPurchaseTasks() {
	return request({
		url: '/api/purchase-tasks',
		method: 'get',
	});
}

/**
 * 开始采购任务
 * @param id 任务ID
 */
export function startPurchaseTask(id: string) {
	return request({
		url: `/api/purchase-tasks/${id}/start`,
		method: 'post',
	});
}

/**
 * 确认到货
 * @param id 任务ID
 */
export function receivePurchaseTask(id: string) {
	return request({
		url: `/api/purchase-tasks/${id}/receive`,
		method: 'post',
	});
}

/**
 * 取消采购任务
 * @param id 任务ID
 */
export function cancelPurchaseTask(id: string) {
	return request({
		url: `/api/purchase-tasks/${id}/cancel`,
		method: 'post',
	});
}
