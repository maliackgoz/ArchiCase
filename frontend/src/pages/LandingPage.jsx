import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { authApi } from '../api/auth.js'
import { useAuth } from '../context/AuthContext.jsx'
import ErrorBanner from '../components/ErrorBanner.jsx'

const PHONE_RE = /^\+90[0-9]{10}$/

function redirectFor(role) {
  return role === 'Admin' ? '/customers' : '/portal/dashboard'
}

export default function LandingPage() {
  const { user, login } = useAuth()
  const navigate = useNavigate()
  const [mode, setMode] = useState('signin')

  useEffect(() => {
    if (user) navigate(redirectFor(user.role), { replace: true })
  }, [user, navigate])

  return (
    <div className="landing">
      <div className="landing-hero">
        <div className="landing-hero-inner">
          <div className="landing-brand">SubscriptionApp</div>
          <h1>One place for every recurring bill.</h1>
          <p>
            Track electricity, water, internet, GSM and natural-gas subscriptions in a single banking
            dashboard. Query live debt, pay in one click, and get reminders before each due date.
          </p>
          <ul className="landing-features">
            <li><span className="dot" /> Unified view across providers</li>
            <li><span className="dot" /> Reminders 5 days before billing day</li>
            <li><span className="dot" /> Pay-as-you-go with secure mock gateway</li>
            <li><span className="dot" /> Full payment history & dashboard</li>
          </ul>
        </div>
      </div>

      <div className="landing-auth">
        <div className="auth-card">
          <div className="auth-tabs">
            <button
              type="button"
              className={mode === 'signin' ? 'tab tab-active' : 'tab'}
              onClick={() => setMode('signin')}
            >
              Sign in
            </button>
            <button
              type="button"
              className={mode === 'signup' ? 'tab tab-active' : 'tab'}
              onClick={() => setMode('signup')}
            >
              Create account
            </button>
          </div>

          {mode === 'signin' ? (
            <SignInForm login={login} navigate={navigate} />
          ) : (
            <SignUpForm login={login} navigate={navigate} switchToSignIn={() => setMode('signin')} />
          )}
        </div>
      </div>
    </div>
  )
}

