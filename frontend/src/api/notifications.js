import { apiFetch } from './client.js'

export const notificationsApi = {
  list: ({ channel = '', take = 100 } = {}) => {
    const params = new URLSearchParams()
    if (channel) params.set('channel', channel)
    if (take) params.set('take', take)
    const qs = params.toString()
    return apiFetch(`/api/notifications${qs ? `?${qs}` : ''}`)
  },
}
