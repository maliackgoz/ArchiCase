import { Navigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext.jsx'

export default function ProtectedRoute({ children, roles }) {
  const { user } = useAuth()
  if (!user) return <Navigate to="/login" replace />
  if (roles && !roles.includes(user.role)) {
    const fallback = user.role === 'Admin' ? '/customers' : '/portal/dashboard'
    return <Navigate to={fallback} replace />
  }
  return children
}