function SignInForm({ login, navigate }) {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState(null)

  async function handleSubmit(e) {
    e.preventDefault()
    setLoading(true); setError(null)
    try {
      const data = await authApi.login(email, password)
      login(data)
      navigate(redirectFor(data.user.role), { replace: true })
    } catch (err) {
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }

  return (
    <>
      <h2>Welcome back</h2>
      <p className="auth-subtitle">Sign in to manage your subscriptions.</p>

      {error && <ErrorBanner message={error} onDismiss={() => setError(null)} />}

      <form onSubmit={handleSubmit} className="auth-form">
        <label>
          Email
          <input type="email" value={email} onChange={(e) => setEmail(e.target.value)}
            placeholder="you@example.com" required autoFocus />
        </label>
        <label>
          Password
          <input type="password" value={password} onChange={(e) => setPassword(e.target.value)}
            placeholder="••••••••" required />
        </label>
        <button type="submit" className="btn btn-primary" disabled={loading}>
          {loading ? 'Signing in…' : 'Sign in'}
        </button>
      </form>

      <div className="auth-hint">
        <strong>Demo accounts — click to fill</strong>
        <div className="demo-accounts">
          {DEMO_ACCOUNTS.map((acc) => (
            <button
              key={acc.email}
              type="button"
              className="demo-account"
              onClick={() => { setEmail(acc.email); setPassword(acc.password); setError(null) }}
            >
              <span className="demo-account-role">{acc.role}</span>
              <span className="demo-account-name">{acc.label}</span>
              <span className="demo-account-email">{acc.email}</span>
              <span className="demo-account-note">{acc.note}</span>
            </button>
          ))}
        </div>
      </div>
    </>
  )
}

const DEMO_ACCOUNTS = [
  {
    role: 'Admin',
    label: 'Bank Admin',
    email: 'admin@bank.com',
    password: 'Admin1234!',
    note: 'Monitor customers, subscriptions and dashboards (read-only).',
  },
  {
    role: 'Customer',
    label: 'Ahmet Yılmaz',
    email: 'ahmet.yilmaz@example.com',
    password: 'Test1234!',
    note: 'Caught up — auto-pay on, no reminders, clean dashboard.',
  },
  {
    role: 'Customer',
    label: 'Fatma Kaya',
    email: 'fatma.kaya@example.com',
    password: 'Test1234!',
    note: 'Manual payer — 1 reminder due soon, 1 overdue, mixed history.',
  },
  {
    role: 'Customer',
    label: 'Mehmet Demir',
    email: 'mehmet.demir@example.com',
    password: 'Test1234!',
    note: 'Due-today reminder, one auto-pay sub, one Passive subscription.',
  },
]

function SignUpForm({ login, navigate, switchToSignIn }) {
  const [form, setForm] = useState({ fullName: '', email: '', password: '', phoneNumber: '+90' })
  const [errors, setErrors] = useState({})
  const [loading, setLoading] = useState(false)
  const [globalError, setGlobalError] = useState(null)

  function validate() {
    const errs = {}
    if (!form.fullName.trim()) errs.fullName = 'Required'
    if (!form.email.trim()) errs.email = 'Required'
    if (form.password.length < 8) errs.password = 'At least 8 characters'
    if (!PHONE_RE.test(form.phoneNumber)) errs.phoneNumber = 'Must be +90XXXXXXXXXX'
    return errs
  }

  async function handleSubmit(e) {
    e.preventDefault()
    const errs = validate()
    if (Object.keys(errs).length) { setErrors(errs); return }
    setLoading(true); setGlobalError(null)
    try {
      const data = await authApi.register(form.email, form.password, form.fullName, form.phoneNumber)
      login(data)
      navigate(redirectFor(data.user.role), { replace: true })
    } catch (err) {
      setGlobalError(err.message)
    } finally {
      setLoading(false)
    }
  }

  function update(field, value) {
    setForm({ ...form, [field]: value })
    if (errors[field]) setErrors({ ...errors, [field]: undefined })
  }

  return (
    <>
      <h2>Create a customer account</h2>
      <p className="auth-subtitle">No verification needed &mdash; sign up and start tracking.</p>

      {globalError && <ErrorBanner message={globalError} onDismiss={() => setGlobalError(null)} />}

      <form onSubmit={handleSubmit} className="auth-form">
        <label>
          Full name
          <input value={form.fullName} onChange={(e) => update('fullName', e.target.value)}
            placeholder="Ada Lovelace" required />
          {errors.fullName && <span className="form-error">{errors.fullName}</span>}
        </label>
        <label>
          Email
          <input type="email" value={form.email} onChange={(e) => update('email', e.target.value)}
            placeholder="you@example.com" required />
          {errors.email && <span className="form-error">{errors.email}</span>}
        </label>
        <label>
          Phone (+90 format)
          <input value={form.phoneNumber} onChange={(e) => update('phoneNumber', e.target.value)}
            placeholder="+905551234567" required />
          {errors.phoneNumber && <span className="form-error">{errors.phoneNumber}</span>}
        </label>
        <label>
          Password
          <input type="password" value={form.password} onChange={(e) => update('password', e.target.value)}
            placeholder="At least 8 characters" required />
          {errors.password && <span className="form-error">{errors.password}</span>}
        </label>
        <button type="submit" className="btn btn-primary" disabled={loading}>
          {loading ? 'Creating account…' : 'Create account'}
        </button>
      </form>

      <p className="auth-switch">
        Already have an account?{' '}
        <button type="button" className="link-btn" onClick={switchToSignIn}>Sign in</button>
      </p>
    </>
  )
}
