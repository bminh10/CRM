import { useEffect, useState } from 'react'
import { ArrowRight, GitBranch, ListChecks, Target } from 'lucide-react'
import { Card, CardContent } from '@/components/ui/card'
import { listPipelineOpportunities, listStages, moveOpportunity } from '@/services/crm'
import type { Option, PipelineOpportunity, RecordItem } from '@/types/crm'

interface PipelineBoardProps {
  workspaceId: string
  pipelines: RecordItem[]
  revision: number
}

export default function PipelineBoard({ workspaceId, pipelines, revision }: PipelineBoardProps) {
  const [selectedId, setSelectedId] = useState('')
  const [selectedStageId, setSelectedStageId] = useState('')
  const [stages, setStages] = useState<Option[]>([])
  const [opportunities, setOpportunities] = useState<PipelineOpportunity[]>([])
  const [loading, setLoading] = useState(false)
  const [moving, setMoving] = useState('')
  const [error, setError] = useState('')
  const pipelineId = pipelines.some(pipeline => pipeline.id === selectedId) ? selectedId : pipelines[0]?.id ?? ''
  const activeStageId = stages.some(stage => stage.id === selectedStageId) ? selectedStageId : stages[0]?.id

  useEffect(() => {
    if (!pipelineId) return
    let active = true
    setLoading(true)
    setError('')
    Promise.all([listStages<Option>(pipelineId), listPipelineOpportunities(workspaceId)])
      .then(([nextStages, nextOpportunities]) => {
        if (active) {
          setStages(nextStages)
          setOpportunities(nextOpportunities.filter(item => item.pipelineId === pipelineId))
        }
      })
      .catch(e => {
        if (active) {
          setStages([])
          setOpportunities([])
          setError(e instanceof Error ? e.message : 'Không thể tải lộ trình.')
        }
      })
      .finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [pipelineId, workspaceId, revision])

  async function move(id: string, stageId: string) {
    const item = opportunities.find(opportunity => opportunity.id === id)
    if (!item || item.stageId === stageId || moving) return
    setMoving(id)
    setError('')
    try {
      const updated = await moveOpportunity(id, stageId)
      setOpportunities(current => current.map(opportunity => opportunity.id === id ? updated : opportunity))
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Không thể chuyển giai đoạn.')
    } finally {
      setMoving('')
    }
  }

  if (!pipelineId) return <Card className="border border-dashed border-[#cbd7ed] bg-white py-14 text-center shadow-none ring-0"><CardContent><GitBranch className="mx-auto size-9 text-[#8193bb]" /><h2 className="mt-4 font-semibold text-[#1e2d58]">Bắt đầu quy trình bán hàng</h2><p className="mt-2 text-sm text-[#718096]">Tạo quy trình và các giai đoạn để xây dựng lộ trình của bạn.</p></CardContent></Card>

  return <section aria-label="Lộ trình bán hàng" className="rounded-3xl border border-[#e2e8f4] bg-white p-3 shadow-[0_12px_40px_-32px_#243d9a] sm:p-5">
    <div className="flex flex-wrap items-center justify-between gap-3 rounded-2xl bg-[#f2f5fb] p-2">
      <div className="flex items-center gap-2 rounded-xl bg-white px-5 py-3 text-sm font-semibold text-[#243d9a] shadow-sm"><ListChecks className="size-4" /> Lộ trình bán hàng</div>
      <div className="flex items-center gap-3 px-2 text-xs text-[#718096]"><span className="hidden sm:inline">{stages.length} giai đoạn</span><label className="sr-only" htmlFor="pipeline-route">Chọn quy trình</label><select id="pipeline-route" value={pipelineId} onChange={e => { setSelectedId(e.target.value); setSelectedStageId('') }} className="max-w-48 rounded-lg border border-[#dce4f2] bg-white px-3 py-2 text-sm font-medium text-[#1e2d58]">{pipelines.map(pipeline => <option key={pipeline.id} value={pipeline.id}>{String(pipeline.name ?? '')}</option>)}</select></div>
    </div>
    {error && <p role="alert" className="mt-4 rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700">{error}</p>}
    {loading ? <p className="p-8 text-sm text-[#718096]">Đang tải lộ trình...</p> : stages.length === 0 ? !error && <p className="p-8 text-sm text-[#718096]">Chưa có giai đoạn. Chọn “Giai đoạn” trong danh sách quy trình để thêm.</p> :
      <ol className="relative mx-2 my-6 space-y-1 before:absolute before:bottom-8 before:left-6 before:top-8 before:w-px before:bg-[#d9e3f2] sm:mx-5">
        {stages.map((stage, index) => {
          const cards = opportunities.filter(item => item.stageId === stage.id)
          const total = cards.reduce((sum, item) => sum + item.expectedValue, 0)
          const active = stage.id === activeStageId
          return <li key={stage.id} className={`relative rounded-2xl border transition-colors ${active ? 'border-[#c9d8f8] bg-[#f3f7ff]' : 'border-transparent'}`}>
            <button type="button" aria-expanded={active} onClick={() => setSelectedStageId(stage.id)} className="flex w-full items-center gap-4 p-3 text-left sm:p-4">
              <span className={`relative z-10 grid size-12 shrink-0 place-items-center rounded-full border-2 text-base font-semibold ${active ? 'border-[#d2dffc] bg-[#243d9a] text-white ring-4 ring-[#e2ebff]' : 'border-[#dce5f2] bg-white text-[#8b9bb4]'}`}>{index + 1}</span>
              <span className="min-w-0 flex-1"><span className="block text-[11px] font-bold tracking-[.15em] text-[#5268b0]">GIAI ĐOẠN {index + 1}</span><span className="mt-1 block text-base font-semibold text-[#1e2d58]">{stage.name}</span><span className="mt-1 block text-xs text-[#8290a6]">{cards.length ? 'Chọn để xem các cơ hội trong giai đoạn' : 'Chưa có cơ hội trong giai đoạn này'}</span></span>
              <span className="shrink-0 text-right"><span className="block text-lg font-bold text-[#243d9a] sm:text-xl">{cards.length} cơ hội</span><span className="mt-1 block text-xs text-[#8998ae]">{new Intl.NumberFormat('vi-VN').format(total)} đ</span></span>
            </button>
            {active && cards.length > 0 && <div className="space-y-2 pb-4 pl-[4.75rem] pr-4 sm:pl-20">{cards.map(item => <div key={item.id} className="flex flex-wrap items-center justify-between gap-2 rounded-xl border border-[#e1e9f7] bg-white px-4 py-3"><div className="min-w-0"><p className="truncate text-sm font-semibold text-[#1e2d58]">{item.title}</p><p className="mt-1 flex items-center gap-1 text-xs text-[#8a99b1]"><Target className="size-3" /> {item.probability}% · {new Intl.NumberFormat('vi-VN').format(item.expectedValue)} đ</p></div><label className="flex items-center gap-2 text-xs text-[#718096]"><ArrowRight className="size-3.5" /><span className="sr-only">Chuyển {item.title} sang giai đoạn</span><select value={item.stageId} disabled={!!moving} onChange={e => void move(item.id, e.target.value)} className="max-w-40 rounded-lg border border-[#dce4f2] bg-white px-2 py-1.5 text-[#52627c]">{stages.map(destination => <option key={destination.id} value={destination.id}>{destination.name}</option>)}</select></label></div>)}</div>}
          </li>
        })}
      </ol>}
  </section>
}
