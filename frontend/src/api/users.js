import { apiFetch } from './client.js'

export const usersApi = {
  getMe: () => apiFetch('/api/users/me'),
  getMySubscriptions: () => apiFetch('/api/users/me/subscriptions'),
  getMySubscription: (id) => apiFetch(`/api/users/me/subscriptions/${id}`),
  getMySubscriptionPayments: (id) => apiFetch(`/api/users/me/subscriptions/${id}/payments`),
  createMySubscription: (data) =>
    apiFetch('/api/users/me/subscriptions', {
      method: 'POST',
      body: JSON.stringify(data),
    }),
  updateMySubscription: (id, data) =>
    apiFetch(`/api/users/me/subscriptions/${id}`, {
      method: 'PUT',
      body: JSON.stringify(data),
    }),
  deleteMySubscription: (id) =>
    apiFetch(`/api/users/me/subscriptions/${id}`, { method: 'DELETE' }),
  runAutoPay: () =>
    apiFetch('/api/users/me/subscriptions/process-auto-pay', { method: 'POST' }),
  getMyDashboard: () => apiFetch('/api/users/me/dashboard'),
  getMyReminders: () => apiFetch('/api/users/me/reminders'),
}
