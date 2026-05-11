import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { getCustomers, createCustomer, deleteCustomer } from '../api/customers'
import Table from '../components/Table'
import Button from '../components/Button'
import Modal from '../components/Modal'
import LoadingSpinner from '../components/LoadingSpinner'
import ErrorBanner from '../components/ErrorBanner'

const PHONE_RE = /^\+90[0-9]{10}$/

const empty = { fullName: '', email: '', phoneNumber: '' }

export default function CustomersPage() {
  const [customers, setCustomers] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [showModal, setShowModal] = useState(false)
  const [form, setForm] = useState(empty)
  const [formErrors, setFormErrors] = useState({})
  const [submitting, setSubmitting] = useState(false)
  const [toast, setToast] = useState(null)
  const navigate = useNavigate()

  const load = async () => {
    setLoading(true)
    setError(null)
    try { setCustomers(await getCustomers()) }
    catch (e) { setError(e.message) }
    finally { setLoading(false) }
  }

  useEffect(() => { load() }, [])

  const validate = () => {
    const errs = {}
    if (!form.fullName.trim()) errs.fullName = 'Required'
    if (!form.email.trim()) errs.email = 'Required'
    if (!PHONE_RE.test(form.phoneNumber)) errs.phoneNumber = 'Must be +90XXXXXXXXXX'
    return errs
  }

  const handleSubmit = async (e) => {
    e.preventDefault()
    const errs = validate()
    if (Object.keys(errs).length) { setFormErrors(errs); return }
    setSubmitting(true)
    try {
      await createCustomer(form)
      setShowModal(false)
      setForm(empty)
      setFormErrors({})
      setToast('Customer created.')
      await load()
    } catch (e) {
      setFormErrors({ _: e.message })
    } finally {
      setSubmitting(false)
    }
  }

  const handleDelete = async (id) => {
    if (!confirm('Delete this customer? All their subscriptions and payments will also be deleted.')) return
    try {
      await deleteCustomer(id)
      setToast('Customer deleted.')
      await load()
    } catch (e) {
      setError(e.message)
    }
  }

  const columns = [
    { key: 'id', label: 'ID' },
    { key: 'fullName', label: 'Full Name', render: (r) => <button className="link-btn" onClick={() => navigate(`/subscriptions?customerId=${r.id}`)}>{r.fullName}</button> },
    { key: 'email', label: 'Email' },
    { key: 'phoneNumber', label: 'Phone' },
    { key: 'subscriptionCount', label: 'Subscriptions' },
    { key: 'actions', label: '', render: (r) => <Button variant="danger" small onClick={() => handleDelete(r.id)}>Delete</Button> },
  ]

  return (
    <div className="page">
      <div className="page-header">
        <h1>Customers</h1>
        <Button onClick={() => setShowModal(true)}>+ Add Customer</Button>
      </div>

      {toast && <div className="toast" onClick={() => setToast(null)}>{toast}</div>}
      <ErrorBanner message={error} onDismiss={() => setError(null)} />

      {loading ? <LoadingSpinner /> : (
        <div className="card">
          <Table columns={columns} rows={customers} emptyMessage="No customers yet." />
        </div>
      )}

      {showModal && (
        <Modal title="Add Customer" onClose={() => { setShowModal(false); setForm(empty); setFormErrors({}) }}>
          <form onSubmit={handleSubmit}>
            {formErrors._ && <ErrorBanner message={formErrors._} />}
            <div className="form-group">
              <label>Full Name</label>
              <input value={form.fullName} onChange={(e) => setForm({ ...form, fullName: e.target.value })} />
              {formErrors.fullName && <div className="form-error">{formErrors.fullName}</div>}
            </div>
            <div className="form-group">
              <label>Email</label>
              <input type="email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} />
              {formErrors.email && <div className="form-error">{formErrors.email}</div>}
            </div>
            <div className="form-group">
              <label>Phone Number</label>
              <input placeholder="+905551234567" value={form.phoneNumber} onChange={(e) => setForm({ ...form, phoneNumber: e.target.value })} />
              {formErrors.phoneNumber && <div className="form-error">{formErrors.phoneNumber}</div>}
            </div>
            <div className="form-actions">
              <Button variant="secondary" onClick={() => setShowModal(false)} type="button">Cancel</Button>
              <Button type="submit" disabled={submitting}>{submitting ? 'Saving…' : 'Create'}</Button>
            </div>
          </form>
        </Modal>
      )}
    </div>
  )
}
