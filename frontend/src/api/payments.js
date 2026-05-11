import { get, post } from './client'

export const getPayments = (subscriptionId) =>
  get(subscriptionId ? `/api/payments?subscriptionId=${subscriptionId}` : '/api/payments')
export const createPayment = (data) => post('/api/payments', data)
