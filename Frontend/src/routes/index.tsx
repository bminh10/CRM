import { Navigate, Route, Routes } from 'react-router-dom'
import LoginPage from '@/pages/LoginPage'
import SignupPage from '@/pages/SignupPage'
import LandingPage from '@/pages/LandingPage'
import WorkspacePage from '@/pages/WorkspacePage'
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
      <Route path="/" element={<LandingPage />} />
      <Route element={<GuestRoute session={session} />}>
        <Route path="/login" element={<LoginPage onLogin={onLogin} />} />
        <Route path="/signup" element={<SignupPage onLogin={onLogin} />} />
      </Route>
      <Route element={<ProtectedRoute session={session} />}>
        <Route path="/dashboard/*" element={session && <WorkspacePage session={session} onLogout={onLogout} />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}

export default AppRoutes
