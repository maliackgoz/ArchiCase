import { get, post, put, del } from './client'

export const getSubscriptions = (customerId) =>
  get(customerId ? `/api/subscriptions?customerId=${customerId}` : '/api/subscriptions')
export const getSubscription = (id) => get(`/api/subscriptions/${id}`)
export const createSubscription = (data) => post('/api/subscriptions', data)
export const updateSubscription = (id, data) => put(`/api/subscriptions/${id}`, data)
export const deleteSubscription = (id) => del(`/api/subscriptions/${id}`)
