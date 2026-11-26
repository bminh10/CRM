import { Navigate, Route, Routes } from 'react-router-dom'
import LoginPage from '@/pages/LoginPage'
import DashboardPage from '@/pages/DashboardPage'
import GuestRoute from './GuestRoute'
import ProtectedRoute from './ProtectedRoute'
import type { AuthSession } from '@/types/auth'

function AppRoutes({ session, onLogin, onLogout }: {
  session: AuthSession | null
  onLogin: (session: AuthSession) => void
  onLogout: () => void
}) {
  return (
    <Routes>
      <Route path="/" element={<Navigate to="/dashboard" replace />} />
      <Route element={<GuestRoute session={session} />}>
        <Route path="/login" element={<LoginPage onLogin={onLogin} />} />
      </Route>
      <Route element={<ProtectedRoute session={session} />}>
        <Route path="/dashboard" element={session && <DashboardPage user={session.user} onLogout={onLogout} />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}

export default AppRoutes
