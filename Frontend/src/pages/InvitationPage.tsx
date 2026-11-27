import { useState } from 'react'
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom'
import { ArrowRight, Check, Layers3, Mail } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { acceptWorkspaceInvitation } from '@/services/crm'
import type { AuthSession } from '@/types/auth'

function InvitationPage({ session }: { session: AuthSession | null }) {
  const { token = '' } = useParams()
  const location = useLocation()
  const navigate = useNavigate()
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const returnTo = `${location.pathname}${location.search}`

  async function accept() {
    if (loading) return
    setLoading(true)
    setError('')
    try {
      await acceptWorkspaceInvitation(token)
      navigate('/workspaces', { replace: true })
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Không thể chấp nhận lời mời.')
    } finally {
      setLoading(false)
    }
  }

  return <main className="grid min-h-screen place-items-center bg-[#f7f8fc] px-5 py-12 text-[#17264a]">
    <Card className="w-full max-w-md rounded-3xl border-[#e7ebf3] bg-white py-0 ring-0 shadow-[0_24px_70px_-45px_#243d9a]">
      <CardContent className="p-8 sm:p-10">
        <span className="grid size-12 place-items-center rounded-2xl bg-[#eaf1ff] text-[#243d9a]"><Layers3 className="size-5" /></span>
        <p className="mt-7 text-xs font-bold tracking-[.16em] text-[#5268b0]">LỜI MỜI WORKSPACE</p>
        <h1 className="mt-2 text-2xl font-semibold tracking-tight">Tham gia cùng đội ngũ</h1>
        <p className="mt-3 text-sm leading-6 text-[#718096]">Chấp nhận lời mời để bắt đầu cộng tác trong workspace. Hãy đăng nhập bằng đúng email đã nhận lời mời.</p>
        {!session ? <div className="mt-7 grid gap-2">
          <Button asChild className="h-10 bg-[#243d9a] text-white hover:bg-[#1c327f]"><Link to="/login" state={{ from: returnTo }}>Đăng nhập để tiếp tục <ArrowRight /></Link></Button>
          <Button asChild variant="outline" className="h-10"><Link to="/signup" state={{ from: returnTo }}>Tạo tài khoản mới</Link></Button>
        </div> : <div className="mt-7">
          {error && <p role="alert" className="mb-4 rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
          <Button onClick={() => void accept()} disabled={loading} className="h-10 w-full bg-[#243d9a] text-white hover:bg-[#1c327f]">{loading ? 'Đang tham gia...' : 'Chấp nhận lời mời'} <Check /></Button>
        </div>}
        <p className="mt-5 flex items-center justify-center gap-1.5 text-xs text-[#8a97aa]"><Mail className="size-3.5" /> Lời mời chỉ dùng được với email được mời.</p>
      </CardContent>
    </Card>
  </main>
}

export default InvitationPage
