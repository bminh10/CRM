import { Navigate, Outlet, useLocation } from 'react-router-dom'
import type { AuthSession } from '@/types/auth'

function GuestRoute({ session }: { session: AuthSession | null }) {
  const location = useLocation()
  const from = (location.state as { from?: string } | null)?.from
  return session ? <Navigate to={from ?? '/workspaces'} replace /> : <Outlet />
}

export default GuestRoute
