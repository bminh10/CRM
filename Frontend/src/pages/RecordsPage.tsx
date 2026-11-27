import { useCallback, useEffect, useRef, useState, type FormEvent } from 'react'
import { useParams } from 'react-router-dom'
import { Plus, RefreshCw } from 'lucide-react'
import PipelineBoard from '@/components/PipelineBoard'
import RecordWorkspace from '@/components/RecordWorkspace'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { addStage as addStageRequest, deleteRecord, deleteStage, listRecords, listStages, renameStage, reorderStages, saveRecord } from '@/services/crm'
import type { RecordItem } from '@/types/crm'
import ActivityForm from './ActivityForm'
import OpportunityForm from './OpportunityForm'

type Field = { key: string; label: string; required?: boolean; type?: string; options?: { value: string; label: string }[] }
type Section = { title: string; description: string; fields: Field[]; columns: string[]; paginated?: boolean; editable?: boolean }

const sections: Record<string, Section> = {
  contacts: { title: 'Khách hàng', description: 'Quản lý các liên hệ trong workspace.', paginated: true, editable: true, columns: ['fullName', 'email', 'phone', 'jobTitle'], fields: [
    { key: 'fullName', label: 'Họ và tên', required: true }, { key: 'email', label: 'Email', type: 'email' }, { key: 'phone', label: 'Điện thoại' }, { key: 'jobTitle', label: 'Chức danh' }, { key: 'notes', label: 'Ghi chú' },
  ] },
  companies: { title: 'Công ty', description: 'Thông tin doanh nghiệp và đối tác của bạn.', editable: true, columns: ['name', 'email', 'phone', 'companyType'], fields: [
    { key: 'name', label: 'Tên công ty', required: true }, { key: 'email', label: 'Email', type: 'email' }, { key: 'phone', label: 'Điện thoại' }, { key: 'taxCode', label: 'Mã số thuế' }, { key: 'website', label: 'Website' }, { key: 'address', label: 'Địa chỉ' }, { key: 'companyType', label: 'Loại', required: true, options: [{ value: 'customer', label: 'Khách hàng' }, { value: 'partner', label: 'Đối tác' }, { value: 'both', label: 'Cả hai' }] }, { key: 'notes', label: 'Ghi chú' },
  ] },
  pipelines: { title: 'Quy trình bán hàng', description: 'Tổ chức quy trình theo các giai đoạn bán hàng.', editable: true, columns: ['name', 'description'], fields: [{ key: 'name', label: 'Tên quy trình', required: true }, { key: 'description', label: 'Mô tả' }] },
  opportunities: { title: 'Cơ hội bán hàng', description: 'Theo dõi các cơ hội của workspace.', editable: true, columns: ['title', 'status', 'expectedValue', 'probability'], fields: [{ key: 'title', label: 'Tên cơ hội' }, { key: 'status', label: 'Trạng thái' }, { key: 'expectedValue', label: 'Giá trị dự kiến' }, { key: 'probability', label: 'Xác suất (%)' }] },
  tasks: { title: 'Công việc', description: 'Theo dõi công việc của đội ngũ.', paginated: true, editable: true, columns: ['title', 'status', 'priority', 'dueDate'], fields: [
    { key: 'title', label: 'Công việc', required: true }, { key: 'description', label: 'Mô tả' }, { key: 'status', label: 'Trạng thái' }, { key: 'priority', label: 'Ưu tiên' }, { key: 'opportunityId', label: 'ID cơ hội liên quan' }, { key: 'startDate', label: 'Bắt đầu' }, { key: 'dueDate', label: 'Hạn hoàn thành' }, { key: 'assigneeUserIds', label: 'ID người được giao' }, { key: 'createdAt', label: 'Ngày tạo' }, { key: 'updatedAt', label: 'Cập nhật lần cuối' },
  ] },
  calendar: { title: 'Lịch làm việc', description: 'Các cuộc hẹn online và offline.', paginated: true, editable: true, columns: ['title', 'eventType', 'startTime', 'location'], fields: [
    { key: 'title', label: 'Cuộc hẹn', required: true }, { key: 'description', label: 'Mô tả' }, { key: 'eventType', label: 'Hình thức' }, { key: 'companyId', label: 'ID công ty liên quan' }, { key: 'opportunityId', label: 'ID cơ hội liên quan' }, { key: 'startTime', label: 'Bắt đầu' }, { key: 'endTime', label: 'Kết thúc' }, { key: 'location', label: 'Địa điểm' }, { key: 'meetingUrl', label: 'Liên kết cuộc họp' }, { key: 'attendeeUserIds', label: 'ID người tham dự' }, { key: 'createdAt', label: 'Ngày tạo' }, { key: 'updatedAt', label: 'Cập nhật lần cuối' },
  ] },
  members: { title: 'Thành viên', description: 'Thành viên của workspace. CRUD tài khoản nhân viên chưa được backend hỗ trợ.', columns: ['fullName', 'email', 'role'], fields: [{ key: 'fullName', label: 'Họ và tên' }, { key: 'email', label: 'Email' }, { key: 'role', label: 'Vai trò' }] },
}

