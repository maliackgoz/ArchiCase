import { useEffect, useMemo, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { notificationsApi } from '../api/notifications.js'
import { getCustomer } from '../api/customers.js'
import LoadingSpinner from '../components/LoadingSpinner'
import ErrorBanner from '../components/ErrorBanner'
import Button from '../components/Button'

const CHANNEL_FILTERS = [
  { value: '', label: 'All channels' },
  { value: 'SMS', label: 'SMS' },
  { value: 'EMAIL', label: 'Email' },
]

function ChannelBadge({ channel }) {
  const cls = channel === 'SMS' ? 'badge badge-info' : channel === 'EMAIL' ? 'badge badge-success' : 'badge'
  return <span className={cls}>{channel}</span>
}

export default function NotificationsPage() {
  const navigate = useNavigate()
  const [searchParams, setSearchParams] = useSearchParams()
  const customerIdParam = searchParams.get('customerId') || ''

  const [items, setItems] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [channel, setChannel] = useState('')
  const [nameQuery, setNameQuery] = useState('')
  const [idQuery, setIdQuery] = useState(customerIdParam)
  const [pinnedCustomer, setPinnedCustomer] = useState(null)

  async function load(c = channel) {
    setLoading(true); setError(null)
    try { setItems(await notificationsApi.list({ channel: c, take: 200 })) }
    catch (e) { setError(e.message) }
    finally { setLoading(false) }
  }

  useEffect(() => { load('') }, [])

  // Keep URL ?customerId in sync with the id filter so deep-links survive refresh.
  useEffect(() => {
    const next = new URLSearchParams(searchParams)
    if (idQuery.trim()) next.set('customerId', idQuery.trim())
    else next.delete('customerId')
    setSearchParams(next, { replace: true })
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [idQuery])

  // When deep-linked with a customer id, fetch the customer for a nicer header.
  useEffect(() => {
    if (!customerIdParam) { setPinnedCustomer(null); return }
    let cancelled = false
    getCustomer(customerIdParam)
      .then((c) => { if (!cancelled) setPinnedCustomer(c) })
      .catch(() => { if (!cancelled) setPinnedCustomer(null) })
    return () => { cancelled = true }
  }, [customerIdParam])

  function onChannelChange(e) {
    const v = e.target.value
    setChannel(v); load(v)
  }

  const filtered = useMemo(() => {
    const name = nameQuery.trim().toLowerCase()
    const id = idQuery.trim()
    return items.filter((n) => {
      if (id && String(n.customerId ?? '') !== id) return false
      if (name && !(n.customerName ?? '').toLowerCase().includes(name)) return false
      return true
    })
  }, [items, nameQuery, idQuery])

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <button className="link-btn" style={{ fontSize: 13, marginBottom: 6 }} onClick={() => navigate('/customers')}>← Back to Customers</button>
          <h1>
            Notifications Log
            {pinnedCustomer && (
              <span style={{ marginLeft: 10, fontSize: 14, fontWeight: 500, color: 'var(--color-text-muted)' }}>
                for {pinnedCustomer.fullName} <span style={{ color: 'var(--color-text-muted)', fontSize: 12 }}>#{pinnedCustomer.id}</span>
              </span>
            )}
          </h1>
          <p className="page-subtitle">
            Every SMS and Email reminder the system has sent. Updated each time a customer&apos;s
            reminders endpoint runs.
          </p>
        </div>
        <Button variant="secondary" onClick={() => load()}>Refresh</Button>
      </div>

      <div className="filters">
        <select value={channel} onChange={onChannelChange}>
          {CHANNEL_FILTERS.map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}
        </select>
        <input
          type="search"
          placeholder="Search by customer name…"
          value={nameQuery}
          onChange={(e) => setNameQuery(e.target.value)}
          style={{ flex: 1, minWidth: 240, padding: '7px 10px', border: '1px solid var(--color-border)', borderRadius: 'var(--radius)', fontSize: 13 }}
        />
        <input
          type="number"
          placeholder="Customer ID"
          value={idQuery}
          onChange={(e) => setIdQuery(e.target.value)}
          style={{ width: 140, padding: '7px 10px', border: '1px solid var(--color-border)', borderRadius: 'var(--radius)', fontSize: 13 }}
        />
        {(nameQuery || idQuery) && (
          <button type="button" className="link-btn" onClick={() => { setNameQuery(''); setIdQuery('') }}>
            Clear
          </button>
        )}
        <span style={{ marginLeft: 'auto', fontSize: 12, color: 'var(--color-text-muted)' }}>
          Showing {filtered.length} of {items.length}
        </span>
      </div>

      <ErrorBanner message={error} onDismiss={() => setError(null)} />

      {loading ? <LoadingSpinner /> : items.length === 0 ? (
        <div className="empty-state">
          <p>No notifications yet. Sign in as a customer with reminders to generate some.</p>
        </div>
      ) : filtered.length === 0 ? (
        <div className="empty-state">
          <p>No notifications match the current filter.</p>
        </div>
      ) : (
        <div className="card" style={{ padding: 0 }}>
          <table className="table">
            <thead>
              <tr>
                <th>Sent</th>
                <th>Channel</th>
                <th>Customer</th>
                <th>Recipient</th>
                <th>Message</th>
              </tr>
            </thead>
            <tbody>
              {filtered.map((n) => (
                <tr key={n.id}>
                  <td style={{ whiteSpace: 'nowrap', fontSize: 12, color: 'var(--color-text-muted)' }}>
                    {new Date(n.sentAt).toLocaleString('en-GB', { hour12: false })}
                  </td>
                  <td><ChannelBadge channel={n.channel} /></td>
                  <td>
                    {n.customerId ? (
                      <button className="link-btn" onClick={() => navigate(`/dashboard/${n.customerId}`)}>
                        {n.customerName} <span style={{ color: 'var(--color-text-muted)', fontSize: 11 }}>#{n.customerId}</span>
                      </button>
                    ) : (
                      <span style={{ color: 'var(--color-text-muted)', fontSize: 12 }}>—</span>
                    )}
                  </td>
                  <td className="txn-cell">{n.recipient}</td>
                  <td className="notification-message">{n.message}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
