import { Navigate, Outlet } from 'react-router-dom'

function ProtectedRoute({ session }) {
  return session ? <Outlet /> : <Navigate to="/login" replace />
}

export default ProtectedRoute