const valueLabels: Record<string, string> = {
  open: 'Đang mở', won: 'Thành công', lost: 'Không thành công', pending: 'Chờ xử lý',
  todo: 'Cần làm', in_progress: 'Đang thực hiện', done: 'Hoàn thành', completed: 'Hoàn thành', cancelled: 'Đã hủy',
  low: 'Thấp', medium: 'Trung bình', high: 'Cao', urgent: 'Khẩn cấp',
  offline: 'Trực tiếp', online: 'Trực tuyến', phone: 'Điện thoại',
  owner: 'Chủ sở hữu', editor: 'Có thể chỉnh sửa', viewer: 'Chỉ xem',
  customer: 'Khách hàng', partner: 'Đối tác', both: 'Khách hàng & đối tác',
}

function displayValue(key: string, value: RecordItem[string]) {
  if (value === null || value === undefined || value === '') return '—'
  if (key === 'expectedValue' && typeof value === 'number') return `${new Intl.NumberFormat('vi-VN').format(value)} đ`
  if (key === 'probability') return `${value}%`
  if (['startDate', 'dueDate', 'startTime', 'endTime', 'createdAt', 'updatedAt'].includes(key)) {
    const date = new Date(String(value))
    if (!Number.isNaN(date.getTime())) return new Intl.DateTimeFormat('vi-VN', { dateStyle: 'medium', timeStyle: 'short' }).format(date)
  }
  if (Array.isArray(value)) return value.length ? value.join(', ') : '—'
  if (['status', 'priority', 'eventType', 'role', 'companyType'].includes(key))
    return valueLabels[String(value)] ?? String(value)
  return String(value)
}

