import { BrowserRouter, Routes, Route, NavLink } from 'react-router-dom'
import './styles/global.css'
import CustomersPage from './pages/CustomersPage'
import SubscriptionsPage from './pages/SubscriptionsPage'
import SubscriptionDetailPage from './pages/SubscriptionDetailPage'
import DashboardPage from './pages/DashboardPage'

export default function App() {
  return (
    <BrowserRouter>
      <nav className="nav">
        <span className="nav-brand">SubscriptionApp</span>
        <NavLink to="/customers">Customers</NavLink>
        <NavLink to="/subscriptions">Subscriptions</NavLink>
        <NavLink to="/dashboard">Dashboard</NavLink>
      </nav>
      <Routes>
        <Route path="/" element={<CustomersPage />} />
        <Route path="/customers" element={<CustomersPage />} />
        <Route path="/subscriptions" element={<SubscriptionsPage />} />
        <Route path="/subscriptions/:id" element={<SubscriptionDetailPage />} />
        <Route path="/dashboard" element={<DashboardPage />} />
      </Routes>
    </BrowserRouter>
  )
}
