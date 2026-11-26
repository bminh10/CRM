import { useState } from 'react'
import { ArrowRight, Eye, EyeOff, Layers3, LockKeyhole, Mail, ShieldCheck, UsersRound } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { isAuthSession, type AuthSession } from '@/types/auth'
import type { FormEvent } from 'react'

function LoginPage({ onLogin }: { onLogin: (session: AuthSession) => void }) {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  async function handleLogin(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (loading) return

    setError('')
    setLoading(true)
    try {
      const response = await fetch('/api/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email: email.trim(), password }),
      })

      if (!response.ok) {
        if (response.status === 401) {
          setError('Email hoặc mật khẩu không chính xác.')
        } else if (response.status === 503) {
          setError('Dịch vụ đăng nhập chưa được cấu hình. Vui lòng liên hệ quản trị viên.')
        } else {
          setError(`Không thể đăng nhập (mã lỗi ${response.status}). Vui lòng thử lại.`)
        }
        return
      }

      const result: unknown = await response.json()
      if (!isAuthSession(result)) {
        throw new Error('Invalid login response')
      }
      onLogin(result)
      setPassword('')
    } catch {
      setError('Không thể kết nối hoặc nhận phản hồi hợp lệ từ máy chủ. Vui lòng thử lại.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <main className="min-h-screen bg-[#f7f8fc] text-slate-900">
      <div className="mx-auto grid min-h-screen max-w-[1600px] lg:grid-cols-2">
        <aside className="relative hidden overflow-hidden bg-[#101c43] px-12 py-10 text-white lg:flex lg:flex-col xl:px-20">
          <div aria-hidden="true" className="pointer-events-none absolute -right-36 -top-32 h-[540px] w-[540px] rounded-full border border-white/10" />
          <div aria-hidden="true" className="pointer-events-none absolute -right-20 -top-16 h-[420px] w-[420px] rounded-full border border-white/10" />
          <div className="relative flex items-center gap-3 text-xl font-bold tracking-tight">
            <span className="flex size-10 items-center justify-center rounded-xl bg-white text-[#243d9a]">
              <Layers3 className="size-5" aria-hidden="true" />
            </span>
            CRM
          </div>

          <div className="relative my-auto max-w-lg py-16">
            <span className="inline-flex items-center gap-2 rounded-full border border-white/15 bg-white/10 px-3 py-1.5 text-xs font-medium text-blue-100">
              <span className="size-1.5 rounded-full bg-blue-300" />
              KHÔNG GIAN LÀM VIỆC CỦA BẠN
            </span>
            <h1 className="mt-7 text-4xl leading-tight font-semibold tracking-tight xl:text-5xl">
              Kết nối tốt hơn.<br />
              <span className="text-blue-300">Phát triển cùng nhau.</span>
            </h1>
            <p className="mt-6 max-w-md text-base leading-7 text-blue-100/70">
              Quản lý khách hàng và công việc của bạn trong một không gian đơn giản, tập trung.
            </p>
            <div className="mt-12 space-y-5 text-sm text-blue-50/85">
              <div className="flex items-center gap-3">
                <span className="rounded-lg bg-white/10 p-2"><UsersRound className="size-4" aria-hidden="true" /></span>
                Theo dõi và chăm sóc khách hàng
              </div>
              <div className="flex items-center gap-3">
                <span className="rounded-lg bg-white/10 p-2"><ShieldCheck className="size-4" aria-hidden="true" /></span>
                Thông tin được quản lý tập trung
              </div>
            </div>
          </div>
          <p className="relative text-xs text-blue-100/50">CRM · Đồng hành cùng sự phát triển của bạn</p>
        </aside>

        <section className="flex min-h-screen flex-col px-6 py-8 sm:px-12 lg:px-16">
          <div className="flex items-center gap-2 text-lg font-bold tracking-tight lg:hidden">
            <span className="flex size-9 items-center justify-center rounded-xl bg-[#243d9a] text-white">
              <Layers3 className="size-5" aria-hidden="true" />
            </span>
            CRM
          </div>
          <div className="mx-auto flex w-full max-w-[400px] flex-1 flex-col justify-center py-12">
              <div>
                <div className="mb-6 flex size-12 items-center justify-center rounded-2xl bg-blue-50 text-[#243d9a]">
                  <LockKeyhole className="size-6" aria-hidden="true" />
                </div>
                <p className="mb-2 text-xs font-semibold tracking-[0.16em] text-[#5268b0] uppercase">Chào mừng trở lại</p>
                <h2 className="text-3xl font-semibold tracking-tight">Đăng nhập</h2>
                <p className="mt-3 text-sm leading-6 text-slate-500">
                  Nhập thông tin tài khoản để tiếp tục vào không gian làm việc của bạn.
                </p>

                <form className="mt-8 space-y-5" onSubmit={handleLogin}>
                  <div className="space-y-2">
                    <Label htmlFor="email" className="text-sm font-medium text-slate-700">Email</Label>
                    <div className="relative">
                      <Mail className="pointer-events-none absolute top-1/2 left-3.5 size-4 -translate-y-1/2 text-slate-400" aria-hidden="true" />
                      <Input id="email" type="email" name="email" autoComplete="email" placeholder="ban@congty.com" maxLength={255} required value={email} onChange={(event) => setEmail(event.target.value)} className="h-11 bg-white pl-10" />
                    </div>
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="password" className="text-sm font-medium text-slate-700">Mật khẩu</Label>
                    <div className="relative">
                      <LockKeyhole className="pointer-events-none absolute top-1/2 left-3.5 size-4 -translate-y-1/2 text-slate-400" aria-hidden="true" />
                      <Input id="password" type={showPassword ? 'text' : 'password'} name="password" autoComplete="current-password" placeholder="Nhập mật khẩu" maxLength={1024} required value={password} onChange={(event) => setPassword(event.target.value)} className="h-11 bg-white pr-11 pl-10" />
                      <button type="button" aria-label={showPassword ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'} aria-pressed={showPassword} onClick={() => setShowPassword(!showPassword)} className="absolute top-1/2 right-3 -translate-y-1/2 rounded p-1 text-slate-400 hover:text-slate-700 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-blue-600">
                        {showPassword ? <EyeOff className="size-4" aria-hidden="true" /> : <Eye className="size-4" aria-hidden="true" />}
                      </button>
                    </div>
                  </div>
                  {error && <p role="alert" className="rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
                  <Button type="submit" disabled={loading} className="h-11 w-full bg-[#243d9a] text-white hover:bg-[#1c327f]">
                    {loading ? 'Đang đăng nhập...' : 'Đăng nhập'}
                    {!loading && <ArrowRight className="size-4" aria-hidden="true" />}
                  </Button>
                </form>
              </div>
          </div>
          <p className="text-center text-xs text-slate-400">CRM · Không gian làm việc của bạn</p>
        </section>
      </div>
    </main>
  )
}

export default LoginPage
