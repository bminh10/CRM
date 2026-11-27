import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { Archive, ArchiveRestore, ArrowRight, BriefcaseBusiness, Check, Copy, Layers3, LogOut, MailPlus, Pencil, Plus, UsersRound } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import InvitationInbox from '@/components/InvitationInbox'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { archiveWorkspace, createWorkspace, createWorkspaceInvitation, listArchivedWorkspaces, listWorkspaces, restoreWorkspace, updateWorkspace } from '@/services/crm'
import type { AuthSession } from '@/types/auth'
import type { Invitation, Workspace } from '@/types/crm'

function WorkspaceHub({ session, onLogout }: { session: AuthSession | null; onLogout: () => void }) {
  const navigate = useNavigate()
  const [workspaces, setWorkspaces] = useState<Workspace[]>([])
  const [archivedWorkspaces, setArchivedWorkspaces] = useState<Workspace[]>([])
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [inviteFor, setInviteFor] = useState('')
  const [email, setEmail] = useState('')
  const [invite, setInvite] = useState<Invitation | null>(null)
  const [copied, setCopied] = useState(false)
  const [editingWorkspaceId, setEditingWorkspaceId] = useState('')
  const [editName, setEditName] = useState('')
  const [editDescription, setEditDescription] = useState('')

  const loadWorkspaces = useCallback(async () => {
    try {
      const [active, archived] = await Promise.all([listWorkspaces(), listArchivedWorkspaces()])
      setWorkspaces(active)
      setArchivedWorkspaces(archived)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Không thể tải workspace.')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => { void loadWorkspaces() }, [loadWorkspaces])

  async function submitWorkspace(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (saving || !name.trim()) return
    setSaving(true)
    setError('')
    try {
      const workspace = await createWorkspace(name.trim(), description.trim())
      setWorkspaces(current => [...current, workspace])
      setName('')
      setDescription('')
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Không thể tạo workspace.')
    } finally {
      setSaving(false)
    }
  }

  async function submitInvitation(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!inviteFor || saving) return
    setSaving(true)
    setError('')
    setInvite(null)
    setCopied(false)
    try {
      setInvite(await createWorkspaceInvitation(inviteFor, email.trim()))
      setEmail('')
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Không thể tạo lời mời.')
    } finally {
      setSaving(false)
    }
  }

  async function submitWorkspaceEdit(event: FormEvent<HTMLFormElement>, workspaceId: string) {
    event.preventDefault()
    if (saving || !editName.trim()) return
    setSaving(true); setError('')
    try {
      const updated = await updateWorkspace(workspaceId, editName.trim(), editDescription.trim())
      setWorkspaces(current => current.map(item => item.id === workspaceId ? updated : item))
      setEditingWorkspaceId('')
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Không thể cập nhật workspace.')
    } finally { setSaving(false) }
  }

  async function archive(workspace: Workspace) {
    if (!window.confirm(`Lưu trữ workspace "${workspace.name}"? Dữ liệu sẽ được giữ lại và có thể khôi phục.`)) return
    setSaving(true); setError('')
    try {
      await archiveWorkspace(workspace.id)
      await loadWorkspaces()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Không thể lưu trữ workspace.')
    } finally { setSaving(false) }
  }

  async function restore(workspace: Workspace) {
    setSaving(true); setError('')
    try {
      await restoreWorkspace(workspace.id)
      await loadWorkspaces()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Không thể khôi phục workspace.')
    } finally { setSaving(false) }
  }

  async function copyInvitation() {
    if (!invite?.token) {
      setError('Máy chủ chưa trả về liên kết lời mời.')
      return
    }
    try {
      await navigator.clipboard.writeText(`${window.location.origin}/invite/${invite.token}`)
      setCopied(true)
      setError('')
    } catch {
      setError('Không thể sao chép liên kết. Hãy thử lại trong trình duyệt hỗ trợ clipboard.')
    }
  }

  function openWorkspace(workspaceId: string) {
    navigate(`/dashboard?workspaceId=${encodeURIComponent(workspaceId)}`)
  }

  return <main className="min-h-screen bg-[#f7f8fc] text-[#17264a]">
    <header className="border-b border-[#e7ebf3] bg-white">
      <div className="mx-auto flex h-[72px] max-w-7xl items-center justify-between px-5 sm:px-8">
        <Link to="/workspaces" className="flex items-center gap-2.5 text-lg font-bold tracking-tight text-[#101c43]">
          <span className="grid size-9 place-items-center rounded-xl bg-[#243d9a] text-white"><Layers3 className="size-[18px]" /></span>
          CRM<span className="text-[#5268b0]">.</span>
        </Link>
        <div className="flex items-center gap-2">
          <InvitationInbox onAccepted={() => { setLoading(true); setError(''); void loadWorkspaces() }} />
          <Badge variant="outline" className="hidden gap-1.5 rounded-full border-[#e4e9f2] bg-[#fbfcff] px-3 py-1.5 text-[#52627c] sm:inline-flex"><UsersRound className="size-3.5" /> Khu vực đội ngũ</Badge>
          <span className="hidden max-w-36 truncate text-sm font-medium text-[#52627c] md:inline">{session?.user.fullName}</span>
          <Button type="button" variant="ghost" size="icon" aria-label="Đăng xuất" title="Đăng xuất" onClick={onLogout} className="text-[#718096]"><LogOut /></Button>
        </div>
      </div>
    </header>

    <div className="mx-auto grid max-w-7xl gap-8 px-5 py-9 sm:px-8 lg:grid-cols-[minmax(0,1fr)_340px] lg:gap-12 lg:py-14">
      <section>
        <div className="mb-8 max-w-2xl">
          <p className="text-xs font-bold tracking-[.18em] text-[#5268b0]">CHỌN KHÔNG GIAN</p>
          <h1 className="mt-3 text-3xl font-semibold tracking-tight text-[#101c43] sm:text-4xl">Bạn muốn làm việc ở đâu?</h1>
          <p className="mt-3 text-sm leading-6 text-[#718096]">Mỗi workspace là một không gian riêng để quản lý khách hàng, quy trình và cộng tác cùng đội ngũ.</p>
        </div>

        {error && <p role="alert" className="mb-5 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">{error}</p>}
        {loading ? <p className="rounded-2xl border border-[#e7ebf3] bg-white p-8 text-sm text-[#718096]">Đang tải workspace...</p> :
          workspaces.length ? <div className="grid gap-4 sm:grid-cols-2">
            {workspaces.map((workspace, index) => {
              const isInviting = inviteFor === workspace.id
              const palette = index % 3
              return <Card key={workspace.id} className="group rounded-2xl border-[#e7ebf3] bg-white py-0 ring-0 transition-all hover:-translate-y-0.5 hover:border-[#cbd7f2] hover:shadow-[0_16px_40px_-28px_#243d9a]">
                <CardContent className="p-5">
                  <div className="flex items-start justify-between gap-3">
                    <span className={`grid size-12 place-items-center rounded-2xl ${palette === 0 ? 'bg-[#eaf1ff] text-[#243d9a]' : palette === 1 ? 'bg-[#eaf7f2] text-[#218065]' : 'bg-[#fff4e8] text-[#b86720]'}`}><BriefcaseBusiness className="size-5" /></span>
                    <Badge variant="secondary" className="rounded-full bg-[#f4f6fa] text-[10px] font-medium text-[#718096]">Workspace</Badge>
                  </div>
                  <h2 className="mt-5 truncate text-lg font-semibold text-[#17264a]">{workspace.name}</h2>
                  <p className="mt-1 text-xs text-[#8a97aa]">Không gian làm việc của đội ngũ</p>
                  <div className="mt-5 flex gap-2">
                    <Button className="h-9 flex-1 justify-between bg-[#243d9a] px-3 text-white hover:bg-[#1c327f]" onClick={() => openWorkspace(workspace.id)}>Mở workspace <ArrowRight /></Button>
                    {workspace.role === 'owner' && <>
                      <Button variant="outline" size="icon" aria-label={`Sửa ${workspace.name}`} title="Sửa workspace" onClick={() => { setEditingWorkspaceId(editingWorkspaceId === workspace.id ? '' : workspace.id); setEditName(workspace.name); setEditDescription(workspace.description ?? ''); setError('') }}><Pencil /></Button>
                      <Button variant="outline" size="icon" aria-label={`Lưu trữ ${workspace.name}`} title="Lưu trữ workspace" disabled={saving} onClick={() => void archive(workspace)}><Archive /></Button>
                      <Button variant="outline" size="icon" aria-label={`Mời thành viên vào ${workspace.name}`} title="Mời thành viên" onClick={() => { setInviteFor(isInviting ? '' : workspace.id); setInvite(null); setError('') }}><MailPlus /></Button>
                    </>}
                  </div>
                  {editingWorkspaceId === workspace.id && <form onSubmit={event => void submitWorkspaceEdit(event, workspace.id)} className="mt-5 space-y-3 border-t border-[#edf0f5] pt-4">
                    <div className="space-y-2"><Label htmlFor={`edit-workspace-name-${workspace.id}`}>Tên workspace</Label><Input id={`edit-workspace-name-${workspace.id}`} required maxLength={150} value={editName} onChange={event => setEditName(event.target.value)} /></div>
                    <div className="space-y-2"><Label htmlFor={`edit-workspace-description-${workspace.id}`}>Mô tả</Label><Input id={`edit-workspace-description-${workspace.id}`} maxLength={500} value={editDescription} onChange={event => setEditDescription(event.target.value)} /></div>
                    <div className="flex gap-2"><Button type="submit" disabled={saving} size="sm" className="bg-[#243d9a] text-white hover:bg-[#1c327f]">{saving ? 'Đang lưu...' : 'Lưu thay đổi'}</Button><Button type="button" variant="outline" size="sm" disabled={saving} onClick={() => setEditingWorkspaceId('')}>Hủy</Button></div>
                  </form>}
                  {isInviting && <form onSubmit={submitInvitation} className="mt-5 border-t border-[#edf0f5] pt-4">
                    <Label htmlFor={`invite-${workspace.id}`} className="text-xs font-semibold text-[#52627c]">Mời bằng email</Label>
                    <div className="mt-2 flex gap-2">
                      <Input id={`invite-${workspace.id}`} type="email" required value={email} onChange={event => setEmail(event.target.value)} placeholder="ten@congty.com" className="h-9 bg-white text-sm" />
                      <Button type="submit" disabled={saving} size="icon" aria-label="Tạo lời mời" title="Tạo lời mời"><ArrowRight /></Button>
                    </div>
                    {invite?.workspaceId === workspace.id && <div className="mt-3 rounded-xl bg-[#f0f8f4] p-3">
                      <p className="flex items-center gap-1.5 text-xs font-medium text-[#28745e]"><Check className="size-3.5" /> Đã tạo lời mời cho {invite.email}</p>
                      <Button type="button" variant="outline" size="sm" onClick={() => void copyInvitation()} className="mt-2 h-8 gap-1.5 border-[#cfe6da] bg-white text-[#28745e]">{copied ? <Check /> : <Copy />}{copied ? 'Đã sao chép liên kết' : 'Sao chép liên kết mời'}</Button>
                    </div>}
                  </form>}
                </CardContent>
              </Card>
            })}
          </div> : <div className="rounded-2xl border border-dashed border-[#cbd5e5] bg-white px-6 py-12 text-center">
            <span className="mx-auto grid size-12 place-items-center rounded-2xl bg-[#eef3ff] text-[#5268b0]"><BriefcaseBusiness className="size-5" /></span>
            <h2 className="mt-4 font-semibold text-[#17264a]">Chưa có workspace nào</h2>
            <p className="mx-auto mt-2 max-w-sm text-sm leading-6 text-[#718096]">Tạo workspace đầu tiên ở bên phải, hoặc nhờ quản trị viên gửi lời mời tham gia.</p>
          </div>}
        {archivedWorkspaces.length > 0 && <section className="mt-8">
          <h2 className="text-sm font-semibold text-[#52627c]">Workspace đã lưu trữ</h2>
          <div className="mt-3 grid gap-3 sm:grid-cols-2">
            {archivedWorkspaces.map(workspace => <div key={workspace.id} className="flex items-center justify-between gap-3 rounded-2xl border border-dashed border-[#d6deea] bg-white p-4">
              <div className="min-w-0"><p className="truncate text-sm font-semibold text-[#52627c]">{workspace.name}</p><p className="mt-1 text-xs text-[#8998ae]">Dữ liệu được giữ nguyên</p></div>
              <Button type="button" variant="outline" size="sm" disabled={saving} onClick={() => void restore(workspace)}><ArchiveRestore className="size-4" /> Khôi phục</Button>
            </div>)}
          </div>
        </section>}
      </section>

      <aside className="lg:pt-[3.55rem]">
        <Card className="overflow-hidden rounded-2xl border-0 bg-[#15264f] py-0 text-white ring-0 shadow-[0_28px_60px_-36px_#15264f]">
          <div className="relative overflow-hidden px-6 pb-6 pt-7">
            <div aria-hidden="true" className="pointer-events-none absolute -right-14 -top-20 size-48 rounded-full border border-white/10" />
            <div aria-hidden="true" className="pointer-events-none absolute -right-5 -top-11 size-32 rounded-full border border-white/10" />
            <Badge className="border border-white/15 bg-white/10 text-blue-100 hover:bg-white/10"><Plus className="mr-1 size-3" /> TẠO MỚI</Badge>
            <h2 className="mt-5 text-xl font-semibold tracking-tight">Tạo workspace</h2>
            <p className="mt-2 text-sm leading-6 text-blue-100/70">Bắt đầu một không gian riêng cho nhóm hoặc dự án mới.</p>
          </div>
          <CardContent className="bg-white p-6 text-[#17264a]">
            <form onSubmit={submitWorkspace} className="space-y-4">
              <div className="space-y-2"><Label htmlFor="workspace-name">Tên workspace</Label><Input id="workspace-name" required maxLength={150} value={name} onChange={event => setName(event.target.value)} placeholder="Ví dụ: Đội ngũ kinh doanh" className="h-10 bg-white" /></div>
              <div className="space-y-2"><Label htmlFor="workspace-description">Mô tả <span className="font-normal text-[#8a97aa]">(không bắt buộc)</span></Label><Input id="workspace-description" maxLength={500} value={description} onChange={event => setDescription(event.target.value)} placeholder="Mục tiêu hoặc phạm vi workspace" className="h-10 bg-white" /></div>
              <Button type="submit" disabled={saving} className="h-10 w-full bg-[#243d9a] text-white hover:bg-[#1c327f]">{saving ? 'Đang tạo...' : 'Tạo workspace'} <ArrowRight /></Button>
            </form>
          </CardContent>
        </Card>
        <p className="mt-4 flex items-start gap-2 px-1 text-xs leading-5 text-[#8a97aa]"><UsersRound className="mt-0.5 size-3.5 shrink-0" /> Bạn có thể mời đồng đội vào từng workspace sau khi tạo.</p>
      </aside>
    </div>
  </main>
}

export default WorkspaceHub
