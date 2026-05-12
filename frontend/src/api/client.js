export async function apiFetch(url, options = {}) {
  const token = localStorage.getItem('auth_token')
  const res = await fetch(url, {
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...options.headers,
    },
    ...options,
  })

  if (res.status === 204) return null

  const body = await res.json().catch(() => ({}))

  if (!res.ok) {
    if (res.status === 401) {
      localStorage.removeItem('auth_token')
      localStorage.removeItem('auth_user')
      if (!window.location.pathname.startsWith('/login')) {
        window.location.assign('/login')
      }
    }
    const err = new Error(body?.error?.message || `HTTP ${res.status}`)
    err.code = body?.error?.code
    err.details = body?.error?.details
    err.status = res.status
    throw err
  }

  return body
}

export const get = (url) => apiFetch(url)
export const post = (url, data) => apiFetch(url, { method: 'POST', body: JSON.stringify(data) })
export const put = (url, data) => apiFetch(url, { method: 'PUT', body: JSON.stringify(data) })
export const del = (url) => apiFetch(url, { method: 'DELETE' })
