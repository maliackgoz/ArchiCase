import { useState, useEffect } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { getSubscriptions, createSubscription, updateSubscription, deleteSubscription } from '../api/subscriptions'
import { getCustomers } from '../api/customers'
import Table from '../components/Table'
import Button from '../components/Button'
import Modal from '../components/Modal'
import LoadingSpinner from '../components/LoadingSpinner'
import ErrorBanner from '../components/ErrorBanner'

const SUB_TYPES = ['Electricity', 'Water', 'Internet', 'GSM', 'Natural Gas']
const STATUS_LABELS = ['Active', 'Passive']

const emptyCreate = { customerId: '', subscriptionType: '0', providerName: '', subscriptionNumber: '', billingDayOfMonth: '10' }
const emptyUpdate = { status: '0', providerName: '', billingDayOfMonth: '10' }

export default function SubscriptionsPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const selectedCustomerId = searchParams.get('customerId') || ''
  const navigate = useNavigate()

  const [subscriptions, setSubscriptions] = useState([])
  const [customers, setCustomers] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [toast, setToast] = useState(null)
  const [showCreate, setShowCreate] = useState(false)
  const [editTarget, setEditTarget] = useState(null)
  const [form, setForm] = useState(emptyCreate)
  const [formErrors, setFormErrors] = useState({})
  const [submitting, setSubmitting] = useState(false)

  const loadAll = async () => {
    setLoading(true); setError(null)
    try {
      const [subs, custs] = await Promise.all([getSubscriptions(selectedCustomerId || null), getCustomers()])
      setSubscriptions(subs); setCustomers(custs)
    } catch (e) { setError(e.message) }
    finally { setLoading(false) }
  }

  useEffect(() => { loadAll() }, [selectedCustomerId])

  const validateCreate = () => {
    const errs = {}
    if (!form.customerId) errs.customerId = 'Required'
    if (!form.providerName.trim()) errs.providerName = 'Required'
    if (!form.subscriptionNumber.trim()) errs.subscriptionNumber = 'Required'
    const day = Number(form.billingDayOfMonth)
    if (!day || day < 1 || day > 28) errs.billingDayOfMonth = 'Must be 1–28'
    return errs
  }

  const handleCreate = async (e) => {
    e.preventDefault()
    const errs = validateCreate()
    if (Object.keys(errs).length) { setFormErrors(errs); return }
    setSubmitting(true)
    try {
      await createSubscription({
        customerId: Number(form.customerId),
        subscriptionType: Number(form.subscriptionType),
        providerName: form.providerName,
        subscriptionNumber: form.subscriptionNumber,
        billingDayOfMonth: Number(form.billingDayOfMonth),
      })
      setShowCreate(false); setForm(emptyCreate); setFormErrors({})
      setToast('Subscription created.'); await loadAll()
    } catch (e) { setFormErrors({ _: e.message }) }
    finally { setSubmitting(false) }
  }

  const openEdit = (sub) => {
    setEditTarget(sub)
    setForm({ status: String(sub.status), providerName: sub.providerName, billingDayOfMonth: String(sub.billingDayOfMonth) })
    setFormErrors({})
  }

  const handleUpdate = async (e) => {
    e.preventDefault()
    const day = Number(form.billingDayOfMonth)
    if (!day || day < 1 || day > 28) { setFormErrors({ billingDayOfMonth: 'Must be 1–28' }); return }
    setSubmitting(true)
    try {
      await updateSubscription(editTarget.id, {
        status: Number(form.status),
        providerName: form.providerName,
        billingDayOfMonth: Number(form.billingDayOfMonth),
      })
      setEditTarget(null); setToast('Subscription updated.'); await loadAll()
    } catch (e) { setFormErrors({ _: e.message }) }
    finally { setSubmitting(false) }
  }

  const handleDelete = async (id) => {
    if (!confirm('Delete this subscription? All its payments will also be deleted.')) return
    try { await deleteSubscription(id); setToast('Subscription deleted.'); await loadAll() }
    catch (e) { setError(e.message) }
  }

  const columns = [
    { key: 'id', label: 'ID' },
    { key: 'providerName', label: 'Provider', render: (r) => <button className="link-btn" onClick={() => navigate(`/subscriptions/${r.id}`)}>{r.providerName}</button> },
    { key: 'subscriptionNumber', label: 'Number' },
    { key: 'subscriptionType', label: 'Type', render: (r) => SUB_TYPES[r.subscriptionType] },
    { key: 'status', label: 'Status', render: (r) => <span className={`badge badge-${r.status === 0 ? 'active' : 'passive'}`}>{STATUS_LABELS[r.status]}</span> },
    { key: 'billingDayOfMonth', label: 'Billing Day' },
    { key: 'customerFullName', label: 'Customer' },
    { key: 'actions', label: '', render: (r) => (
      <div style={{ display: 'flex', gap: 6 }}>
        <Button variant="secondary" small onClick={() => openEdit(r)}>Edit</Button>
        <Button variant="danger" small onClick={() => handleDelete(r.id)}>Delete</Button>
      </div>
    )},
  ]

  return (
    <div className="page">
      <div className="page-header">
        <h1>Subscriptions</h1>
        <Button onClick={() => { setShowCreate(true); setForm(emptyCreate); setFormErrors({}) }}>+ Add Subscription</Button>
      </div>

      <div className="filters">
        <label style={{ fontWeight: 500 }}>Filter by customer:</label>
        <select value={selectedCustomerId} onChange={(e) => setSearchParams(e.target.value ? { customerId: e.target.value } : {})}>
          <option value="">All customers</option>
          {customers.map((c) => <option key={c.id} value={c.id}>{c.fullName}</option>)}
        </select>
      </div>

      {toast && <div className="toast" onClick={() => setToast(null)}>{toast}</div>}
      <ErrorBanner message={error} onDismiss={() => setError(null)} />

      {loading ? <LoadingSpinner /> : (
        <div className="card">
          <Table columns={columns} rows={subscriptions} emptyMessage="No subscriptions found." />
        </div>
      )}

      {showCreate && (
        <Modal title="Add Subscription" onClose={() => setShowCreate(false)}>
          <form onSubmit={handleCreate}>
            {formErrors._ && <ErrorBanner message={formErrors._} />}
            <div className="form-group">
              <label>Customer</label>
              <select value={form.customerId} onChange={(e) => setForm({ ...form, customerId: e.target.value })}>
                <option value="">Select…</option>
                {customers.map((c) => <option key={c.id} value={c.id}>{c.fullName}</option>)}
              </select>
              {formErrors.customerId && <div className="form-error">{formErrors.customerId}</div>}
            </div>
            <div className="form-group">
              <label>Subscription Type</label>
              <select value={form.subscriptionType} onChange={(e) => setForm({ ...form, subscriptionType: e.target.value })}>
                {SUB_TYPES.map((t, i) => <option key={i} value={i}>{t}</option>)}
              </select>
            </div>
            <div className="form-group">
              <label>Provider Name</label>
              <input value={form.providerName} onChange={(e) => setForm({ ...form, providerName: e.target.value })} />
              {formErrors.providerName && <div className="form-error">{formErrors.providerName}</div>}
            </div>
            <div className="form-group">
              <label>Subscription Number</label>
              <input value={form.subscriptionNumber} onChange={(e) => setForm({ ...form, subscriptionNumber: e.target.value })} />
              {formErrors.subscriptionNumber && <div className="form-error">{formErrors.subscriptionNumber}</div>}
            </div>
            <div className="form-group">
              <label>Billing Day of Month (1–28)</label>
              <input type="number" min="1" max="28" value={form.billingDayOfMonth} onChange={(e) => setForm({ ...form, billingDayOfMonth: e.target.value })} />
              {formErrors.billingDayOfMonth && <div className="form-error">{formErrors.billingDayOfMonth}</div>}
            </div>
            <div className="form-actions">
              <Button variant="secondary" type="button" onClick={() => setShowCreate(false)}>Cancel</Button>
              <Button type="submit" disabled={submitting}>{submitting ? 'Saving…' : 'Create'}</Button>
            </div>
          </form>
        </Modal>
      )}

      {editTarget && (
        <Modal title={`Edit — ${editTarget.providerName}`} onClose={() => setEditTarget(null)}>
          <form onSubmit={handleUpdate}>
            {formErrors._ && <ErrorBanner message={formErrors._} />}
            <div className="form-group">
              <label>Status</label>
              <select value={form.status} onChange={(e) => setForm({ ...form, status: e.target.value })}>
                <option value="0">Active</option>
                <option value="1">Passive</option>
              </select>
            </div>
            <div className="form-group">
              <label>Provider Name</label>
              <input value={form.providerName} onChange={(e) => setForm({ ...form, providerName: e.target.value })} />
            </div>
            <div className="form-group">
              <label>Billing Day of Month (1–28)</label>
              <input type="number" min="1" max="28" value={form.billingDayOfMonth} onChange={(e) => setForm({ ...form, billingDayOfMonth: e.target.value })} />
              {formErrors.billingDayOfMonth && <div className="form-error">{formErrors.billingDayOfMonth}</div>}
            </div>
            <div className="form-actions">
              <Button variant="secondary" type="button" onClick={() => setEditTarget(null)}>Cancel</Button>
              <Button type="submit" disabled={submitting}>{submitting ? 'Saving…' : 'Update'}</Button>
            </div>
          </form>
        </Modal>
      )}
    </div>
  )
}
