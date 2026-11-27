import { useEffect, useState } from 'react'
import { BrowserRouter } from 'react-router-dom'
import AppRoutes from '@/routes'
import type { AuthSession } from '@/types/auth'
import { ApiError, getSession, logoutSession } from '@/services/auth'

function App() {
  return <BrowserRouter><AppContent /></BrowserRouter>
}

function AppContent() {
  const [session, setSession] = useState<AuthSession | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [retry, setRetry] = useState(0)

  useEffect(() => {
    let active = true
    async function restore() {
      try {
        const result = await getSession()
        if (active) setSession(result)
      } catch (e) {
        if (active && !(e instanceof ApiError && e.status === 401))
          setError('Không thể kiểm tra phiên đăng nhập. Vui lòng thử lại.')
      } finally {
        if (active) setLoading(false)
      }
    }
    void restore()
    return () => { active = false }
  }, [retry])

  useEffect(() => {
    const expire = () => setSession(null)
    window.addEventListener('crm:session-expired', expire)
    return () => window.removeEventListener('crm:session-expired', expire)
  }, [])

  async function logout() {
    try {
      await logoutSession()
      setSession(null)
      setError('')
    } catch {
      setError('Không thể đăng xuất. Vui lòng thử lại.')
    }
  }

  if (loading) return <p className="p-8 text-sm">Đang kiểm tra phiên đăng nhập...</p>
  if (error && !session) return <div role="alert" className="p-8 text-sm">{error} <button type="button" className="underline" onClick={() => { setError(''); setLoading(true); setRetry(value => value + 1) }}>Thử lại</button></div>
  return <>{error && <p role="alert" className="bg-red-50 p-3 text-center text-sm text-red-700">{error}</p>}<AppRoutes session={session} onLogin={setSession} onLogout={() => void logout()} /></>
}

export default App
