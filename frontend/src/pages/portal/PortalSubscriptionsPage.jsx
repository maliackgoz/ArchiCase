import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { usersApi } from '../../api/users.js'
import LoadingSpinner from '../../components/LoadingSpinner.jsx'
import ErrorBanner from '../../components/ErrorBanner.jsx'
import Modal from '../../components/Modal.jsx'
import Button from '../../components/Button.jsx'

const TYPE_LABEL = ['Electricity', 'Water', 'Internet', 'GSM', 'Natural Gas']
const STATUS_CLASS = { 0: 'badge badge-success', 1: 'badge badge-warning' }
const STATUS_LABEL = { 0: 'Active', 1: 'Passive' }

const emptyCreate = { subscriptionType: 0, providerName: '', subscriptionNumber: '' }

export default function PortalSubscriptionsPage() {
  const [subscriptions, setSubscriptions] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [toast, setToast] = useState(null)

  const [showCreate, setShowCreate] = useState(false)
  const [createForm, setCreateForm] = useState(emptyCreate)
  const [createErrors, setCreateErrors] = useState({})
  const [creating, setCreating] = useState(false)

  const [editing, setEditing] = useState(null)
  const [editForm, setEditForm] = useState({ status: 0, providerName: '', paymentDayOfMonth: 1, isAutoPay: false })
  const [editErrors, setEditErrors] = useState({})
  const [savingEdit, setSavingEdit] = useState(false)

  const [autoPaying, setAutoPaying] = useState(false)

  async function load() {
    setLoading(true); setError(null)
    try { setSubscriptions(await usersApi.getMySubscriptions()) }
    catch (e) { setError(e.message) }
    finally { setLoading(false) }
  }

  useEffect(() => { load() }, [])

  function validateCreate(form) {
    const errs = {}
    if (!form.providerName.trim()) errs.providerName = 'Required'
    if (!form.subscriptionNumber.trim()) errs.subscriptionNumber = 'Required'
    return errs
  }

  async function handleCreate(e) {
    e.preventDefault()
    const errs = validateCreate(createForm)
    if (Object.keys(errs).length) { setCreateErrors(errs); return }
    setCreating(true)
    try {
      const created = await usersApi.createMySubscription({
        subscriptionType: Number(createForm.subscriptionType),
        providerName: createForm.providerName.trim(),
        subscriptionNumber: createForm.subscriptionNumber.trim(),
      })
      setShowCreate(false)
      setCreateForm(emptyCreate); setCreateErrors({})
      setToast(`Added. Provider window: billing day ${created.billingDayOfMonth}, last payment day ${created.lastPaymentDayOfMonth}.`)
      await load()
    } catch (e) {
      setCreateErrors({ _: e.message })
    } finally {
      setCreating(false)
    }
  }

  function openEdit(sub) {
    setEditing(sub)
    setEditForm({
      status: sub.status,
      providerName: sub.providerName,
      paymentDayOfMonth: sub.paymentDayOfMonth,
      isAutoPay: sub.isAutoPay,
    })
    setEditErrors({})
  }

  function paymentDayOutOfRange() {
    if (!editing) return false
    const d = Number(editForm.paymentDayOfMonth)
    return !Number.isInteger(d) || d < editing.billingDayOfMonth || d > editing.lastPaymentDayOfMonth
  }

  async function handleEdit(e) {
    e.preventDefault()
    if (!editForm.providerName.trim()) { setEditErrors({ providerName: 'Required' }); return }
    if (paymentDayOutOfRange()) {
      setEditErrors({ paymentDayOfMonth: `Must be between ${editing.billingDayOfMonth} and ${editing.lastPaymentDayOfMonth}` })
      return
    }
    setSavingEdit(true)
    try {
      await usersApi.updateMySubscription(editing.id, {
        status: Number(editForm.status),
        providerName: editForm.providerName.trim(),
        paymentDayOfMonth: Number(editForm.paymentDayOfMonth),
        isAutoPay: !!editForm.isAutoPay,
      })
      setEditing(null)
      setToast('Subscription updated.')
      await load()
    } catch (e) {
      setEditErrors({ _: e.message })
    } finally {
      setSavingEdit(false)
    }
  }

  async function handleDelete(sub) {
    if (!confirm(`Delete subscription to ${sub.providerName}? All payment history will also be removed.`)) return
    try {
      await usersApi.deleteMySubscription(sub.id)
      setToast('Subscription deleted.')
      await load()
    } catch (e) {
      setError(e.message)
    }
  }

  async function handleRunAutoPay() {
    setAutoPaying(true)
    try {
      const result = await usersApi.runAutoPay()
      const parts = []
      if (result.succeeded) parts.push(`${result.succeeded} paid`)
      if (result.failed) parts.push(`${result.failed} failed`)
      if (result.skipped) parts.push(`${result.skipped} skipped`)
      setToast(result.processed === 0
        ? 'No auto-pay subscriptions found.'
        : `Auto-pay complete: ${parts.join(', ') || 'nothing to do'}.`)
      await load()
    } catch (e) {
      setError(e.message)
    } finally {
      setAutoPaying(false)
    }
  }

  const autoPayCount = subscriptions.filter((s) => s.isAutoPay && s.status === 0).length

  if (loading) return <LoadingSpinner />

  return (
    <div>
      <div className="page-header">
        <h1>My Subscriptions</h1>
        <div style={{ display: 'flex', gap: 8 }}>
          {autoPayCount > 0 && (
            <Button variant="secondary" disabled={autoPaying} onClick={handleRunAutoPay}>
              {autoPaying ? 'Running…' : `Run auto-pay (${autoPayCount})`}
            </Button>
          )}
          <Button onClick={() => { setCreateForm(emptyCreate); setCreateErrors({}); setShowCreate(true) }}>
            + Add Subscription
          </Button>
        </div>
      </div>

      {toast && <div className="toast" onClick={() => setToast(null)}>{toast}</div>}
      <ErrorBanner message={error} onDismiss={() => setError(null)} />

      {subscriptions.length === 0 ? (
        <div className="empty-state">
          <p>You haven&apos;t added any subscriptions yet.</p>
          <p style={{ marginTop: 8 }}>
            <Button onClick={() => setShowCreate(true)}>Add your first subscription</Button>
          </p>
        </div>
      ) : (
        <table className="table">
          <thead>
            <tr>
              <th>Provider</th>
              <th>Type</th>
              <th>Number</th>
              <th title="Set by the provider">Billing Day</th>
              <th title="Set by the provider — your hard deadline">Last Payment Day</th>
              <th title="Your chosen auto-pay day, between billing and last payment">Payment Day</th>
              <th>Auto-pay</th>
              <th>Status</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {subscriptions.map((s) => (
              <tr key={s.id}>
                <td>{s.providerName}</td>
                <td>{TYPE_LABEL[s.subscriptionType] ?? s.subscriptionType}</td>
                <td>{s.subscriptionNumber}</td>
                <td>{s.billingDayOfMonth}</td>
                <td>{s.lastPaymentDayOfMonth}</td>
                <td>{s.paymentDayOfMonth}</td>
                <td>{s.isAutoPay ? <span className="badge badge-info">On</span> : <span className="badge badge-warning">Off</span>}</td>
                <td><span className={STATUS_CLASS[s.status]}>{STATUS_LABEL[s.status]}</span></td>
                <td style={{ display: 'flex', gap: 6 }}>
                  <Link to={`/subscriptions/${s.id}`} className="btn btn-sm btn-primary">View / Pay</Link>
                  <Button variant="secondary" small onClick={() => openEdit(s)}>Edit</Button>
                  <Button variant="danger" small onClick={() => handleDelete(s)}>Delete</Button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {showCreate && (
        <Modal title="Add Subscription" onClose={() => setShowCreate(false)}>
          <form onSubmit={handleCreate}>
            {createErrors._ && <ErrorBanner message={createErrors._} />}
            <div className="form-group">
              <label>Type</label>
              <select value={createForm.subscriptionType} onChange={(e) => setCreateForm({ ...createForm, subscriptionType: e.target.value })}>
                {TYPE_LABEL.map((t, i) => <option key={i} value={i}>{t}</option>)}
              </select>
            </div>
            <div className="form-group">
              <label>Provider Name</label>
              <input value={createForm.providerName} placeholder="e.g. BEDAŞ"
                onChange={(e) => setCreateForm({ ...createForm, providerName: e.target.value })} />
              {createErrors.providerName && <div className="form-error">{createErrors.providerName}</div>}
            </div>
            <div className="form-group">
              <label>Subscription / Customer Number</label>
              <input value={createForm.subscriptionNumber} placeholder="e.g. BEDAS-1234"
                onChange={(e) => setCreateForm({ ...createForm, subscriptionNumber: e.target.value })} />
              {createErrors.subscriptionNumber && <div className="form-error">{createErrors.subscriptionNumber}</div>}
            </div>
            <div className="form-hint">
              Your provider sets the billing day and the last payment day (7-day window). You can later pick an auto-pay day inside that window.
            </div>
            <div className="form-actions">
              <Button variant="secondary" type="button" onClick={() => setShowCreate(false)}>Cancel</Button>
              <Button type="submit" disabled={creating}>{creating ? 'Saving…' : 'Add'}</Button>
            </div>
          </form>
        </Modal>
      )}

      {editing && (
        <Modal title={`Edit ${editing.providerName}`} onClose={() => setEditing(null)}>
          <form onSubmit={handleEdit}>
            {editErrors._ && <ErrorBanner message={editErrors._} />}

            <div className="form-group">
              <label>Provider Name</label>
              <input value={editForm.providerName}
                onChange={(e) => setEditForm({ ...editForm, providerName: e.target.value })} />
              {editErrors.providerName && <div className="form-error">{editErrors.providerName}</div>}
            </div>

            <div className="form-row">
              <div className="form-group" style={{ flex: 1 }}>
                <label>Billing Day</label>
                <input value={editing.billingDayOfMonth} disabled readOnly />
              </div>
              <div className="form-group" style={{ flex: 1 }}>
                <label>Last Payment Day</label>
                <input value={editing.lastPaymentDayOfMonth} disabled readOnly />
              </div>
            </div>
            <div className="form-hint" style={{ marginTop: -8 }}>
              Both days come from the provider. You can&apos;t change them here.
            </div>

            <div className="form-group">
              <label>Payment Day (auto-pay date)</label>
              <input
                type="number"
                min={editing.billingDayOfMonth}
                max={editing.lastPaymentDayOfMonth}
                value={editForm.paymentDayOfMonth}
                onChange={(e) => setEditForm({ ...editForm, paymentDayOfMonth: e.target.value })}
              />
              {paymentDayOutOfRange() && (
                <div className="form-warning">
                  Payment day should be between {editing.billingDayOfMonth} (billing) and {editing.lastPaymentDayOfMonth} (last payment).
                </div>
              )}
              {editErrors.paymentDayOfMonth && <div className="form-error">{editErrors.paymentDayOfMonth}</div>}
            </div>

            <div className="form-group">
              <label className="checkbox-label">
                <input
                  type="checkbox"
                  checked={!!editForm.isAutoPay}
                  onChange={(e) => setEditForm({ ...editForm, isAutoPay: e.target.checked })}
                />
                Pay automatically on payment day
              </label>
              {editForm.isAutoPay && (
                <div className="form-hint" style={{ marginTop: 6, marginBottom: 0 }}>
                  We&apos;ll attempt the payment on day {editForm.paymentDayOfMonth} of each month.
                </div>
              )}
            </div>

            <div className="form-group">
              <label>Status</label>
              <select value={editForm.status} onChange={(e) => setEditForm({ ...editForm, status: e.target.value })}>
                <option value={0}>Active</option>
                <option value={1}>Passive</option>
              </select>
            </div>

            <div className="form-actions">
              <Button variant="secondary" type="button" onClick={() => setEditing(null)}>Cancel</Button>
              <Button type="submit" disabled={savingEdit || paymentDayOutOfRange()}>
                {savingEdit ? 'Saving…' : 'Save'}
              </Button>
            </div>
          </form>
        </Modal>
      )}
    </div>
  )
}
