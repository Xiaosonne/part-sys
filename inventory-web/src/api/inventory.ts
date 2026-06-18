import request from '/@/utils/request';

export type PartItem = {
	id: string;
	name: string;
	model: string;
	category: string;
	totalQty: number;
	lockedQty: number;
	availableQty: number;
};

export type ProjectNode = {
	id: string;
	name: string;
	type?: string;
	parentId?: string | null;
};

export type SelectionPlan = {
	id: string;
	name: string;
	projectId: string;
	status?: any;
};

export type PurchaseTask = {
	id: string;
	partId: string;
	partName: string;
	partModel?: string;
	requiredQty: number;
	lockedQty: number;
	status: string;
	selectionPlanId?: string;
	selectionItemId?: string;
	projectId?: string;
	projectName?: string;
	selectionPlanName?: string;
	remark?: string;
};

export type StockOverviewItem = {
	partId: string;
	partName: string;
	partModel: string;
	category: string;
	totalQty: number;
	lockedQty: number;
	availableQty: number;
	pendingPurchaseQty: number;
	updatedAt?: string;
};

export type StockLockDetail = {
	transactionId: string;
	projectId: string;
	projectName: string;
	selectionPlanId: string;
	selectionPlanName: string;
	selectionItemId: string;
	lockedQty: number;
	operatorId: string;
	operatorName: string;
	lockedAt?: string;
};

export type StockLockSummary = {
	partId: string;
	partName: string;
	partModel: string;
	category: string;
	totalLocked: number;
	locks: StockLockDetail[];
};

export type StockTransaction = {
	id: string;
	type: string;
	partId: string;
	partName: string;
	partModel: string;
	quantity: number;
	sourceType: number;
	sourceTypeName: string;
	operatorId: string;
	operatorName: string;
	projectId?: string;
	projectName?: string;
	selectionPlanId?: string;
	selectionPlanName?: string;
	usage?: string;
	note?: string;
	createdAt: string;
};

export function getParts() {
	return request({
		url: '/api/parts',
		method: 'get',
	});
}

export function getProjects(parentId?: string | null) {
	return request({
		url: '/api/projects',
		method: 'get',
		params: parentId ? { parentId } : undefined,
	});
}

export function getSelections(projectId?: string | null) {
	return request({
		url: '/api/selections',
		method: 'get',
		params: projectId ? { projectId } : undefined,
	});
}

export function getStockOverview() {
	return request({
		url: '/api/stock/overview',
		method: 'get',
	});
}

export function getStockLocks() {
	return request({
		url: '/api/stock/locks',
		method: 'get',
	});
}

export function getStockLocksByPartId(partId: string) {
	return request({
		url: `/api/stock/locks/${partId}`,
		method: 'get',
	});
}

export function getTransactions(params: any) {
	return request({
		url: '/api/stock/transactions',
		method: 'get',
		params,
	});
}

export function inboundStock(data: { partId: string; quantity: number; sourceType?: any; note?: string | null }) {
	return request({
		url: '/api/stock/inbound',
		method: 'post',
		data,
	});
}

export function outboundStock(data: {
	partId: string;
	quantity: number;
	projectId: string;
	recipientId?: string | null;
	recipientName?: string | null;
	selectionPlanId?: string | null;
	usage?: string | null;
	note?: string | null;
}) {
	return request({
		url: '/api/stock/outbound',
		method: 'post',
		data,
	});
}

export function lockStock(data: {
	partId: string;
	quantity: number;
	projectId?: string | null;
	selectionPlanId?: string | null;
	selectionItemId?: string | null;
	note?: string | null;
}) {
	return request({
		url: '/api/stock/lock',
		method: 'post',
		data,
	});
}

export function unlockStock(data: { partId: string; quantity: number; projectId?: string | null; note?: string | null }) {
	return request({
		url: '/api/stock/unlock',
		method: 'post',
		data,
	});
}

export function returnStock(data: { partId: string; quantity: number; projectId?: string | null; note?: string | null }) {
	return request({
		url: '/api/stock/return',
		method: 'post',
		data,
	});
}

export function getPurchaseTasks(status?: string) {
	return request({
		url: '/api/purchase-tasks',
		method: 'get',
		params: status ? { status } : undefined,
	});
}

export function receivePurchaseTask(taskId: string, data?: { remark?: string | null }) {
	return request({
		url: `/api/purchase-tasks/${taskId}/receive`,
		method: 'post',
		data: data || {},
	});
}
