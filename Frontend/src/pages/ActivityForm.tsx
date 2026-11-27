import { useEffect, useState, type FormEvent } from 'react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { createActivity, listOpportunities } from '@/services/crm'
import type { OpportunityOption } from '@/types/crm'


function ActivityForm({ workspaceId, kind, onSaved, onCancel }: {
  workspaceId: string; kind: 'tasks' | 'calendar'; onSaved: () => void; onCancel: () => void
}) {
  const [opportunities, setOpportunities] = useState<OpportunityOption[]>([])
  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [opportunityId, setOpportunityId] = useState('')
  const [type, setType] = useState('offline')
  const [location, setLocation] = useState('')
  const [meetingUrl, setMeetingUrl] = useState('')
  const [start, setStart] = useState('')
  const [end, setEnd] = useState('')
  const [priority, setPriority] = useState('medium')
  const [error, setError] = useState('')
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    let active = true
    listOpportunities(workspaceId)
      .then(data => { if (active) setOpportunities(data) })
      .catch(e => { if (active) setError(e instanceof Error ? e.message : 'Không thể tải cơ hội.') })
    return () => { active = false }
  }, [workspaceId])

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (saving) return
    if (!title.trim()) { setError('Vui lòng nhập tiêu đề.'); return }
    if (kind === 'calendar' && (!start || !end || new Date(end) <= new Date(start))) { setError('Thời gian kết thúc phải sau thời gian bắt đầu.'); return }
    if (kind === 'tasks' && start && end && new Date(end) < new Date(start)) { setError('Hạn hoàn thành không được trước thời gian bắt đầu.'); return }
    setSaving(true); setError('')
    const body = kind === 'calendar'
      ? { workspaceId, title: title.trim(), description: description.trim() || null, opportunityId: opportunityId || null, companyId: null, eventType: type, location: location.trim() || null, meetingUrl: meetingUrl.trim() || null, startTime: new Date(start).toISOString(), endTime: new Date(end).toISOString(), attendeeUserIds: [] }
      : { workspaceId, title: title.trim(), description: description.trim() || null, opportunityId: opportunityId || null, priority, startDate: start ? new Date(start).toISOString() : null, dueDate: end ? new Date(end).toISOString() : null, assigneeUserIds: [] }
    try { await createActivity(kind, body); onSaved() }
    catch (e) { setError(e instanceof Error ? e.message : 'Không thể lưu.') }
    finally { setSaving(false) }
  }
  const selectClass = 'h-10 w-full rounded-lg border border-input bg-white px-3 text-sm'
  return <form onSubmit={submit} className="mt-6 rounded-2xl border border-[#e2e8f4] bg-white p-6"><h2 className="mb-5 text-lg font-semibold">{kind === 'tasks' ? 'Tạo công việc' : 'Tạo lịch hẹn'}</h2><div className="grid gap-4 sm:grid-cols-2">
    <div className="space-y-2"><Label htmlFor="activity-title">Tiêu đề *</Label><Input id="activity-title" required value={title} onChange={e => setTitle(e.target.value)} /></div>
    <div className="space-y-2"><Label htmlFor="activity-opportunity">Cơ hội liên quan</Label><select id="activity-opportunity" className={selectClass} value={opportunityId} onChange={e => setOpportunityId(e.target.value)}><option value="">Không chọn</option>{opportunities.map(item => <option key={item.id} value={item.id}>{item.title}</option>)}</select></div>
    <div className="space-y-2"><Label htmlFor="activity-start">Bắt đầu{kind === 'calendar' && ' *'}</Label><Input id="activity-start" type="datetime-local" required={kind === 'calendar'} value={start} onChange={e => setStart(e.target.value)} /></div>
    <div className="space-y-2"><Label htmlFor="activity-end">{kind === 'calendar' ? 'Kết thúc *' : 'Hạn hoàn thành'}</Label><Input id="activity-end" type="datetime-local" required={kind === 'calendar'} value={end} onChange={e => setEnd(e.target.value)} /></div>
    {kind === 'calendar' ? <><div className="space-y-2"><Label htmlFor="activity-type">Hình thức</Label><select id="activity-type" className={selectClass} value={type} onChange={e => setType(e.target.value)}><option value="offline">Trực tiếp</option><option value="online">Trực tuyến</option><option value="phone">Điện thoại</option></select></div><div className="space-y-2"><Label htmlFor="activity-location">Địa điểm</Label><Input id="activity-location" value={location} onChange={e => setLocation(e.target.value)} /></div><div className="space-y-2"><Label htmlFor="activity-url">Liên kết cuộc họp</Label><Input id="activity-url" type="url" value={meetingUrl} onChange={e => setMeetingUrl(e.target.value)} /></div></> : <div className="space-y-2"><Label htmlFor="activity-priority">Ưu tiên</Label><select id="activity-priority" className={selectClass} value={priority} onChange={e => setPriority(e.target.value)}><option value="low">Thấp</option><option value="medium">Trung bình</option><option value="high">Cao</option><option value="urgent">Khẩn cấp</option></select></div>}
    <div className="space-y-2"><Label htmlFor="activity-description">Mô tả / kế hoạch</Label><Input id="activity-description" value={description} onChange={e => setDescription(e.target.value)} /></div>
  </div>{error && <p role="alert" className="mt-4 text-sm text-red-700">{error}</p>}<div className="mt-5 flex gap-2"><Button disabled={saving} className="bg-[#243d9a] text-white hover:bg-[#1c327f]">{saving ? 'Đang lưu...' : 'Lưu'}</Button><Button type="button" variant="outline" onClick={onCancel}>Hủy</Button></div></form>
}

export default ActivityForm
