import { useEffect, useState, type FormEvent } from 'react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select } from '@/components/ui/select'
import { listOpportunities, saveActivity } from '@/services/crm'
import type { OpportunityOption, RecordItem } from '@/types/crm'

function stringValue(value: RecordItem[string] | undefined) {
  return typeof value === 'string' ? value : ''
}

function dateValue(value: RecordItem[string] | undefined) {
  const date = typeof value === 'string' ? new Date(value) : null
  if (!date || Number.isNaN(date.getTime())) return ''
  return new Date(date.getTime() - date.getTimezoneOffset() * 60_000).toISOString().slice(0, 16)
}

function ActivityForm({ workspaceId, kind, item, onSaved, onCancel }: {
  workspaceId: string; kind: 'tasks' | 'calendar'; item: RecordItem | null; onSaved: () => void; onCancel: () => void
}) {
  const [opportunities, setOpportunities] = useState<OpportunityOption[]>([])
  const [title, setTitle] = useState(() => stringValue(item?.title))
  const [description, setDescription] = useState(() => stringValue(item?.description))
  const [opportunityId, setOpportunityId] = useState(() => stringValue(item?.opportunityId))
  const [type, setType] = useState(() => stringValue(item?.eventType) || 'offline')
  const [location, setLocation] = useState(() => stringValue(item?.location))
  const [meetingUrl, setMeetingUrl] = useState(() => stringValue(item?.meetingUrl))
  const [start, setStart] = useState(() => dateValue(kind === 'calendar' ? item?.startTime : item?.startDate))
  const [end, setEnd] = useState(() => dateValue(kind === 'calendar' ? item?.endTime : item?.dueDate))
  const [priority, setPriority] = useState(() => stringValue(item?.priority) || 'medium')
  const [status, setStatus] = useState(() => stringValue(item?.status) || 'todo')
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

    setSaving(true)
    setError('')
    const body = kind === 'calendar'
      ? {
          workspaceId, companyId: item?.companyId ?? null, title: title.trim(), description: description.trim() || null,
          opportunityId: opportunityId || null, eventType: type, location: location.trim() || null,
          meetingUrl: meetingUrl.trim() || null, startTime: new Date(start).toISOString(), endTime: new Date(end).toISOString(),
          attendeeUserIds: Array.isArray(item?.attendeeUserIds) ? item.attendeeUserIds : [],
        }
      : {
          workspaceId, title: title.trim(), description: description.trim() || null, opportunityId: opportunityId || null,
          priority, status, startDate: start ? new Date(start).toISOString() : null,
          dueDate: end ? new Date(end).toISOString() : null,
          assigneeUserIds: Array.isArray(item?.assigneeUserIds) ? item.assigneeUserIds : [],
        }
    try {
      await saveActivity(kind, item?.id, body)
      onSaved()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Không thể lưu.')
    } finally {
      setSaving(false)
    }
  }

  return <form onSubmit={submit} className="mt-6 rounded-2xl border border-[#e2e8f4] bg-white p-6">
    <h2 className="mb-5 text-lg font-semibold">{item ? 'Chỉnh sửa' : 'Tạo'} {kind === 'tasks' ? 'công việc' : 'lịch hẹn'}</h2>
    <div className="grid gap-4 sm:grid-cols-2">
      <div className="space-y-2"><Label htmlFor="activity-title">Tiêu đề *</Label><Input id="activity-title" required value={title} onChange={event => setTitle(event.target.value)} /></div>
      <div className="space-y-2"><Label htmlFor="activity-opportunity">Cơ hội liên quan</Label><Select id="activity-opportunity" value={opportunityId} onValueChange={setOpportunityId} options={[{ value: '', label: 'Không chọn' }, ...opportunities.map(opportunity => ({ value: opportunity.id, label: opportunity.title }))]} /></div>
      <div className="space-y-2"><Label htmlFor="activity-start">Bắt đầu{kind === 'calendar' && ' *'}</Label><Input id="activity-start" type="datetime-local" required={kind === 'calendar'} value={start} onChange={event => setStart(event.target.value)} /></div>
      <div className="space-y-2"><Label htmlFor="activity-end">{kind === 'calendar' ? 'Kết thúc *' : 'Hạn hoàn thành'}</Label><Input id="activity-end" type="datetime-local" required={kind === 'calendar'} value={end} onChange={event => setEnd(event.target.value)} /></div>
      {kind === 'calendar' ? <>
        <div className="space-y-2"><Label htmlFor="activity-type">Hình thức</Label><Select id="activity-type" value={type} onValueChange={setType} options={[{ value: 'offline', label: 'Trực tiếp' }, { value: 'online', label: 'Trực tuyến' }, { value: 'phone', label: 'Điện thoại' }]} /></div>
        <div className="space-y-2"><Label htmlFor="activity-location">Địa điểm</Label><Input id="activity-location" value={location} onChange={event => setLocation(event.target.value)} /></div>
        <div className="space-y-2"><Label htmlFor="activity-url">Liên kết cuộc họp</Label><Input id="activity-url" type="url" value={meetingUrl} onChange={event => setMeetingUrl(event.target.value)} /></div>
      </> : <>
        <div className="space-y-2"><Label htmlFor="activity-priority">Ưu tiên</Label><Select id="activity-priority" value={priority} onValueChange={setPriority} options={[{ value: 'low', label: 'Thấp' }, { value: 'medium', label: 'Trung bình' }, { value: 'high', label: 'Cao' }, { value: 'urgent', label: 'Khẩn cấp' }]} /></div>
        <div className="space-y-2"><Label htmlFor="activity-status">Trạng thái</Label><Select id="activity-status" value={status} onValueChange={setStatus} options={[{ value: 'todo', label: 'Cần làm' }, { value: 'in_progress', label: 'Đang thực hiện' }, { value: 'done', label: 'Hoàn thành' }, { value: 'cancelled', label: 'Đã hủy' }]} /></div>
      </>}
      <div className="space-y-2 sm:col-span-2"><Label htmlFor="activity-description">Mô tả / kế hoạch</Label><textarea id="activity-description" rows={3} value={description} onChange={event => setDescription(event.target.value)} className="w-full resize-y rounded-lg border border-input bg-white px-3 py-2 text-sm outline-none transition-colors placeholder:text-muted-foreground focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50" /></div>
    </div>
    {error && <p role="alert" className="mt-4 text-sm text-red-700">{error}</p>}
    <div className="mt-5 flex gap-2"><Button disabled={saving} className="bg-[#243d9a] text-white hover:bg-[#1c327f]">{saving ? 'Đang lưu...' : 'Lưu'}</Button><Button type="button" variant="outline" onClick={onCancel}>Hủy</Button></div>
  </form>
}

export default ActivityForm
