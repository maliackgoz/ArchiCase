import { useState, useEffect, useMemo } from 'react'
import { useNavigate } from 'react-router-dom'
import { getCustomers, deleteCustomer } from '../api/customers'
import Table from '../components/Table'
import Button from '../components/Button'
import LoadingSpinner from '../components/LoadingSpinner'
import ErrorBanner from '../components/ErrorBanner'

export default function CustomersPage() {
  const [customers, setCustomers] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [toast, setToast] = useState(null)
  const [nameQuery, setNameQuery] = useState('')
  const [idQuery, setIdQuery] = useState('')
  const navigate = useNavigate()

  async function load() {
    setLoading(true); setError(null)
    try { setCustomers(await getCustomers()) }
    catch (e) { setError(e.message) }
    finally { setLoading(false) }
  }

  useEffect(() => { load() }, [])

  const filtered = useMemo(() => {
    const name = nameQuery.trim().toLowerCase()
    const id = idQuery.trim()
    return customers.filter((c) => {
      if (name && !c.fullName.toLowerCase().includes(name) && !c.email.toLowerCase().includes(name)) return false
      if (id && String(c.id) !== id) return false
      return true
    })
  }, [customers, nameQuery, idQuery])

  async function handleDelete(id) {
    if (!confirm('Delete this customer? All their subscriptions, payments and login will be removed.')) return
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
    {
      key: 'fullName', label: 'Full Name',
      render: (r) => (
        <button className="link-btn" onClick={() => navigate(`/subscriptions?customerId=${r.id}`)}>
          {r.fullName}
        </button>
      ),
    },
    { key: 'email', label: 'Email' },
    { key: 'phoneNumber', label: 'Phone' },
    { key: 'subscriptionCount', label: 'Subs' },
    {
      key: 'actions', label: '',
      render: (r) => (
        <div style={{ display: 'flex', gap: 6, justifyContent: 'flex-end' }}>
          <Button variant="secondary" small onClick={() => navigate(`/dashboard/${r.id}`)}>Dashboard</Button>
          <Button variant="secondary" small onClick={() => navigate(`/subscriptions?customerId=${r.id}`)}>Subs</Button>
          <Button variant="secondary" small onClick={() => navigate(`/notifications?customerId=${r.id}`)}>Notifs</Button>
          <Button variant="danger" small onClick={() => handleDelete(r.id)}>Delete</Button>
        </div>
      ),
    },
  ]

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1>Customers</h1>
          <p className="page-subtitle">
            Monitor registered customers. New customers self-register from the landing page; admins
            can off-board.
          </p>
        </div>
      </div>

      {toast && <div className="toast" onClick={() => setToast(null)}>{toast}</div>}
      <ErrorBanner message={error} onDismiss={() => setError(null)} />

      <div className="filters">
        <input
          type="search"
          placeholder="Search by name or email…"
          value={nameQuery}
          onChange={(e) => setNameQuery(e.target.value)}
          style={{ flex: 1, minWidth: 240, padding: '7px 10px', border: '1px solid var(--color-border)', borderRadius: 'var(--radius)', fontSize: 13 }}
        />
        <input
          type="number"
          placeholder="Filter by ID"
          value={idQuery}
          onChange={(e) => setIdQuery(e.target.value)}
          style={{ width: 130, padding: '7px 10px', border: '1px solid var(--color-border)', borderRadius: 'var(--radius)', fontSize: 13 }}
        />
        {(nameQuery || idQuery) && (
          <button type="button" className="link-btn" onClick={() => { setNameQuery(''); setIdQuery('') }}>
            Clear
          </button>
        )}
        <span style={{ marginLeft: 'auto', fontSize: 12, color: 'var(--color-text-muted)' }}>
          {filtered.length} of {customers.length}
        </span>
      </div>

      {loading ? <LoadingSpinner /> : (
        <div className="card">
          <Table columns={columns} rows={filtered} emptyMessage="No customers match the filter." />
        </div>
      )}
    </div>
  )
}
