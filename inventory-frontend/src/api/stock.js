import request from './request'

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

export function getTransactions(params) {
  return request.get('/stock/transactions', { params })
}
