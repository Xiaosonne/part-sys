import request from './request'

export function getStockOverview() {
  return request.get('/stock/overview')
}

export function getStockLocks() {
  return request.get('/stock/locks')
}

export function getStockLocksByPartId(partId) {
  return request.get(`/stock/locks/${partId}`)
}

export function getTransactions(params) {
  return request.get('/stock/transactions', { params })
}

export function getTransactionsByPartId(partId) {
  return request.get(`/stock/transactions/${partId}`)
}

export function inbound(data) {
  return request.post('/stock/inbound', data)
}

export function outbound(data) {
  return request.post('/stock/outbound', data)
}

export function lock(data) {
  return request.post('/stock/lock', data)
}

export function unlock(data) {
  return request.post('/stock/unlock', data)
}

export function returnStock(data) {
  return request.post('/stock/return', data)
}
