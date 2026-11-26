import { Navigate, Outlet } from 'react-router-dom'
import type { AuthSession } from '@/types/auth'

function GuestRoute({ session }: { session: AuthSession | null }) {
  return session ? <Navigate to="/dashboard" replace /> : <Outlet />
}

export default GuestRoute
