import { useCallback, useEffect, useState } from 'react'
import { Check, Inbox, RefreshCw, X } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { listIncomingInvitations, respondToInvitation } from '@/services/crm'
import type { IncomingInvitation } from '@/types/crm'

function InvitationInbox({ onAccepted }: { onAccepted: () => void }) {
  const [invitations, setInvitations] = useState<IncomingInvitation[]>([])
  const [loading, setLoading] = useState(true)
  const [open, setOpen] = useState(false)
  const [responding, setResponding] = useState('')
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')

  const loadInvitations = useCallback(async () => {
    try {
      setInvitations(await listIncomingInvitations())
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Không thể tải lời mời.')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => { void loadInvitations() }, [loadInvitations])

  async function respond(invitation: IncomingInvitation, response: 'accept' | 'reject') {
    if (responding) return
    setResponding(invitation.id)
    setError('')
    setMessage('')
    try {
      await respondToInvitation(invitation.id, response)
      setInvitations(current => current.filter(item => item.id !== invitation.id))
      setMessage(response === 'accept' ? `Đã tham gia ${invitation.workspaceName}.` : 'Đã từ chối lời mời.')
      if (response === 'accept') onAccepted()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Không thể xử lý lời mời.')
    } finally {
      setResponding('')
    }
  }

  return <div className="relative">
    <Button type="button" variant="ghost" size="icon" aria-label={`Hòm thư lời mời${invitations.length ? `, ${invitations.length} lời mời mới` : ''}`} aria-expanded={open} aria-controls="invitation-inbox" title="Hòm thư lời mời" onClick={() => { const next = !open; setOpen(next); if (next) { setError(''); setLoading(true); void loadInvitations() } }} className="relative text-[#52627c]">
      <Inbox />
      {invitations.length > 0 && <Badge className="absolute -right-1 -top-1 min-w-5 justify-center rounded-full border-2 border-white bg-[#d94a59] px-1 text-[10px] leading-4 text-white">{invitations.length > 9 ? '9+' : invitations.length}</Badge>}
    </Button>
    {open && <section id="invitation-inbox" aria-label="Hòm thư lời mời" className="absolute right-0 top-12 z-50 w-[min(90vw,390px)] overflow-hidden rounded-2xl border border-[#e5eaf2] bg-white text-[#17264a] shadow-[0_24px_70px_-30px_#15264f55]">
      <header className="flex items-center justify-between border-b border-[#edf1f8] px-4 py-3.5">
        <div><h2 className="text-sm font-semibold">Hòm thư lời mời</h2><p className="mt-0.5 text-xs text-[#8a97aa]">{invitations.length} lời mời đang chờ</p></div>
        <Button type="button" variant="ghost" size="icon-sm" aria-label="Tải lại lời mời" title="Tải lại" disabled={loading} onClick={() => { setError(''); setLoading(true); void loadInvitations() }}><RefreshCw className={loading ? 'animate-spin' : ''} /></Button>
      </header>
      {error && <p role="alert" className="mx-3 mt-3 rounded-lg bg-red-50 px-3 py-2 text-xs text-red-700">{error}</p>}
      {message && <p role="status" className="mx-3 mt-3 rounded-lg bg-[#eff8f3] px-3 py-2 text-xs text-[#28745e]">{message}</p>}
      <div className="max-h-[min(65vh,440px)] overflow-y-auto p-3">
        {loading ? <p className="px-3 py-8 text-center text-sm text-[#8491a5]">Đang tải lời mời...</p> :
          invitations.length === 0 ? <div className="px-4 py-8 text-center"><span className="mx-auto grid size-11 place-items-center rounded-2xl bg-[#f3f6fb] text-[#8291a7]"><Inbox className="size-5" /></span><p className="mt-3 text-sm font-medium text-[#52627c]">Hòm thư đang trống</p><p className="mt-1 text-xs text-[#98a4b5]">Lời mời tham gia workspace sẽ xuất hiện ở đây.</p></div> :
            <ul className="space-y-2">{invitations.map(invitation => <li key={invitation.id} className="rounded-xl border border-[#e8edf5] p-3.5">
              <div className="flex items-start gap-3">
                <span className="grid size-9 shrink-0 place-items-center rounded-xl bg-[#eef3ff] text-[#5268b0]"><Inbox className="size-4" /></span>
                <div className="min-w-0 flex-1">
                  <p className="text-sm font-semibold text-[#253452]">{invitation.workspaceName}</p>
                  <p className="mt-1 text-xs leading-5 text-[#748198]">{invitation.invitedByName} mời bạn tham gia bằng email {invitation.email}.</p>
                  <p className="mt-1 text-[11px] text-[#9aa6b8]">Hết hạn {new Intl.DateTimeFormat('vi-VN', { dateStyle: 'medium' }).format(new Date(invitation.expiresAt))}</p>
                </div>
              </div>
              <div className="mt-3 flex justify-end gap-2">
                <Button type="button" variant="outline" size="sm" disabled={!!responding} onClick={() => void respond(invitation, 'reject')} className="h-8"><X /> Từ chối</Button>
                <Button type="button" size="sm" disabled={!!responding} onClick={() => void respond(invitation, 'accept')} className="h-8 bg-[#243d9a] text-white hover:bg-[#1c327f]">{responding === invitation.id ? 'Đang xử lý...' : <><Check /> Chấp nhận</>}</Button>
              </div>
            </li>)}</ul>}
      </div>
    </section>}
  </div>
}

export default InvitationInbox
