import { useState, useEffect } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { getSubscription } from '../api/subscriptions'
import { getDebt } from '../api/external'
import { createPayment } from '../api/payments'
import Button from '../components/Button'
import Modal from '../components/Modal'
import LoadingSpinner from '../components/LoadingSpinner'
import ErrorBanner from '../components/ErrorBanner'

const SUB_TYPES = ['Electricity', 'Water', 'Internet', 'GSM', 'Natural Gas']
const STATUS_LABELS = ['Active', 'Passive']

const PERIOD_RE = /^\d{4}-(0[1-9]|1[0-2])$/
const currentPeriod = () => {
  const d = new Date()
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}`
}

export default function SubscriptionDetailPage() {
  const { id } = useParams()
  const navigate = useNavigate()

  const [sub, setSub] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [toast, setToast] = useState(null)

  const [debtLoading, setDebtLoading] = useState(false)
  const [debt, setDebt] = useState(null)
  const [debtError, setDebtError] = useState(null)

  const [showPayModal, setShowPayModal] = useState(false)
  const [payForm, setPayForm] = useState({ amount: '', period: currentPeriod() })
  const [payErrors, setPayErrors] = useState({})
  const [paying, setPaying] = useState(false)

  useEffect(() => {
    setLoading(true)
    getSubscription(id)
      .then(setSub)
      .catch((e) => setError(e.message))
      .finally(() => setLoading(false))
  }, [id])

  const queryDebt = async () => {
    setDebtLoading(true); setDebtError(null); setDebt(null)
    try { setDebt(await getDebt(id)) }
    catch (e) { setDebtError(e.message) }
    finally { setDebtLoading(false) }
  }

  const openPayModal = () => {
    setPayForm({ amount: debt ? String(debt.amount) : '', period: debt?.period ?? currentPeriod() })
    setPayErrors({})
    setShowPayModal(true)
  }

  const handlePay = async (e) => {
    e.preventDefault()
    const errs = {}
    const amt = Number(payForm.amount)
    if (!amt || amt <= 0) errs.amount = 'Must be > 0'
    if (!PERIOD_RE.test(payForm.period)) errs.period = 'Format: YYYY-MM (e.g. 2026-05)'
    if (Object.keys(errs).length) { setPayErrors(errs); return }
    setPaying(true)
    try {
      await createPayment({ subscriptionId: Number(id), amount: amt, period: payForm.period })
      setShowPayModal(false)
      setToast(`Payment of ${amt} TRY accepted for ${payForm.period}.`)
      setDebt(null)
    } catch (e) {
      setPayErrors({ _: e.message })
    } finally {
      setPaying(false)
    }
  }

  if (loading) return <div className="page"><LoadingSpinner /></div>

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <button className="link-btn" style={{ fontSize: 13, marginBottom: 6 }} onClick={() => navigate('/subscriptions')}>← Back to Subscriptions</button>
          <h1>{sub?.providerName}</h1>
        </div>
      </div>

      {toast && <div className="toast" onClick={() => setToast(null)}>{toast}</div>}
      <ErrorBanner message={error} onDismiss={() => setError(null)} />

      {sub && (
        <div className="card" style={{ marginBottom: 20 }}>
          <table style={{ width: '100%', fontSize: 14, borderCollapse: 'collapse' }}>
            <tbody>
              {[
                ['ID', sub.id],
                ['Customer', sub.customerFullName],
                ['Type', SUB_TYPES[sub.subscriptionType]],
                ['Subscription Number', sub.subscriptionNumber],
                ['Status', <span className={`badge badge-${sub.status === 0 ? 'active' : 'passive'}`}>{STATUS_LABELS[sub.status]}</span>],
                ['Billing Day', sub.billingDayOfMonth],
                ['Created At', new Date(sub.createdAt).toLocaleDateString()],
              ].map(([label, val]) => (
                <tr key={label}>
                  <td style={{ padding: '7px 0', fontWeight: 600, width: 160, color: 'var(--color-text-muted)' }}>{label}</td>
                  <td style={{ padding: '7px 0' }}>{val}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <div style={{ display: 'flex', gap: 10, marginBottom: 8 }}>
        <Button onClick={queryDebt} disabled={debtLoading} variant="secondary">
          {debtLoading ? 'Querying…' : 'Query Debt'}
        </Button>
        {debt && <Button onClick={openPayModal}>Pay Now</Button>}
      </div>

      <ErrorBanner message={debtError} onDismiss={() => setDebtError(null)} />

      {debt && (
        <div className="debt-card">
          <h3>Current Debt — {debt.period}</h3>
          <div className="debt-amount">{debt.amount.toLocaleString()} {debt.currency}</div>
        </div>
      )}

      {showPayModal && (
        <Modal title="Process Payment" onClose={() => setShowPayModal(false)}>
          <form onSubmit={handlePay}>
            {payErrors._ && <ErrorBanner message={payErrors._} />}
            <div className="form-group">
              <label>Amount (TRY)</label>
              <input type="number" step="0.01" min="0.01" value={payForm.amount} onChange={(e) => setPayForm({ ...payForm, amount: e.target.value })} />
              {payErrors.amount && <div className="form-error">{payErrors.amount}</div>}
            </div>
            <div className="form-group">
              <label>Period (YYYY-MM)</label>
              <input value={payForm.period} onChange={(e) => setPayForm({ ...payForm, period: e.target.value })} />
              {payErrors.period && <div className="form-error">{payErrors.period}</div>}
            </div>
            <div className="form-actions">
              <Button variant="secondary" type="button" onClick={() => setShowPayModal(false)}>Cancel</Button>
              <Button type="submit" disabled={paying}>{paying ? 'Processing…' : 'Confirm Payment'}</Button>
            </div>
          </form>
        </Modal>
      )}
    </div>
  )
}
