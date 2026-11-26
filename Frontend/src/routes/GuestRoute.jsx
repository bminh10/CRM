import { Navigate, Outlet } from 'react-router-dom'

function GuestRoute({ session }) {
  return session ? <Navigate to="/dashboard" replace /> : <Outlet />
}

export default GuestRoute
