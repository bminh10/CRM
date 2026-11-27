import { AlertCircle, Layers3, LoaderCircle, RefreshCw, ShieldCheck } from 'lucide-react'
import { Button } from '@/components/ui/button'

interface SessionStatusProps {
  error?: string
  onRetry?: () => void
}

export default function SessionStatus({ error, onRetry }: SessionStatusProps) {
  return <main className="flex min-h-screen items-center justify-center bg-[#f5f7fc] px-5 py-12 text-[#101c43]">
    <div className="w-full max-w-md text-center">
      <div className="mb-8 inline-flex items-center gap-2 text-xl font-bold tracking-tight">
        <span className="grid size-10 place-items-center rounded-xl bg-[#243d9a] text-white"><Layers3 className="size-5" /></span>
        CRM<span className="-ml-2 text-[#5268b0]">.</span>
      </div>
      <section role={error ? 'alert' : 'status'} aria-live="polite" aria-busy={!error} className="rounded-3xl border border-[#e2e8f4] bg-white px-7 py-10 shadow-[0_24px_70px_-35px_#243d9a] sm:px-10">
        {error ? <>
          <span className="mx-auto grid size-16 place-items-center rounded-2xl bg-[#eef3ff] text-[#243d9a]"><AlertCircle className="size-8" aria-hidden="true" /></span>
          <p className="mt-7 text-xs font-semibold tracking-[.16em] text-[#5268b0]">KẾT NỐI GIÁN ĐOẠN</p>
          <h1 className="mt-3 text-2xl font-semibold tracking-tight">Không thể kiểm tra phiên</h1>
          <p className="mt-3 text-sm leading-6 text-[#718096]">{error}</p>
          <p className="mt-2 text-sm leading-6 text-[#718096]">Vui lòng kiểm tra backend và thử lại.</p>
          <Button type="button" onClick={onRetry} className="mt-7 h-11 w-full bg-[#243d9a] text-white hover:bg-[#1c327f]"><RefreshCw /> Thử lại</Button>
        </> : <>
          <span className="mx-auto grid size-16 place-items-center rounded-2xl bg-[#eef3ff] text-[#243d9a]"><LoaderCircle className="size-8 animate-spin motion-reduce:animate-none" aria-hidden="true" /></span>
          <p className="mt-7 text-xs font-semibold tracking-[.16em] text-[#5268b0]">ĐANG KẾT NỐI</p>
          <h1 className="mt-3 text-2xl font-semibold tracking-tight">Đang kiểm tra phiên đăng nhập</h1>
          <p className="mt-3 text-sm leading-6 text-[#718096]">Vui lòng đợi trong giây lát để tiếp tục vào không gian làm việc.</p>
        </>}
      </section>
      <p className="mt-6 inline-flex items-center gap-2 text-xs text-[#94a3b8]"><ShieldCheck className="size-4" /> Không gian làm việc của bạn</p>
    </div>
  </main>
}
