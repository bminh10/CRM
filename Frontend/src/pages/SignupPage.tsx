import { useState, type FormEvent } from 'react'
import { ArrowLeft, ArrowRight, Layers3 } from 'lucide-react'
import { Link } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import type { AuthSession } from '@/types/auth'
import { ApiError, register } from '@/services/auth'

function SignupPage({ onLogin }: { onLogin: (session: AuthSession) => void }) {
  const [fullName, setFullName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [confirm, setConfirm] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (loading) return
    if (!fullName.trim()) { setError('Vui lòng nhập họ tên.'); return }
    if (password !== confirm) { setError('Mật khẩu xác nhận không khớp.'); return }
    setError('')
    setLoading(true)
    try {
      const result = await register(fullName.trim(), email.trim(), password)
      setPassword('')
      setConfirm('')
      onLogin(result)
    } catch (e) {
      setError(e instanceof ApiError
        ? e.status === 409 ? 'Email này đã được đăng ký.' : e.status === 503 ? 'Dịch vụ đăng ký chưa được cấu hình.' : `Không thể đăng ký (mã lỗi ${e.status}).`
        : 'Không thể kết nối hoặc nhận phản hồi hợp lệ từ máy chủ.')
    } finally { setLoading(false) }
  }

  return <main className="flex min-h-screen items-center justify-center bg-[#f7f8fc] px-5 py-12">
    <div className="w-full max-w-md">
      <Link to="/" className="mb-8 inline-flex items-center gap-2 text-sm text-[#243d9a]"><ArrowLeft className="size-4" /> Trang chủ</Link>
      <div className="rounded-3xl border border-[#e2e8f4] bg-white p-7 shadow-[0_24px_70px_-35px_#243d9a] sm:p-10">
        <div className="mb-7 grid size-11 place-items-center rounded-xl bg-[#243d9a] text-white"><Layers3 className="size-5" /></div>
        <p className="text-xs font-semibold tracking-[.16em] text-[#243d9a]">BẮT ĐẦU CÙNG CRM</p>
        <h1 className="mt-2 text-3xl font-semibold tracking-tight">Tạo tài khoản</h1>
        <p className="mt-3 text-sm leading-6 text-slate-500">Kết nối đội ngũ và quản lý khách hàng trong một không gian.</p>
        <form className="mt-7 space-y-4" onSubmit={submit}>
          <div className="space-y-2"><Label htmlFor="fullName">Họ và tên</Label><Input id="fullName" autoComplete="name" required maxLength={150} value={fullName} onChange={e => setFullName(e.target.value)} className="h-11" /></div>
          <div className="space-y-2"><Label htmlFor="signup-email">Email</Label><Input id="signup-email" type="email" autoComplete="email" required maxLength={255} value={email} onChange={e => setEmail(e.target.value)} className="h-11" /></div>
          <div className="space-y-2"><Label htmlFor="signup-password">Mật khẩu</Label><Input id="signup-password" type="password" autoComplete="new-password" required minLength={12} maxLength={1024} value={password} onChange={e => setPassword(e.target.value)} className="h-11" /><p className="text-xs text-slate-500">Ít nhất 12 ký tự.</p></div>
          <div className="space-y-2"><Label htmlFor="confirm-password">Xác nhận mật khẩu</Label><Input id="confirm-password" type="password" autoComplete="new-password" required minLength={12} maxLength={1024} value={confirm} onChange={e => setConfirm(e.target.value)} className="h-11" /></div>
          {error && <p role="alert" className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
          <Button type="submit" disabled={loading} className="h-11 w-full bg-[#243d9a] text-white hover:bg-[#1c327f]">{loading ? 'Đang tạo tài khoản...' : 'Tạo tài khoản'} <ArrowRight /></Button>
        </form>
        <p className="mt-6 text-center text-sm text-slate-500">Đã có tài khoản? <Link to="/login" className="font-semibold text-[#243d9a] hover:underline">Đăng nhập</Link></p>
      </div>
    </div>
  </main>
}

export default SignupPage
