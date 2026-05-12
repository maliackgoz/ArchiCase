import { useState, useEffect } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { getCustomer, getDashboard } from '../api/customers'
import LoadingSpinner from '../components/LoadingSpinner'
import ErrorBanner from '../components/ErrorBanner'
import Table from '../components/Table'

const SUB_TYPES = ['Electricity', 'Water', 'Internet', 'GSM', 'Natural Gas']
const PAYMENT_STATUS = ['Successful', 'Failed']

export default function DashboardPage() {
  const { customerId } = useParams()
  const navigate = useNavigate()
  const [customer, setCustomer] = useState(null)
  const [dashboard, setDashboard] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  useEffect(() => {
    let cancelled = false
    setLoading(true); setError(null)
    Promise.all([getCustomer(customerId), getDashboard(customerId)])
      .then(([c, d]) => { if (!cancelled) { setCustomer(c); setDashboard(d) } })
      .catch((e) => { if (!cancelled) setError(e.message) })
      .finally(() => { if (!cancelled) setLoading(false) })
    return () => { cancelled = true }
  }, [customerId])

  const unpaidColumns = [
    { key: 'id', label: 'ID' },
    {
      key: 'providerName', label: 'Provider',
      render: (r) => <button className="link-btn" onClick={() => navigate(`/subscriptions/${r.id}`)}>{r.providerName}</button>,
    },
    { key: 'subscriptionType', label: 'Type', render: (r) => SUB_TYPES[r.subscriptionType] },
  ]

  const paymentColumns = [
    {
      key: 'providerName', label: 'Provider',
      render: (r) => <button className="link-btn" onClick={() => navigate(`/subscriptions/${r.subscriptionId}`)}>{r.providerName}</button>,
    },
    { key: 'subscriptionType', label: 'Type', render: (r) => SUB_TYPES[r.subscriptionType] },
    { key: 'period', label: 'Period' },
    { key: 'amount', label: 'Amount', render: (r) => `${Number(r.amount).toLocaleString()} TRY` },
    { key: 'paymentDate', label: 'Date', render: (r) => new Date(r.paymentDate).toLocaleString() },
    { key: 'status', label: 'Status', render: (r) => <span className={`badge badge-${r.status === 0 ? 'success' : 'failed'}`}>{PAYMENT_STATUS[r.status]}</span> },
    { key: 'externalTransactionId', label: 'Transaction', render: (r) => r.externalTransactionId ?? '—' },
  ]

  if (loading) return <div className="page"><LoadingSpinner /></div>
  if (error) return <div className="page"><ErrorBanner message={error} /></div>
  if (!dashboard || !customer) return null

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <button className="link-btn" style={{ fontSize: 13, marginBottom: 6 }} onClick={() => navigate('/customers')}>← Back to Customers</button>
          <h1>{customer.fullName}&apos;s Dashboard</h1>
          <p className="page-subtitle">{customer.email} · {customer.phoneNumber}</p>
        </div>
      </div>

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
    </div>
  )
}
