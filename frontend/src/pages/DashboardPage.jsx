import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { getCustomers, getDashboard } from '../api/customers'
import LoadingSpinner from '../components/LoadingSpinner'
import ErrorBanner from '../components/ErrorBanner'
import Table from '../components/Table'

const SUB_TYPES = ['Electricity', 'Water', 'Internet', 'GSM', 'Natural Gas']
const PAYMENT_STATUS = ['Successful', 'Failed']

export default function DashboardPage() {
  const [customers, setCustomers] = useState([])
  const [selectedId, setSelectedId] = useState('')
  const [dashboard, setDashboard] = useState(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState(null)
  const navigate = useNavigate()

  useEffect(() => {
    getCustomers()
      .then(setCustomers)
      .catch((e) => setError(e.message))
  }, [])

  const handleSelect = async (id) => {
    setSelectedId(id)
    if (!id) { setDashboard(null); return }
    setLoading(true); setError(null)
    try { setDashboard(await getDashboard(id)) }
    catch (e) { setError(e.message); setDashboard(null) }
    finally { setLoading(false) }
  }

  const unpaidColumns = [
    { key: 'id', label: 'ID' },
    { key: 'providerName', label: 'Provider', render: (r) => <button className="link-btn" onClick={() => navigate(`/subscriptions/${r.id}`)}>{r.providerName}</button> },
    { key: 'subscriptionType', label: 'Type', render: (r) => SUB_TYPES[r.subscriptionType] },
  ]

  const paymentColumns = [
    { key: 'id', label: 'ID' },
    { key: 'subscriptionId', label: 'Sub ID' },
    { key: 'amount', label: 'Amount', render: (r) => `${Number(r.amount).toLocaleString()} TRY` },
    { key: 'period', label: 'Period' },
    { key: 'status', label: 'Status', render: (r) => <span className={`badge badge-${r.status === 0 ? 'success' : 'failed'}`}>{PAYMENT_STATUS[r.status]}</span> },
    { key: 'paymentDate', label: 'Date', render: (r) => new Date(r.paymentDate).toLocaleString() },
  ]

  return (
    <div className="page">
      <div className="page-header"><h1>Customer Dashboard</h1></div>
      <ErrorBanner message={error} onDismiss={() => setError(null)} />

      <div className="filters" style={{ marginBottom: 24 }}>
        <label style={{ fontWeight: 500 }}>Customer:</label>
        <select value={selectedId} onChange={(e) => handleSelect(e.target.value)}>
          <option value="">Select a customer…</option>
          {customers.map((c) => <option key={c.id} value={c.id}>{c.fullName}</option>)}
        </select>
      </div>

      {loading && <LoadingSpinner />}

      {dashboard && !loading && (
        <>
          <div className="dashboard-grid">
            <div className="stat-card">
              <div className="stat-label">Active Subscriptions</div>
              <div className="stat-value">{dashboard.activeSubscriptionCount}</div>
            </div>
            <div className="stat-card">
              <div className="stat-label">Unpaid This Month</div>
              <div className="stat-value">{dashboard.unpaidThisMonth.length}</div>
            </div>
            <div className="stat-card">
              <div className="stat-label">Total Paid This Year</div>
              <div className="stat-value" style={{ fontSize: 22 }}>{Number(dashboard.totalPaidThisYear).toLocaleString()} TRY</div>
            </div>
          </div>

          <div className="section-title">Unpaid This Month</div>
          <div className="card" style={{ marginBottom: 20 }}>
            <Table columns={unpaidColumns} rows={dashboard.unpaidThisMonth} emptyMessage="All subscriptions are paid for this month." />
          </div>

          <div className="section-title">Recent Payments (last 10)</div>
          <div className="card">
            <Table columns={paymentColumns} rows={dashboard.recentPayments} emptyMessage="No payments recorded." />
          </div>
        </>
      )}

      {!selectedId && !loading && (
        <div className="card" style={{ textAlign: 'center', padding: 40, color: 'var(--color-text-muted)' }}>
          Select a customer to view their dashboard.
        </div>
      )}
    </div>
  )
}