function RecordsPage({ workspaceId }: { workspaceId: string }) {
  const { section = '' } = useParams()
  const config = sections[section]
  const [items, setItems] = useState<RecordItem[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [editing, setEditing] = useState<RecordItem | null>(null)
  const [form, setForm] = useState<Record<string, string>>({})
  const [showForm, setShowForm] = useState(false)
  const [saving, setSaving] = useState(false)
  const [page, setPage] = useState(1)
  const [total, setTotal] = useState(0)
  const [stagesFor, setStagesFor] = useState<RecordItem | null>(null)
  const [stages, setStages] = useState<RecordItem[]>([])
  const [stageName, setStageName] = useState('')
  const [stageLoading, setStageLoading] = useState(false)
  const [stageRevision, setStageRevision] = useState(0)
  const requestId = useRef(0)

  const load = useCallback(async () => {
    if (!config) return
    const request = ++requestId.current
    setLoading(true); setError('')
    try {
      const result = await listRecords(section, workspaceId, page)
      if (request !== requestId.current) return
      if (Array.isArray(result)) { setItems(result); setTotal(result.length) }
      else { setItems(result.items); setTotal(result.total) }
    } catch (e) { if (request === requestId.current) setError(e instanceof Error ? e.message : 'Không thể tải dữ liệu.') }
    finally { if (request === requestId.current) setLoading(false) }
  }, [config, section, workspaceId, page])

  useEffect(() => { void load(); return () => { requestId.current++ } }, [load])

  if (!config) return <p>Không tìm thấy trang.</p>

  function openForm(item?: RecordItem) {
    setEditing(item ?? null)
    setForm(Object.fromEntries(config.fields.map(field => [field.key, item?.[field.key]?.toString() ?? (field.key === 'companyType' ? 'customer' : '')])))
    setShowForm(true); setError('')
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (saving) return
    if (config.fields.some(field => field.required && !form[field.key]?.trim())) { setError('Vui lòng nhập các trường bắt buộc.'); return }
    setSaving(true); setError('')
    try {
      const body: Record<string, unknown> = { ...(editing ?? {}), workspaceId }
      for (const field of config.fields) body[field.key] = form[field.key]?.trim() || null
      if (section === 'pipelines') body.isDefault = editing?.isDefault ?? false
      await saveRecord(section, workspaceId, editing?.id, body)
      setShowForm(false)
      await load()
    } catch (e) { setError(e instanceof Error ? e.message : 'Không thể lưu dữ liệu.') }
    finally { setSaving(false) }
  }

  async function remove(item: RecordItem) {
    if (!window.confirm('Bạn có chắc muốn xóa mục này?')) return
    setError('')
    try {
      await deleteRecord(section, workspaceId, item.id)
      await load()
    } catch (e) { setError(e instanceof Error ? e.message : 'Không thể xóa dữ liệu.') }
  }

  async function loadStages(pipeline: RecordItem) {
    setStageLoading(true); setError(''); setStagesFor(pipeline); setStages([])
    try { setStages(await listStages<RecordItem>(pipeline.id)); setStageRevision(current => current + 1) }
    catch (e) { setStagesFor(null); setError(e instanceof Error ? e.message : 'Không thể tải giai đoạn.') }
    finally { setStageLoading(false) }
  }

  async function changeStage(stage: RecordItem, action: 'rename' | 'delete' | 'up' | 'down') {
    if (!stagesFor || saving) return
    const index = stages.findIndex(item => item.id === stage.id)
    let update: () => Promise<void> = () => deleteStage(stage.id)
    if (action === 'delete' && !window.confirm('Bạn có chắc muốn xóa giai đoạn này?')) return
    if (action === 'rename') {
      const name = window.prompt('Tên giai đoạn', String(stage.name))
      if (name === null) return
      if (!name.trim()) { setError('Tên giai đoạn không được để trống.'); return }
      update = () => renameStage(stage, name.trim())
    }
    if (action === 'up' || action === 'down') {
      const other = index + (action === 'up' ? -1 : 1)
      if (other < 0 || other >= stages.length) return
      const ids = stages.map(item => item.id)
      ;[ids[index], ids[other]] = [ids[other], ids[index]]
      const pipelineId = stagesFor.id
      update = () => reorderStages(pipelineId, ids)
    }
    setSaving(true); setError('')
    try { await update(); await loadStages(stagesFor) }
    catch (e) { setError(e instanceof Error ? e.message : 'Không thể cập nhật giai đoạn.') }
    finally { setSaving(false) }
  }

  async function addStage(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!stagesFor || !stageName.trim() || saving) return
    setSaving(true); setError('')
    try {
      const position = stages.reduce((max, stage) => Math.max(max, Number(stage.position)), -1) + 1
      await addStageRequest(stagesFor.id, stageName.trim(), position)
      setStageName(''); await loadStages(stagesFor)
    } catch (e) { setError(e instanceof Error ? e.message : 'Không thể thêm giai đoạn.') }
    finally { setSaving(false) }
  }

  return <section><div className="flex flex-wrap items-end justify-between gap-4"><div><p className="text-xs font-semibold tracking-[.15em] text-[#5268b0]">KHÔNG GIAN LÀM VIỆC</p><h1 className="mt-1 text-2xl font-semibold tracking-tight text-[#101c43]">{config.title}</h1><p className="mt-1 text-sm text-[#8998ae]">{config.description}</p></div><div className="flex gap-2"><Button variant="outline" onClick={() => { void load(); setStageRevision(current => current + 1) }}><RefreshCw /> Làm mới</Button>{(config.editable || section === 'tasks' || section === 'calendar') && <Button onClick={() => config.editable ? openForm() : setShowForm(true)} className="bg-[#243d9a] text-white hover:bg-[#1c327f]"><Plus /> Thêm mới</Button>}</div></div>
    {!config.editable && section !== 'members' && <p className="mt-4 text-xs text-[#8493aa]">Hiện tại hỗ trợ xem và tạo mới; chỉnh sửa đang được phát triển.</p>}
    {error && <p role="alert" className="mt-6 rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700">{error}</p>}
    {showForm && section === 'opportunities' && <OpportunityForm key={editing?.id ?? 'new'} workspaceId={workspaceId} item={editing} onSaved={() => { setShowForm(false); void load() }} onCancel={() => setShowForm(false)} />}
    {showForm && config.editable && section !== 'opportunities' && section !== 'tasks' && section !== 'calendar' && <form onSubmit={submit} className="mt-6 rounded-2xl border border-[#e2e8f4] bg-white p-6"><h2 className="mb-5 text-lg font-semibold">{editing ? 'Chỉnh sửa' : 'Thêm mới'} {config.title.toLowerCase()}</h2><div className="grid gap-4 sm:grid-cols-2">{config.fields.map(field => <div key={field.key} className="space-y-2"><Label htmlFor={`field-${field.key}`}>{field.label}{field.required && ' *'}</Label>{field.options ? <select id={`field-${field.key}`} value={form[field.key] ?? ''} onChange={e => setForm({ ...form, [field.key]: e.target.value })} className="h-10 w-full rounded-lg border border-input bg-white px-3 text-sm">{field.options.map(option => <option key={option.value} value={option.value}>{option.label}</option>)}</select> : <Input id={`field-${field.key}`} type={field.type ?? 'text'} required={field.required} value={form[field.key] ?? ''} onChange={e => setForm({ ...form, [field.key]: e.target.value })} />}</div>)}</div><div className="mt-5 flex gap-2"><Button disabled={saving} className="bg-[#243d9a] text-white hover:bg-[#1c327f]">{saving ? 'Đang lưu...' : 'Lưu'}</Button><Button type="button" variant="outline" onClick={() => setShowForm(false)}>Hủy</Button></div></form>}
    {showForm && (section === 'tasks' || section === 'calendar') && <ActivityForm key={`${section}:${editing?.id ?? 'new'}`} workspaceId={workspaceId} kind={section} item={editing} onSaved={() => { setShowForm(false); void load() }} onCancel={() => setShowForm(false)} />}
    {section === 'pipelines' && !loading && <div className="mx-auto mt-5 max-w-5xl"><PipelineBoard workspaceId={workspaceId} pipelines={items} revision={stageRevision} /></div>}
    {section !== 'pipelines' ? <RecordWorkspace title={config.title} description={config.description} fields={config.fields} columns={config.columns} items={items} total={total} loading={loading} editable={config.editable} paginated={config.paginated} displayValue={displayValue} onEdit={openForm} onDelete={item => void remove(item)} /> :
    <div className="mt-5 overflow-x-auto rounded-2xl bg-white"><div className="border-b border-[#edf1f8] px-5 py-4"><h2 className="font-semibold text-[#1e2d58]">Quản lý quy trình</h2><p className="mt-1 text-xs text-[#8998ae]">Tạo và sắp xếp các giai đoạn bán hàng</p></div><Table className="min-w-[600px] text-left"><TableHeader className="bg-[#f8faff]"><TableRow>{config.columns.map(key => <TableHead key={key} className="px-5 py-4 text-xs text-[#8998ae]">{config.fields.find(f => f.key === key)?.label ?? key}</TableHead>)}<TableHead className="px-5 py-4 text-xs text-[#8998ae]">Thao tác</TableHead></TableRow></TableHeader><TableBody>{!loading && items.map(item => <TableRow key={item.id}>{config.columns.map(key => <TableCell key={key} className="px-5 py-4 text-[#52627c]">{['status', 'priority', 'eventType', 'role', 'companyType'].includes(key) && item[key] ? <Badge className="bg-[#eaf1ff] text-[#243d9a]">{displayValue(key, item[key])}</Badge> : displayValue(key, item[key])}</TableCell>)}<TableCell className="px-5 py-4"><Button variant="ghost" size="sm" onClick={() => openForm(item)}>Sửa</Button><Button variant="ghost" size="sm" onClick={() => void remove(item)} className="text-red-600">Xóa</Button><Button variant="ghost" size="sm" disabled={stageLoading || saving} onClick={() => void loadStages(item)}>Giai đoạn</Button></TableCell></TableRow>)}</TableBody></Table>{loading ? <p className="p-8 text-center text-sm text-slate-500">Đang tải...</p> : items.length === 0 && !error ? <p className="p-8 text-center text-sm text-slate-500">Chưa có dữ liệu.</p> : null}</div>}
    {config.paginated && <div className="mt-4 flex items-center justify-between text-sm text-slate-500"><span>{total} kết quả</span><div className="flex items-center gap-2"><Button variant="outline" size="sm" disabled={loading || page <= 1} onClick={() => setPage(page - 1)}>Trước</Button><span>Trang {page}</span><Button variant="outline" size="sm" disabled={loading || page * 50 >= total} onClick={() => setPage(page + 1)}>Sau</Button></div></div>}
    {stagesFor && <div className="mt-6 rounded-2xl border border-[#e2e8f4] bg-white p-6"><div className="flex justify-between"><h2 className="font-semibold">Giai đoạn · {stagesFor.name}</h2><Button variant="ghost" size="sm" onClick={() => setStagesFor(null)}>Đóng</Button></div>{stageLoading ? <p className="mt-4 text-sm text-slate-500">Đang tải...</p> : <ol className="mt-4 space-y-2">{stages.map((stage, index) => <li key={stage.id} className="flex flex-wrap items-center justify-between gap-2 rounded-lg bg-[#f3f6fc] px-4 py-3 text-sm"><span>{stage.name}</span><span className="flex gap-1"><Button variant="ghost" size="sm" disabled={saving || index === 0} onClick={() => void changeStage(stage, 'up')} aria-label={`Di chuyển ${stage.name} lên`}>↑</Button><Button variant="ghost" size="sm" disabled={saving || index === stages.length - 1} onClick={() => void changeStage(stage, 'down')} aria-label={`Di chuyển ${stage.name} xuống`}>↓</Button><Button variant="ghost" size="sm" disabled={saving} onClick={() => void changeStage(stage, 'rename')}>Đổi tên</Button><Button variant="ghost" size="sm" disabled={saving} className="text-red-600" onClick={() => void changeStage(stage, 'delete')}>Xóa</Button></span></li>)}</ol>}<form className="mt-4 flex gap-2" onSubmit={addStage}><Input aria-label="Tên giai đoạn" value={stageName} onChange={e => setStageName(e.target.value)} placeholder="Tên giai đoạn mới" required /><Button disabled={saving || stageLoading} className="bg-[#243d9a] text-white hover:bg-[#1c327f]">Thêm giai đoạn</Button></form></div>}
  </section>
}

export default RecordsPage
