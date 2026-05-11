async function apiFetch(url, options = {}) {
  const res = await fetch(url, {
    headers: { 'Content-Type': 'application/json', ...options.headers },
    ...options,
  })

  if (res.status === 204) return null

  const body = await res.json().catch(() => ({}))

  if (!res.ok) {
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
