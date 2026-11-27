import { useCallback, useEffect, useRef, useState } from 'react'
import { Link, NavLink, Route, Routes, useLocation, useSearchParams } from 'react-router-dom'
import { ArrowUpRight, BarChart3, Building2, CalendarDays, CheckSquare, ChevronDown, GitBranch, Layers3, LogOut, Target, UserRound, UsersRound } from 'lucide-react'
import { Button } from '@/components/ui/button'
import InvitationInbox from '@/components/InvitationInbox'
import { ensurePersonalWorkspace, getStatistics, listWorkspaces } from '@/services/crm'
import type { AuthSession } from '@/types/auth'
import type { Statistics, Workspace } from '@/types/crm'
import RecordsPage from './RecordsPage'
import ProfilePage from './ProfilePage'

const links = [
  { to: '/dashboard', label: 'Tổng quan', icon: BarChart3, end: true },
  { to: '/dashboard/contacts', label: 'Khách hàng', icon: UsersRound },
  { to: '/dashboard/companies', label: 'Công ty', icon: Building2 },
  { to: '/dashboard/pipelines', label: 'Quy trình bán hàng', icon: GitBranch },
  { to: '/dashboard/opportunities', label: 'Cơ hội', icon: Target },
  { to: '/dashboard/tasks', label: 'Công việc', icon: CheckSquare },
  { to: '/dashboard/calendar', label: 'Lịch làm việc', icon: CalendarDays },
  { to: '/dashboard/members', label: 'Thành viên', icon: UsersRound },
]

