import { useState } from 'react'
import { Button } from '@/components/ui/button'

function App() {
  const [count, setCount] = useState(0)

  return (
    <main className="flex min-h-screen flex-col items-center justify-center gap-4">
      <h1 className="text-2xl font-semibold">React + shadcn/ui</h1>
      <Button onClick={() => setCount((value) => value + 1)}>
        Count: {count}
      </Button>
    </main>
  )
}

export default App
