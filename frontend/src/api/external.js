import { get } from './client'

export const getDebt = (subscriptionId) => get(`/api/external/debt-inquiry/${subscriptionId}`)
