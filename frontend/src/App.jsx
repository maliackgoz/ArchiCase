import { BrowserRouter, Routes, Route, NavLink, Navigate, useNavigate } from 'react-router-dom'
import './styles/global.css'
import { AuthProvider, useAuth } from './context/AuthContext.jsx'
import ProtectedRoute from './components/ProtectedRoute.jsx'
import CustomersPage from './pages/CustomersPage'
import SubscriptionsPage from './pages/SubscriptionsPage'
import SubscriptionDetailPage from './pages/SubscriptionDetailPage'
import DashboardPage from './pages/DashboardPage'
import NotificationsPage from './pages/NotificationsPage.jsx'
import LandingPage from './pages/LandingPage.jsx'
import PortalDashboardPage from './pages/portal/PortalDashboardPage.jsx'
import PortalSubscriptionsPage from './pages/portal/PortalSubscriptionsPage.jsx'
import RemindersPage from './pages/portal/RemindersPage.jsx'

function NavBar() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()

  function handleLogout() {
    logout()
    navigate('/login', { replace: true })
  }

  if (!user) {
    return (
      <nav className="nav">
        <span className="nav-brand">SubscriptionApp</span>
        <div className="nav-spacer" />
        <NavLink to="/login" className="btn btn-sm btn-primary nav-cta">Sign in</NavLink>
      </nav>
    )
  }

  return (
    <nav className="nav">
      <span className="nav-brand">SubscriptionApp</span>
      {user.role === 'Admin' ? (
        <div className="nav-group">
          <span className="nav-section-label">Admin</span>
          <NavLink to="/customers">Customers</NavLink>
          <NavLink to="/subscriptions">Analysis</NavLink>
          <NavLink to="/notifications">Notifications</NavLink>
        </div>
      ) : (
        <div className="nav-group">
          <span className="nav-section-label">Portal</span>
          <NavLink to="/portal/dashboard">Dashboard</NavLink>
          <NavLink to="/portal/subscriptions">Subscriptions</NavLink>
          <NavLink to="/portal/reminders">Reminders</NavLink>
        </div>
      )}
      <div className="nav-spacer" />
      <div className="nav-user">
        <span className="nav-user-name">{user.fullName}</span>
        <span className="nav-user-role">{user.role}</span>
        <button className="btn btn-sm nav-logout" onClick={handleLogout}>Sign out</button>
      </div>
    </nav>
  )
}

function HomeRedirect() {
  const { user } = useAuth()
  if (!user) return <Navigate to="/login" replace />
  return <Navigate to={user.role === 'Admin' ? '/customers' : '/portal/dashboard'} replace />
}

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <NavBar />
        <main className="main-content">
          <Routes>
            <Route path="/" element={<HomeRedirect />} />
            <Route path="/login" element={<LandingPage />} />

            <Route path="/customers" element={<ProtectedRoute roles={['Admin']}><CustomersPage /></ProtectedRoute>} />
            <Route path="/subscriptions" element={<ProtectedRoute roles={['Admin']}><SubscriptionsPage /></ProtectedRoute>} />
            <Route path="/dashboard" element={<Navigate to="/customers" replace />} />
            <Route path="/dashboard/:customerId" element={<ProtectedRoute roles={['Admin']}><DashboardPage /></ProtectedRoute>} />
            <Route path="/notifications" element={<ProtectedRoute roles={['Admin']}><NotificationsPage /></ProtectedRoute>} />
            <Route path="/subscriptions/:id" element={<ProtectedRoute><SubscriptionDetailPage /></ProtectedRoute>} />

            <Route path="/portal/dashboard" element={<ProtectedRoute roles={['Customer']}><PortalDashboardPage /></ProtectedRoute>} />
            <Route path="/portal/subscriptions" element={<ProtectedRoute roles={['Customer']}><PortalSubscriptionsPage /></ProtectedRoute>} />
            <Route path="/portal/reminders" element={<ProtectedRoute roles={['Customer']}><RemindersPage /></ProtectedRoute>} />

            <Route path="*" element={<HomeRedirect />} />
          </Routes>
        </main>
      </BrowserRouter>
    </AuthProvider>
  )
}
