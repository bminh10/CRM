import { useEffect, useState } from 'react'
import { BrowserRouter } from 'react-router-dom'
import AppRoutes from '@/routes'
import type { AuthSession } from '@/types/auth'

function App() {
  const [session, setSession] = useState<AuthSession | null>(null)

  useEffect(() => {
    if (!session) return

    const timer = setTimeout(() => setSession(null), session.tokens.expiresIn * 1000)
    return () => clearTimeout(timer)
  }, [session])

  return (
    <BrowserRouter>
      <AppRoutes session={session} onLogin={setSession} onLogout={() => setSession(null)} />
    </BrowserRouter>
  )
}

export default App