function WorkspacePage({ session, onLogout }: { session: AuthSession; onLogout: () => void }) {
  const location = useLocation()
  const [searchParams, setSearchParams] = useSearchParams()
  const [workspaces, setWorkspaces] = useState<Workspace[]>([])
  const [workspaceId, setWorkspaceId] = useState(() => searchParams.get('workspaceId') ?? '')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [profileMenuOpen, setProfileMenuOpen] = useState(false)
  const profileMenuRef = useRef<HTMLDivElement>(null)

  const loadWorkspaces = useCallback(async () => {
    setLoading(true)
    setError('')
    try {
      let items = await listWorkspaces()
      if (items.length === 0) items = [await ensurePersonalWorkspace()]
      setWorkspaces(items)
      setWorkspaceId(current => items.some(item => item.id === current) ? current : items[0]?.id ?? '')
    } catch (e) { setError(e instanceof Error ? e.message : 'Không thể tải workspace.') }
    finally { setLoading(false) }
  }, [])

  useEffect(() => { void loadWorkspaces() }, [loadWorkspaces])
  useEffect(() => {
    if (!profileMenuOpen) return
    function closeOnOutside(event: PointerEvent) {
      if (profileMenuRef.current && !profileMenuRef.current.contains(event.target as Node))
        setProfileMenuOpen(false)
    }
    function closeOnEscape(event: KeyboardEvent) {
      if (event.key === 'Escape') setProfileMenuOpen(false)
    }
    document.addEventListener('pointerdown', closeOnOutside)
    document.addEventListener('keydown', closeOnEscape)
    return () => {
      document.removeEventListener('pointerdown', closeOnOutside)
      document.removeEventListener('keydown', closeOnEscape)
    }
  }, [profileMenuOpen])
  const currentPage = location.pathname === '/dashboard/profile'
    ? 'Hồ sơ'
    : links.find(link => link.to === location.pathname)?.label ?? 'Workspace'
  const initials = session.user.fullName.trim().split(/\s+/).slice(-2).map(part => part[0]).join('').toUpperCase()

  return <div className="min-h-screen bg-[#eef2f8] text-[#17264a] lg:pl-16">
    <aside className="bg-[#1d337f] lg:fixed lg:inset-y-0 lg:left-0 lg:z-20 lg:flex lg:w-16 lg:flex-col lg:items-center">
      <Link to="/workspaces" aria-label="Chọn workspace" title="Chọn workspace" className="m-2 hidden size-11 place-items-center rounded-xl bg-white text-[#243d9a] shadow-sm lg:grid"><Layers3 className="size-5" /></Link>
      <nav className="flex items-center gap-1 overflow-x-auto px-2 py-2 lg:mt-4 lg:w-full lg:flex-col lg:gap-2" aria-label="CRM">{links.map(({ to, label, icon: Icon, end }) => <NavLink key={to} to={to} end={end} aria-label={label} title={label} className={({ isActive }) => `flex shrink-0 items-center gap-2 rounded-xl px-3 py-2 text-sm transition-colors lg:size-10 lg:justify-center lg:p-0 ${isActive ? 'bg-white/20 text-white' : 'text-blue-100/70 hover:bg-white/10 hover:text-white'}`}><Icon className="size-[18px]" /><span className="lg:sr-only">{label}</span></NavLink>)}</nav>
      <Button variant="ghost" aria-label="Đăng xuất" title="Đăng xuất" onClick={onLogout} className="mb-4 mt-auto hidden size-10 p-0 text-blue-100/70 hover:bg-white/10 hover:text-white lg:flex"><LogOut className="size-[18px]" /></Button>
    </aside>
    <header className="flex min-h-16 flex-wrap items-center gap-3 border-b border-[#e1e8f2] bg-white px-4 py-3 sm:px-7">
      <Link to="/workspaces" className="mr-1 text-lg font-bold tracking-tight text-[#101c43]">CRM<span className="text-[#5268b0]">.</span></Link>
      <label className="sr-only" htmlFor="workspace-select">Chọn workspace</label><select id="workspace-select" value={workspaceId} onChange={e => { setWorkspaceId(e.target.value); setSearchParams({ workspaceId: e.target.value }) }} className="max-w-48 rounded-full border border-[#e3eaf4] bg-white px-3 py-2 text-xs font-medium text-[#243d9a]">{workspaces.length === 0 && <option value="">Đang tải workspace</option>}{workspaces.map(w => <option key={w.id} value={w.id}>{w.name}</option>)}</select>
      <span className="hidden text-sm text-[#9aa8bb] md:inline">/</span><span className="hidden text-sm text-[#718096] md:inline">{currentPage}</span>
      <div className="ml-auto flex items-center gap-2">
        <InvitationInbox onAccepted={() => void loadWorkspaces()} />
        <div ref={profileMenuRef} className="relative">
          <Button type="button" variant="ghost" aria-haspopup="menu" aria-expanded={profileMenuOpen} aria-label="Mở menu người dùng" onClick={() => setProfileMenuOpen(open => !open)} className="h-10 gap-2 px-2 text-[#52627c]">
            <span className="grid size-8 place-items-center rounded-full bg-[#e6edff] text-xs font-bold text-[#243d9a]">{initials}</span>
            <span className="hidden max-w-36 truncate text-sm font-semibold sm:inline">{session.user.fullName}</span>
            <ChevronDown className="size-4" />
          </Button>
          {profileMenuOpen && <div role="menu" aria-label="Menu người dùng" className="absolute right-0 top-full z-50 mt-2 w-56 overflow-hidden rounded-xl border border-[#e1e8f2] bg-white py-1 shadow-lg">
            <div className="border-b border-[#edf1f7] px-4 py-3"><p className="truncate text-sm font-semibold text-[#17264a]">{session.user.fullName}</p><p className="mt-0.5 text-xs text-[#8998ae]">Tài khoản CRM</p></div>
            <Link to="/dashboard/profile" role="menuitem" onClick={() => setProfileMenuOpen(false)} className="flex items-center gap-2 px-4 py-3 text-sm text-[#42536e] hover:bg-[#f4f6fb]">
              <UserRound className="size-4" /> Hồ sơ và bảo mật
            </Link>
          </div>}
        </div>
        <Button variant="ghost" size="icon" aria-label="Đăng xuất" onClick={onLogout} className="text-[#718096] lg:hidden"><LogOut className="size-4" /></Button>
      </div>
    </header>
    <main className="mx-auto max-w-[1600px] px-4 py-5 sm:px-6">
      {error && <div role="alert" className="mb-5 rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700">{error} <button type="button" className="underline" onClick={() => void loadWorkspaces()}>Thử lại</button></div>}
      {loading ? <p className="p-6 text-sm text-slate-500">Đang tải workspace...</p> : <Routes><Route index element={workspaceId ? <Overview workspaceId={workspaceId} userName={session.user.fullName} /> : null} /><Route path="profile" element={<ProfilePage />} /><Route path=":section" element={workspaceId ? <RecordsPage key={`${workspaceId}:${location.pathname}`} workspaceId={workspaceId} /> : null} /></Routes>}
    </main>
  </div>
}

