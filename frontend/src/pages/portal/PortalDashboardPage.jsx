import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { usersApi } from '../../api/users.js'
import { useAuth } from '../../context/AuthContext.jsx'
import LoadingSpinner from '../../components/LoadingSpinner.jsx'
import ErrorBanner from '../../components/ErrorBanner.jsx'

const STATUS_LABEL = { 0: 'Successful', 1: 'Failed' }
const STATUS_CLASS = { 0: 'badge badge-success', 1: 'badge badge-danger' }
const TYPE_LABEL = ['Electricity', 'Water', 'Internet', 'GSM', 'Natural Gas']

export default function PortalDashboardPage() {
  const { user } = useAuth()
  const [data, setData] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  useEffect(() => {
    usersApi.getMyDashboard()
      .then(setData)
      .catch((err) => setError(err.message))
      .finally(() => setLoading(false))
  }, [])

  if (loading) return <LoadingSpinner />
  if (error) return <ErrorBanner message={error} />

  return (
    <div>
      <h1>Welcome, {user?.fullName}</h1>

      <div className="stats-row">
        <div className="stat-card">
          <span className="stat-value">{data.activeSubscriptionCount}</span>
          <span className="stat-label">Active Subscriptions</span>
        </div>
        <div className="stat-card">
          <span className="stat-value">{data.unpaidThisMonth?.length ?? 0}</span>
          <span className="stat-label">Unpaid This Month</span>
        </div>
        <div className="stat-card">
          <span className="stat-value">₺{data.totalPaidThisYear?.toFixed(2)}</span>
          <span className="stat-label">Paid This Year</span>
        </div>
      </div>

      {data.unpaidThisMonth?.length > 0 && (
        <section className="dashboard-section">
          <h2>Unpaid This Month</h2>
          <ul className="unpaid-list">
            {data.unpaidThisMonth.map((s) => (
              <li key={s.id}>
                <span>{s.providerName}</span>
                <Link to={`/subscriptions/${s.id}`} className="btn btn-sm">Pay Now</Link>
              </li>
            ))}
          </ul>
        </section>
      )}

      <section className="dashboard-section">
        <h2>Recent Payments</h2>
        {data.recentPayments?.length === 0 ? (
          <p className="empty-state">No payments yet.</p>
        ) : (
          <table className="table">
            <thead>
              <tr>
                <th>Provider</th>
                <th>Type</th>
                <th>Period</th>
                <th>Amount</th>
                <th>Date</th>
                <th>Status</th>
                <th>Transaction</th>
              </tr>
            </thead>
            <tbody>
              {data.recentPayments?.map((p) => (
                <tr key={p.id}>
                  <td>
                    <Link to={`/subscriptions/${p.subscriptionId}`} className="link-btn">
                      {p.providerName}
                    </Link>
                  </td>
                  <td>{TYPE_LABEL[p.subscriptionType] ?? p.subscriptionType}</td>
                  <td>{p.period}</td>
                  <td>₺{p.amount.toFixed(2)}</td>
                  <td>{new Date(p.paymentDate).toLocaleDateString()}</td>
                  <td><span className={STATUS_CLASS[p.status]}>{STATUS_LABEL[p.status]}</span></td>
                  <td className="txn-cell">{p.externalTransactionId ?? '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>
    </div>
  )
}
