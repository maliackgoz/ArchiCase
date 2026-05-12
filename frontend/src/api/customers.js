import { get, del } from './client'

export const getCustomers = () => get('/api/customers')
export const getCustomer = (id) => get(`/api/customers/${id}`)
export const deleteCustomer = (id) => del(`/api/customers/${id}`)
export const getDashboard = (id) => get(`/api/customers/${id}/dashboard`)