function Overview({ workspaceId, userName }: { workspaceId: string; userName: string }) {
  const [stats, setStats] = useState<Statistics | null>(null)
  const [error, setError] = useState('')
  useEffect(() => {
    let active = true
    setStats(null); setError('')
    getStatistics(workspaceId).then(data => { if (active) setStats(data) }).catch(e => { if (active) setError(e instanceof Error ? e.message : 'Không thể tải báo cáo.') })
    return () => { active = false }
  }, [workspaceId])
  const metrics = [
    { key: 'contacts', label: 'Khách hàng', icon: UsersRound, to: '/dashboard/contacts' },
    { key: 'companies', label: 'Công ty', icon: Building2, to: '/dashboard/companies' },
    { key: 'opportunities', label: 'Cơ hội', icon: Target, to: '/dashboard/opportunities' },
    { key: 'tasks', label: 'Công việc', icon: CheckSquare, to: '/dashboard/tasks' },
    { key: 'events', label: 'Lịch hẹn', icon: CalendarDays, to: '/dashboard/calendar' },
  ] as const
  return <>
    <div className="mb-5"><p className="text-xs font-semibold tracking-[.15em] text-[#5268b0]">KHÔNG GIAN LÀM VIỆC</p><h1 className="mt-1 text-2xl font-semibold tracking-tight text-[#101c43]">Xin chào, {userName}</h1><p className="mt-1 text-sm text-[#8493aa]">Tổng quan hoạt động của bạn, tất cả ở một nơi.</p></div>
    {error ? <p role="alert" className="rounded-2xl bg-white p-6 text-sm text-red-700">{error}</p> : !stats ? <p className="rounded-2xl bg-white p-6 text-sm text-[#718096]">Đang tải báo cáo...</p> : <>
      <div className="grid gap-4 lg:grid-cols-[190px_minmax(0,1fr)_250px]">
        <section className="rounded-2xl bg-white p-5"><h2 className="text-sm font-semibold">Không gian</h2><p className="mt-1 text-xs text-[#9aa8bb]">Truy cập nhanh</p><div className="mt-5 space-y-1">{metrics.map(({ key, label, to }) => <Link key={key} to={to} className="flex items-center justify-between rounded-lg px-3 py-2.5 text-sm text-[#52627c] hover:bg-[#eff4ff] hover:text-[#243d9a]"><span>{label}</span><span className="text-xs text-[#9baac0]">{stats[key]}</span></Link>)}</div></section>
        <section className="rounded-2xl bg-white p-5 sm:p-6"><div className="flex items-start justify-between gap-3"><div><p className="text-xs font-semibold tracking-widest text-[#7d8faa]">HOẠT ĐỘNG CRM</p><h2 className="mt-2 text-lg font-semibold">Tổng quan workspace</h2><p className="mt-1 text-sm text-[#8998ae]">Số liệu hiện tại từ không gian làm việc của bạn.</p></div><span className="rounded-full bg-[#edf3ff] px-3 py-1 text-xs font-medium text-[#243d9a]">Đang hoạt động</span></div><div className="mt-7 space-y-5">{metrics.map(({ key, label, icon: Icon, to }) => <Link key={key} to={to} className="group flex items-center gap-3"><span className="grid size-10 shrink-0 place-items-center rounded-xl bg-[#f1f5ff] text-[#5268b0]"><Icon className="size-4" /></span><span className="min-w-0 flex-1 border-b border-[#edf1f7] pb-3"><span className="block text-sm font-medium">{label}</span><span className="mt-1 block text-xs text-[#9aa8bb]">Xem danh sách</span></span><span className="font-semibold text-[#243d9a]">{stats[key]}</span><ArrowUpRight className="size-4 text-[#bcc7d7] group-hover:text-[#243d9a]" /></Link>)}</div></section>
        <section className="rounded-2xl bg-white p-5"><h2 className="text-sm font-semibold">Bắt đầu từ đây</h2><p className="mt-2 text-xs leading-5 text-[#8998ae]">Tập trung thông tin và theo dõi các cơ hội của bạn.</p><div className="mt-5 space-y-3"><Link to="/dashboard/contacts" className="block rounded-xl border border-[#e8edf5] p-4 hover:border-[#c5d5f3]"><UsersRound className="size-4 text-[#5268b0]" /><p className="mt-3 text-sm font-semibold">Khách hàng</p><p className="mt-1 text-xs text-[#8998ae]">Quản lý liên hệ</p></Link><Link to="/dashboard/pipelines" className="block rounded-xl border border-[#e8edf5] p-4 hover:border-[#c5d5f3]"><GitBranch className="size-4 text-[#5268b0]" /><p className="mt-3 text-sm font-semibold">Lộ trình bán hàng</p><p className="mt-1 text-xs text-[#8998ae]">Theo dõi từng giai đoạn</p></Link></div></section>
      </div>
      <div className="mt-4 grid grid-cols-2 gap-3 md:grid-cols-5">{metrics.map(({ key, label, icon: Icon }) => <div key={key} className="flex items-center gap-3 rounded-2xl bg-white p-4"><span className="grid size-10 place-items-center rounded-xl bg-[#eef4ff] text-[#5268b0]"><Icon className="size-4" /></span><div><p className="text-xs text-[#8998ae]">{label}</p><p className="mt-0.5 text-lg font-semibold">{stats[key]}</p></div></div>)}</div>
    </>}
  </>
}

export default WorkspacePage
