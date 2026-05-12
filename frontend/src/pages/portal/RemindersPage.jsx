import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { usersApi } from '../../api/users.js'
import LoadingSpinner from '../../components/LoadingSpinner.jsx'
import ErrorBanner from '../../components/ErrorBanner.jsx'

function DueBadge({ daysUntilDue, isOverdue }) {
  if (isOverdue) return <span className="badge badge-danger">{Math.abs(daysUntilDue)}d overdue</span>
  if (daysUntilDue === 0) return <span className="badge badge-warning">Due today</span>
  return <span className="badge badge-info">In {daysUntilDue}d</span>
}

export default function RemindersPage() {
  const [reminders, setReminders] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [notified, setNotified] = useState(false)

  useEffect(() => {
    usersApi.getMyReminders()
      .then((data) => {
        setReminders(data)
        if (data.length > 0) setNotified(true)
      })
      .catch((err) => setError(err.message))
      .finally(() => setLoading(false))
  }, [])

  if (loading) return <LoadingSpinner />
  if (error) return <ErrorBanner message={error} />

  return (
    <div>
      <h1>Payment Reminders</h1>
      <p className="page-subtitle">
        Active subscriptions whose provider last-payment day is within 5 days and remain unpaid this period.
      </p>

      {notified && (
        <div className="alert alert-info">
          SMS and email reminders have been sent to your registered phone number and email address.
        </div>
      )}

      {reminders.length === 0 ? (
        <div className="empty-state">
          <p>No upcoming payments — you&apos;re all caught up!</p>
        </div>
      ) : (
        <table className="table">
          <thead>
            <tr>
              <th>Provider</th>
              <th>Period</th>
              <th title="Set by the provider">Billing Day</th>
              <th title="Provider's hard deadline">Last Payment Day</th>
              <th title="Your chosen auto-pay date">Payment Day</th>
              <th>Auto-pay</th>
              <th>Due</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {reminders.map((r) => (
              <tr key={r.subscriptionId} className={r.isOverdue ? 'row-overdue' : ''}>
                <td>{r.providerName}</td>
                <td>{r.period}</td>
                <td>{r.billingDayOfMonth}</td>
                <td>{r.lastPaymentDayOfMonth}</td>
                <td>{r.paymentDayOfMonth}</td>
                <td>{r.isAutoPay ? <span className="badge badge-info">On</span> : <span className="badge badge-warning">Off</span>}</td>
                <td><DueBadge daysUntilDue={r.daysUntilDue} isOverdue={r.isOverdue} /></td>
                <td>
                  <Link to={`/subscriptions/${r.subscriptionId}`} className="btn btn-sm btn-primary">
                    Pay Now
                  </Link>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  )
}
