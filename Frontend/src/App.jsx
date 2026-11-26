import { useEffect, useState } from 'react'
import { BrowserRouter } from 'react-router-dom'
import AppRoutes from '@/routes'

function App() {
  const [session, setSession] = useState(null)

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
