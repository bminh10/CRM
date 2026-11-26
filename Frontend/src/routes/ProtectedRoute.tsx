import { Navigate, Outlet } from 'react-router-dom'
import type { AuthSession } from '@/types/auth'

function ProtectedRoute({ session }: { session: AuthSession | null }) {
  return session ? <Outlet /> : <Navigate to="/login" replace />
}

export default ProtectedRoute
