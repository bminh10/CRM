import { ArrowDown, ArrowRight, BarChart3, CalendarDays, Check, CircleDot, GitBranch, Layers3, UsersRound } from 'lucide-react'
import { Link } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent } from '@/components/ui/card'

const features = [
  { icon: UsersRound, title: 'Khách hàng & công ty', text: 'Thông tin liên hệ luôn sẵn sàng cho những cuộc trò chuyện ý nghĩa.' },
  { icon: GitBranch, title: 'Quy trình bán hàng', text: 'Theo dõi từng cơ hội qua các giai đoạn trong quy trình của bạn.' },
  { icon: CalendarDays, title: 'Lịch & công việc', text: 'Sắp xếp cuộc hẹn và công việc để cả đội cùng tiến về phía trước.' },
  { icon: BarChart3, title: 'Báo cáo tổng quan', text: 'Nắm bắt hoạt động kinh doanh từ dữ liệu của workspace.' },
]

function LandingPage() {
  return (
    <div className="min-h-screen bg-[#f7f8fc] text-[#101c43]">
      <header className="border-b border-[#e2e8f4] bg-white/90">
        <nav className="mx-auto flex h-20 max-w-7xl items-center justify-between gap-5 px-5 sm:px-8" aria-label="Điều hướng chính">
          <Link to="/" className="flex items-center gap-2 text-xl font-bold tracking-tight">
            <span className="grid size-9 place-items-center rounded-xl bg-[#243d9a] text-white"><Layers3 className="size-5" /></span> CRM<span className="text-[#5268b0]">.</span>
          </Link>
          <div className="hidden items-center gap-8 text-sm font-medium text-[#52627c] md:flex">
            <a href="#features" className="hover:text-[#243d9a]">Tính năng</a>
            <a href="#how-it-works" className="hover:text-[#243d9a]">Cách hoạt động</a>
          </div>
          <div className="flex items-center gap-2">
            <Button asChild variant="ghost" className="hidden text-[#243d9a] sm:inline-flex"><Link to="/login">Đăng nhập</Link></Button>
            <Button asChild className="h-10 rounded-full bg-[#243d9a] px-5 text-white hover:bg-[#1c327f]"><Link to="/signup">Bắt đầu ngay <ArrowRight /></Link></Button>
          </div>
        </nav>
      </header>

      <main>
        <section className="relative overflow-hidden bg-[radial-gradient(ellipse_at_75%_35%,#cbdfff_0%,#eef4ff_38%,#f7f8fc_75%)]">
          <div className="mx-auto grid min-h-[620px] max-w-7xl items-center gap-12 px-5 py-20 sm:px-8 lg:grid-cols-2 lg:py-24">
            <div className="relative z-10">
              <Badge variant="outline" className="mb-6 inline-flex items-center gap-2 rounded-full border-[#cbdcf5] bg-white/70 px-4 py-2 text-xs font-semibold tracking-wide text-[#243d9a]"><CircleDot className="size-3.5" /> MỘT KHÔNG GIAN. MỌI CƠ HỘI.</Badge>
              <h1 className="max-w-2xl text-5xl leading-[1.08] font-semibold tracking-[-0.045em] sm:text-6xl xl:text-7xl">Hiểu khách hàng.<br /><span className="text-[#243d9a]">Dẫn lối tăng trưởng.</span></h1>
              <p className="mt-7 max-w-lg text-lg leading-8 text-[#64748b]">Từ liên hệ đầu tiên đến cơ hội thành công, quản lý mọi hoạt động bán hàng của bạn ở một nơi.</p>
              <div className="mt-9 flex flex-wrap gap-3">
                <Button asChild className="h-12 rounded-full bg-[#243d9a] px-6 text-white hover:bg-[#1c327f]"><Link to="/signup">Tạo tài khoản <ArrowRight /></Link></Button>
                <Button asChild variant="outline" className="h-12 rounded-full border-[#abc0e4] bg-transparent px-6 text-[#243d9a] hover:bg-white"><a href="#features">Khám phá tính năng <ArrowDown /></a></Button>
              </div>
              <p className="mt-8 flex items-center gap-2 text-sm text-[#64748b]"><Check className="size-4 text-[#5268b0]" /> Dữ liệu tập trung · Đội ngũ kết nối</p>
            </div>
            <div className="relative mx-auto w-full max-w-xl" aria-label="Minh họa không gian quản lý bán hàng">
              <div className="absolute -inset-10 rounded-full bg-[#a9c8ff]/40 blur-3xl" />
              <div className="relative rotate-[-3deg] rounded-2xl border border-[#d8e4f6] bg-white p-5 shadow-[0_35px_80px_-25px_#243d9a48] sm:p-7">
                <div className="flex items-center justify-between border-b border-[#edf1f8] pb-5"><div><p className="text-xs text-[#8998ae]">KHÔNG GIAN LÀM VIỆC</p><p className="mt-1 font-semibold">Tổng quan bán hàng</p></div><span className="rounded-full bg-[#eaf1ff] px-3 py-1 text-xs font-medium text-[#243d9a]">Tổng quan</span></div>
                <div className="mt-6 grid grid-cols-2 gap-3">{['Khách hàng', 'Cơ hội', 'Công việc', 'Lịch hẹn'].map((name, i) => <div key={name} className="rounded-xl border border-[#edf1f8] bg-[#f8faff] p-4"><div className="mb-5 flex size-9 items-center justify-center rounded-lg bg-[#eaf1ff] text-[#243d9a]">{i === 0 ? <UsersRound className="size-4" /> : i === 1 ? <GitBranch className="size-4" /> : i === 2 ? <Check className="size-4" /> : <CalendarDays className="size-4" />}</div><p className="text-sm font-medium">{name}</p><p className="mt-1 text-xs text-[#94a3b8]">Quản lý tập trung</p></div>)}</div>
                <div className="mt-5 flex items-center gap-3 rounded-xl bg-[#101c43] p-4 text-white"><span className="grid size-9 place-items-center rounded-lg bg-white/10"><BarChart3 className="size-4" /></span><span className="text-sm">Mọi thông tin bạn cần, ở một nơi.</span></div>
              </div>
              <div className="absolute -right-3 -bottom-5 rounded-2xl border border-white bg-[#243d9a] p-4 text-white shadow-xl sm:-right-8"><GitBranch className="size-6" /><p className="mt-2 text-xs font-medium">Tiến về phía trước ↗</p></div>
            </div>
          </div>
        </section>
        <section id="features" className="mx-auto max-w-7xl px-5 py-24 sm:px-8">
          <p className="text-xs font-semibold tracking-[.18em] text-[#5268b0]">ĐƯỢC THIẾT KẾ CHO ĐỘI NGŨ CỦA BẠN</p>
          <div className="mt-4 flex flex-wrap items-end justify-between gap-5"><h2 className="max-w-xl text-3xl font-semibold tracking-tight sm:text-4xl">Ít thao tác hơn.<br />Nhiều kết nối hơn.</h2><p className="max-w-sm text-sm leading-6 text-[#718096]">Một quy trình rõ ràng giúp bạn tập trung vào điều quan trọng nhất: khách hàng.</p></div>
          <div className="mt-10 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">{features.map(({ icon: Icon, title, text }) => <Card key={title} className="rounded-2xl border border-[#e2e8f4] bg-white py-6 ring-0"><CardContent className="px-6"><div className="mb-8 grid size-11 place-items-center rounded-xl bg-[#eaf1ff] text-[#243d9a]"><Icon className="size-5" /></div><h3 className="font-semibold">{title}</h3><p className="mt-3 text-sm leading-6 text-[#718096]">{text}</p></CardContent></Card>)}</div>
        </section>
        <section id="how-it-works" className="mx-auto mb-20 max-w-7xl px-5 sm:px-8"><div className="flex flex-col items-start justify-between gap-6 rounded-3xl bg-[#101c43] px-8 py-12 text-white sm:px-12 md:flex-row md:items-center"><div><p className="text-xs tracking-[.18em] text-[#93c5fd]">SẴN SÀNG BẮT ĐẦU?</p><h2 className="mt-3 text-3xl font-semibold tracking-tight">Cùng xây dựng những mối quan hệ tốt hơn.</h2></div><Button asChild className="h-12 rounded-full bg-white px-6 text-[#243d9a] hover:bg-[#eaf1ff]"><Link to="/signup">Tạo tài khoản <ArrowRight /></Link></Button></div></section>
      </main>
      <footer className="border-t border-[#e2e8f4] py-7 text-center text-xs text-[#8492a8]">CRM · Đồng hành cùng sự phát triển của bạn</footer>
    </div>
  )
}

export default LandingPage
