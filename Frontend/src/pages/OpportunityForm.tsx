import { useEffect, useState, type FormEvent } from 'react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { listPipelines, listStages, saveRecord } from '@/services/crm'
import type { Option, RecordItem } from '@/types/crm'


function OpportunityForm({ workspaceId, item, onSaved, onCancel }: {
  workspaceId: string; item: RecordItem | null; onSaved: () => void; onCancel: () => void
}) {
  const [pipelines, setPipelines] = useState<Option[]>([])
  const [stages, setStages] = useState<Option[]>([])
  const [pipelineId, setPipelineId] = useState(String(item?.pipelineId ?? ''))
  const [stageId, setStageId] = useState(String(item?.stageId ?? ''))
  const [title, setTitle] = useState(String(item?.title ?? ''))
  const [value, setValue] = useState(String(item?.expectedValue ?? 0))
  const [probability, setProbability] = useState(String(item?.probability ?? 0))
  const [error, setError] = useState('')
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    let active = true
    listPipelines(workspaceId)
      .then(data => { if (active) setPipelines(data) })
      .catch(e => { if (active) setError(e instanceof Error ? e.message : 'Không thể tải quy trình.') })
    return () => { active = false }
  }, [workspaceId])
  useEffect(() => {
    if (!pipelineId) return
    let active = true
    listStages<Option>(pipelineId)
      .then(data => { if (active) setStages(data) })
      .catch(e => { if (active) setError(e instanceof Error ? e.message : 'Không thể tải giai đoạn.') })
    return () => { active = false }
  }, [pipelineId])

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (saving) return
    if (!title.trim() || !pipelineId || !stageId) { setError('Vui lòng chọn quy trình, giai đoạn và nhập tiêu đề.'); return }
    setSaving(true); setError('')
    const body = { workspaceId, pipelineId, stageId, title: title.trim(), expectedValue: Number(value), probability: Number(probability), companyId: item?.companyId ?? null, contactId: item?.contactId ?? null, description: item?.description ?? null, expectedCloseDate: item?.expectedCloseDate ?? null }
    try { await saveRecord('opportunities', workspaceId, item?.id, body); onSaved() }
    catch (e) { setError(e instanceof Error ? e.message : 'Không thể lưu cơ hội.') }
    finally { setSaving(false) }
  }
  const selectClass = 'h-10 w-full rounded-lg border border-input bg-white px-3 text-sm'
  return <form onSubmit={submit} className="mt-6 rounded-2xl border border-[#e2e8f4] bg-white p-6"><h2 className="mb-5 text-lg font-semibold">{item ? 'Chỉnh sửa' : 'Thêm'} cơ hội</h2><div className="grid gap-4 sm:grid-cols-2">
    <div className="space-y-2"><Label htmlFor="op-title">Tên cơ hội *</Label><Input id="op-title" required value={title} onChange={e => setTitle(e.target.value)} /></div>
    <div className="space-y-2"><Label htmlFor="op-pipeline">Quy trình *</Label><select id="op-pipeline" required value={pipelineId} onChange={e => { setPipelineId(e.target.value); setStageId(''); setStages([]) }} className={selectClass}><option value="">Chọn quy trình</option>{pipelines.map(p => <option key={p.id} value={p.id}>{p.name}</option>)}</select></div>
    <div className="space-y-2"><Label htmlFor="op-stage">Giai đoạn *</Label><select id="op-stage" required value={stageId} onChange={e => setStageId(e.target.value)} className={selectClass}><option value="">Chọn giai đoạn</option>{stages.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}</select></div>
    <div className="space-y-2"><Label htmlFor="op-value">Giá trị dự kiến *</Label><Input id="op-value" type="number" required min="0" step="any" value={value} onChange={e => setValue(e.target.value)} /></div>
    <div className="space-y-2"><Label htmlFor="op-probability">Xác suất (%) *</Label><Input id="op-probability" type="number" required min="0" max="100" step="1" value={probability} onChange={e => setProbability(e.target.value)} /></div>
  </div>{pipelines.length === 0 && <p className="mt-4 text-sm text-slate-500">Tạo quy trình và giai đoạn trước khi thêm cơ hội.</p>}{error && <p role="alert" className="mt-4 text-sm text-red-700">{error}</p>}<div className="mt-5 flex gap-2"><Button disabled={saving} className="bg-[#243d9a] text-white hover:bg-[#1c327f]">{saving ? 'Đang lưu...' : 'Lưu'}</Button><Button type="button" variant="outline" onClick={onCancel}>Hủy</Button></div></form>
}

export default OpportunityForm
