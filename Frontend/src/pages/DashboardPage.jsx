import { ShieldCheck } from 'lucide-react'
import { Button } from '@/components/ui/button'

function DashboardPage({ user, onLogout }) {
  return (
    <main className="flex min-h-screen items-center justify-center bg-[#f7f8fc] px-6 text-slate-900">
      <div className="w-full max-w-[400px]">
        <div className="mb-6 flex size-12 items-center justify-center rounded-2xl bg-emerald-50 text-emerald-600">
          <ShieldCheck className="size-6" aria-hidden="true" />
        </div>
        <h1 className="text-3xl font-semibold tracking-tight">Đăng nhập thành công</h1>
        <p className="mt-3 text-sm leading-6 text-slate-500">
          Xin chào, {user.fullName}. Bạn đã đăng nhập vào CRM.
        </p>
        <Button className="mt-8 h-11 w-full bg-[#243d9a] text-white hover:bg-[#1c327f]" onClick={onLogout}>
          Đăng xuất
        </Button>
      </div>
    </main>
  )
}

export default DashboardPage
