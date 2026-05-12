import { useState, useEffect, useMemo } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { getSubscriptions } from '../api/subscriptions'
import { getCustomers } from '../api/customers'
import Table from '../components/Table'
import LoadingSpinner from '../components/LoadingSpinner'
import ErrorBanner from '../components/ErrorBanner'

const SUB_TYPES = ['Electricity', 'Water', 'Internet', 'GSM', 'Natural Gas']
const STATUS_LABELS = ['Active', 'Passive']

const TYPE_OPTIONS = [
  { value: '', label: 'All types' },
  ...SUB_TYPES.map((label, value) => ({ value: String(value), label })),
]
const STATUS_OPTIONS = [
  { value: '', label: 'All statuses' },
  { value: '0', label: 'Active' },
  { value: '1', label: 'Passive' },
]
const AUTOPAY_OPTIONS = [
  { value: '', label: 'Any' },
  { value: 'true', label: 'Auto-pay on' },
  { value: 'false', label: 'Auto-pay off' },
]

export default function SubscriptionsPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const navigate = useNavigate()

  const customerIdFilter = searchParams.get('customerId') || ''
  const typeFilter = searchParams.get('type') || ''
  const statusFilter = searchParams.get('status') || ''
  const autoPayFilter = searchParams.get('autoPay') || ''
  const providerQuery = searchParams.get('provider') || ''

  const [subscriptions, setSubscriptions] = useState([])
  const [customers, setCustomers] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  useEffect(() => {
    let cancelled = false
    setLoading(true); setError(null)
    Promise.all([
      getSubscriptions(customerIdFilter || null),
      getCustomers(),
    ])
      .then(([subs, custs]) => { if (!cancelled) { setSubscriptions(subs); setCustomers(custs) } })
      .catch((e) => { if (!cancelled) setError(e.message) })
      .finally(() => { if (!cancelled) setLoading(false) })
    return () => { cancelled = true }
  }, [customerIdFilter])

  function patchParams(patch) {
    const next = new URLSearchParams(searchParams)
    for (const [k, v] of Object.entries(patch)) {
      if (v == null || v === '') next.delete(k)
      else next.set(k, v)
    }
    setSearchParams(next)
  }

  const filtered = useMemo(() => {
    const q = providerQuery.trim().toLowerCase()
    return subscriptions.filter((s) => {
      if (typeFilter !== '' && String(s.subscriptionType) !== typeFilter) return false
      if (statusFilter !== '' && String(s.status) !== statusFilter) return false
      if (autoPayFilter !== '' && String(!!s.isAutoPay) !== autoPayFilter) return false
      if (q && !s.providerName.toLowerCase().includes(q) && !s.subscriptionNumber.toLowerCase().includes(q)) return false
      return true
    })
  }, [subscriptions, typeFilter, statusFilter, autoPayFilter, providerQuery])

  const stats = useMemo(() => {
    const total = filtered.length
    const active = filtered.filter((s) => s.status === 0).length
    const passive = total - active
    const autopayOn = filtered.filter((s) => s.isAutoPay).length
    const byType = SUB_TYPES.map((label, i) => ({
      label,
      count: filtered.filter((s) => s.subscriptionType === i).length,
    })).filter((x) => x.count > 0)
    return { total, active, passive, autopayOn, byType }
  }, [filtered])

  const hasActiveFilters = customerIdFilter || typeFilter || statusFilter || autoPayFilter || providerQuery

  const columns = [
    { key: 'id', label: 'ID' },
    {
      key: 'providerName', label: 'Provider',
      render: (r) => (
        <button className="link-btn" onClick={() => navigate(`/subscriptions/${r.id}`)}>
          {r.providerName}
        </button>
      ),
    },
    { key: 'subscriptionNumber', label: 'Number' },
    { key: 'subscriptionType', label: 'Type', render: (r) => SUB_TYPES[r.subscriptionType] },
    {
      key: 'status', label: 'Status',
      render: (r) => (
        <span className={`badge badge-${r.status === 0 ? 'active' : 'passive'}`}>{STATUS_LABELS[r.status]}</span>
      ),
    },
    { key: 'billingDayOfMonth', label: 'Billing' },
    { key: 'lastPaymentDayOfMonth', label: 'Last Pay' },
    { key: 'paymentDayOfMonth', label: 'Pay Day' },
    {
      key: 'isAutoPay', label: 'Auto-pay',
      render: (r) => (r.isAutoPay ? <span className="badge badge-info">On</span> : <span className="badge badge-warning">Off</span>),
    },
    {
      key: 'customerFullName', label: 'Customer',
      render: (r) => (
        <button className="link-btn" onClick={() => navigate(`/dashboard/${r.customerId}`)}>
          {r.customerFullName}
        </button>
      ),
    },
  ]

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <button className="link-btn" style={{ fontSize: 13, marginBottom: 6 }} onClick={() => navigate('/customers')}>← Back to Customers</button>
          <h1>
            Subscription Analysis
            {customerIdFilter && (
              <span style={{ marginLeft: 10, fontSize: 14, fontWeight: 500, color: 'var(--color-text-muted)' }}>
                {(() => {
                  const c = customers.find((x) => String(x.id) === customerIdFilter)
                  return c ? <>for {c.fullName} <span style={{ color: 'var(--color-text-muted)', fontSize: 12 }}>#{c.id}</span></> : null
                })()}
              </span>
            )}
          </h1>
          <p className="page-subtitle">
            Slice every subscription in the system. Filters compose; numbers update live.
          </p>
        </div>
      </div>

      <ErrorBanner message={error} onDismiss={() => setError(null)} />

      <div className="analysis-filters">
        <label>
          <span className="filter-label">Customer</span>
          <select value={customerIdFilter} onChange={(e) => patchParams({ customerId: e.target.value })}>
            <option value="">All customers</option>
            {customers.map((c) => <option key={c.id} value={c.id}>{c.fullName}</option>)}
          </select>
        </label>
        <label>
          <span className="filter-label">Type</span>
          <select value={typeFilter} onChange={(e) => patchParams({ type: e.target.value })}>
            {TYPE_OPTIONS.map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}
          </select>
        </label>
        <label>
          <span className="filter-label">Status</span>
          <select value={statusFilter} onChange={(e) => patchParams({ status: e.target.value })}>
            {STATUS_OPTIONS.map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}
          </select>
        </label>
        <label>
          <span className="filter-label">Auto-pay</span>
          <select value={autoPayFilter} onChange={(e) => patchParams({ autoPay: e.target.value })}>
            {AUTOPAY_OPTIONS.map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}
          </select>
        </label>
        <label style={{ flex: 1, minWidth: 200 }}>
          <span className="filter-label">Provider / Number</span>
          <input
            type="search"
            placeholder="Search provider or number…"
            value={providerQuery}
            onChange={(e) => patchParams({ provider: e.target.value })}
          />
        </label>
        {hasActiveFilters && (
          <button type="button" className="link-btn" onClick={() => setSearchParams(new URLSearchParams())} style={{ alignSelf: 'flex-end', paddingBottom: 8 }}>
            Clear filters
          </button>
        )}
      </div>

      <div className="dashboard-grid" style={{ marginBottom: 16 }}>
        <div className="stat-card">
          <div className="stat-label">Matching</div>
          <div className="stat-value">{stats.total}</div>
        </div>
        <div className="stat-card">
          <div className="stat-label">Active / Passive</div>
          <div className="stat-value" style={{ fontSize: 22 }}>{stats.active} / {stats.passive}</div>
        </div>
        <div className="stat-card">
          <div className="stat-label">Auto-pay on</div>
          <div className="stat-value">{stats.autopayOn}</div>
        </div>
        <div className="stat-card">
          <div className="stat-label">By Type</div>
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: 6, marginTop: 6 }}>
            {stats.byType.length === 0
              ? <span style={{ color: 'var(--color-text-muted)', fontSize: 12 }}>—</span>
              : stats.byType.map((t) => (
                <span key={t.label} className="badge badge-info">{t.label} · {t.count}</span>
              ))}
          </div>
        </div>
      </div>

      {loading ? <LoadingSpinner /> : (
        <div className="card" style={{ padding: 0 }}>
          <Table columns={columns} rows={filtered} emptyMessage="No subscriptions match these filters." />
        </div>
      )}
    </div>
  )
}
