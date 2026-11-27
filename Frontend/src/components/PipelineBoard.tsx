import { Fragment, useEffect, useState } from 'react'
import { ArrowRight, Check, CircleDollarSign, GitBranch, ListChecks, MoveRight, Target } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent } from '@/components/ui/card'
import { listPipelineOpportunities, listStages, moveOpportunity } from '@/services/crm'
import type { Option, PipelineOpportunity, RecordItem } from '@/types/crm'

interface PipelineBoardProps {
  workspaceId: string
  pipelines: RecordItem[]
  revision: number
  editable: boolean
}

const stageStyles = [
  { dot: 'bg-[#4164c5]', selected: 'border-[#b9c9f1] bg-[#f1f5ff] ring-2 ring-[#e6edff]', badge: 'bg-[#e5edff] text-[#3858ad]' },
  { dot: 'bg-[#6d68d8]', selected: 'border-[#c9c6f4] bg-[#f5f4ff] ring-2 ring-[#ecebff]', badge: 'bg-[#eeedff] text-[#625dc0]' },
  { dot: 'bg-[#d68a3a]', selected: 'border-[#f0d7b8] bg-[#fff8ef] ring-2 ring-[#fff0dc]', badge: 'bg-[#fff0dc] text-[#a76722]' },
  { dot: 'bg-[#338a72]', selected: 'border-[#b8dfd3] bg-[#eff9f5] ring-2 ring-[#e2f4ed]', badge: 'bg-[#e2f4ed] text-[#28745e]' },
]

const currency = new Intl.NumberFormat('vi-VN')

function PipelineBoard({ workspaceId, pipelines, revision, editable }: PipelineBoardProps) {
  const [selectedId, setSelectedId] = useState('')
  const [selectedStageId, setSelectedStageId] = useState('')
  const [stages, setStages] = useState<Option[]>([])
  const [opportunities, setOpportunities] = useState<PipelineOpportunity[]>([])
  const [loading, setLoading] = useState(false)
  const [moving, setMoving] = useState('')
  const [error, setError] = useState('')
  const pipelineId = pipelines.some(pipeline => pipeline.id === selectedId) ? selectedId : pipelines[0]?.id ?? ''
  const activeStageId = stages.some(stage => stage.id === selectedStageId) ? selectedStageId : stages[0]?.id
  const activeStage = stages.find(stage => stage.id === activeStageId)
  const activeOpportunities = opportunities.filter(item => item.stageId === activeStageId)
  const activeTotal = activeOpportunities.reduce((sum, item) => sum + item.expectedValue, 0)

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

  if (!pipelineId) return <Card className="border-dashed border-[#cbd7ed] bg-white py-14 text-center shadow-none ring-0"><CardContent><GitBranch className="mx-auto size-9 text-[#8193bb]" /><h2 className="mt-4 font-semibold text-[#1e2d58]">Bắt đầu quy trình bán hàng</h2><p className="mt-2 text-sm text-[#718096]">Tạo quy trình và các giai đoạn để xây dựng lộ trình của bạn.</p></CardContent></Card>

  return <section aria-label="Lộ trình bán hàng" className="overflow-hidden rounded-3xl border border-[#e2e8f4] bg-white shadow-[0_12px_40px_-32px_#243d9a]">
    <header className="flex flex-wrap items-center justify-between gap-4 border-b border-[#edf1f8] px-5 py-5 sm:px-7">
      <div className="flex items-start gap-3">
        <span className="grid size-10 shrink-0 place-items-center rounded-xl bg-[#15264f] text-white"><GitBranch className="size-[18px]" /></span>
        <div><p className="text-[11px] font-bold tracking-[.15em] text-[#718096]">BẢN ĐỒ CƠ HỘI</p><h2 className="mt-1 text-lg font-semibold text-[#17264a]">Lộ trình bán hàng</h2></div>
      </div>
      <div className="flex items-center gap-3">
        <Badge variant="outline" className="hidden rounded-full border-[#e5eaf2] px-3 py-1.5 text-[#718096] sm:inline-flex">{stages.length} giai đoạn</Badge>
        <label className="sr-only" htmlFor="pipeline-route">Chọn quy trình</label>
        <select id="pipeline-route" value={pipelineId} onChange={event => { setSelectedId(event.target.value); setSelectedStageId('') }} className="max-w-48 rounded-lg border border-[#dce4f2] bg-white px-3 py-2 text-sm font-medium text-[#1e2d58] focus-visible:outline-2 focus-visible:outline-[#5268b0]">
          {pipelines.map(pipeline => <option key={pipeline.id} value={pipeline.id}>{String(pipeline.name ?? '')}</option>)}
        </select>
      </div>
    </header>

    {error && <p role="alert" className="mx-5 mt-4 rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700 sm:mx-7">{error}</p>}

    {loading ? <p className="p-8 text-sm text-[#718096]">Đang tải lộ trình...</p> : stages.length === 0 ?
      !error && <p className="p-8 text-sm text-[#718096]">Chưa có giai đoạn. Chọn “Giai đoạn” trong danh sách quy trình để thêm.</p> :
      <>
        <div className="border-b border-[#edf1f8] bg-[#fbfcff] px-5 py-6 sm:px-7">
          <div className="mb-4 flex items-center justify-between gap-3">
            <div><p className="text-[11px] font-bold tracking-[.14em] text-[#8794aa]">DÒNG CHẢY BÁN HÀNG</p><p className="mt-1 text-xs text-[#9aa6b8]">Chọn một nút để xem và cập nhật cơ hội</p></div>
            <span className="hidden items-center gap-1.5 text-xs text-[#8a97aa] md:flex"><MoveRight className="size-4" /> Tiến triển qua từng giai đoạn</span>
          </div>
          <div className="-mx-5 overflow-x-auto px-5 pb-2 sm:-mx-7 sm:px-7">
            <ol className="flex min-w-max items-center">
              {stages.map((stage, index) => {
                const stageOpportunities = opportunities.filter(item => item.stageId === stage.id)
                const stageTotal = stageOpportunities.reduce((sum, item) => sum + item.expectedValue, 0)
                const selected = stage.id === activeStageId
                const style = stageStyles[index % stageStyles.length]
                return <Fragment key={stage.id}>
                  <li>
                    <button type="button" aria-pressed={selected} onClick={() => setSelectedStageId(stage.id)} className={`w-[220px] rounded-2xl border px-4 py-4 text-left transition-all hover:-translate-y-0.5 hover:shadow-md ${selected ? style.selected : 'border-[#e8edf5] bg-white hover:border-[#cbd7f2]'}`}>
                      <span className="flex items-center justify-between gap-2">
                        <span className="flex min-w-0 items-center gap-2.5"><span className={`size-2.5 shrink-0 rounded-full ${style.dot}`} /><span className="truncate text-sm font-semibold text-[#253452]">{stage.name}</span></span>
                        <Badge variant="secondary" className={`shrink-0 rounded-full px-2 py-0.5 text-[10px] ${style.badge}`}>{stageOpportunities.length}</Badge>
                      </span>
                      <span className="mt-4 flex items-end justify-between border-t border-[#edf1f8] pt-3">
                        <span><span className="block text-[10px] font-medium uppercase tracking-wide text-[#98a3b4]">Giá trị cơ hội</span><span className="mt-1 block text-sm font-semibold text-[#263a67]">{currency.format(stageTotal)} đ</span></span>
                        <span className={`grid size-7 place-items-center rounded-lg ${selected ? style.badge : 'bg-[#f5f7fb] text-[#9aa6b8]'}`}>{selected ? <Check className="size-3.5" /> : <span className="text-xs">{String(index + 1).padStart(2, '0')}</span>}</span>
                      </span>
                    </button>
                  </li>
                  {index < stages.length - 1 && <li aria-hidden="true" className="flex w-12 shrink-0 items-center justify-center text-[#bbc6d8]"><span className="h-px w-5 bg-[#d8e0ee]" /><ArrowRight className="-ml-1 size-4" /></li>}
                </Fragment>
              })}
            </ol>
          </div>
        </div>

        <div className="px-5 py-6 sm:px-7 sm:py-7">
          <div className="flex flex-wrap items-end justify-between gap-4">
            <div>
              <p className="text-[11px] font-bold tracking-[.14em] text-[#8997ae]">GIAI ĐOẠN ĐANG CHỌN</p>
              <h3 className="mt-1.5 text-xl font-semibold tracking-tight text-[#17264a]">{activeStage?.name}</h3>
            </div>
            <div className="flex items-center gap-5">
              <span className="text-right"><span className="block text-[10px] uppercase tracking-wide text-[#9aa6b8]">Cơ hội</span><span className="mt-1 block text-sm font-semibold text-[#253452]">{activeOpportunities.length}</span></span>
              <span className="h-8 w-px bg-[#e8edf5]" />
              <span className="text-right"><span className="flex items-center justify-end gap-1 text-[10px] uppercase tracking-wide text-[#9aa6b8]"><CircleDollarSign className="size-3" /> Tổng giá trị</span><span className="mt-1 block text-sm font-semibold text-[#253452]">{currency.format(activeTotal)} đ</span></span>
            </div>
          </div>
          {activeOpportunities.length ? <div className="mt-5 grid gap-3 lg:grid-cols-2">
            {activeOpportunities.map(item => <Card key={item.id} className="rounded-xl border-[#e8edf5] bg-white py-0 ring-0 transition-colors hover:border-[#cbd7f2]">
              <CardContent className="p-4">
                <div className="flex items-start gap-3">
                  <span className="mt-0.5 grid size-9 shrink-0 place-items-center rounded-xl bg-[#f1f5ff] text-[#5268b0]"><Target className="size-4" /></span>
                  <div className="min-w-0 flex-1">
                    <p className="truncate text-sm font-semibold text-[#253452]">{item.title}</p>
                    <div className="mt-2 flex flex-wrap items-center gap-2">
                      <Badge variant="outline" className="rounded-full border-[#e6ebf3] text-[10px] text-[#748198]">{item.probability}% xác suất</Badge>
                      {item.status && <Badge variant="secondary" className="rounded-full bg-[#f4f6fa] text-[10px] text-[#748198]">{item.status}</Badge>}
                    </div>
                  </div>
                </div>
                <div className="mt-4 flex items-center justify-between gap-3 border-t border-[#edf1f8] pt-3">
                  <span className="flex items-center gap-1.5 text-sm font-semibold text-[#243d9a]"><CircleDollarSign className="size-4" /> {currency.format(item.expectedValue)} đ</span>
                  <label className="flex items-center gap-1.5 text-xs text-[#8290a6]"><span className="sr-only">Chuyển {item.title} sang giai đoạn</span><MoveRight className="size-3.5" />
                        <select aria-label={`Chuyển ${item.title} sang giai đoạn`} value={item.stageId} disabled={!editable || !!moving} onChange={event => void move(item.id, event.target.value)} className="max-w-36 rounded-md border border-[#e1e7f0] bg-white px-2 py-1.5 text-xs text-[#52627c] disabled:opacity-50">
                      {stages.map(destination => <option key={destination.id} value={destination.id}>{destination.name}</option>)}
                    </select>
                  </label>
                </div>
              </CardContent>
            </Card>)}
          </div> : <div className="mt-5 flex items-center gap-4 rounded-2xl border border-dashed border-[#dce4f0] bg-[#fbfcff] px-5 py-6">
            <span className="grid size-10 shrink-0 place-items-center rounded-xl bg-white text-[#8493ad] shadow-sm"><ListChecks className="size-4" /></span>
            <div><p className="text-sm font-medium text-[#52627c]">Chưa có cơ hội trong giai đoạn này</p><p className="mt-1 text-xs text-[#8a97aa]">Khi có cơ hội, bạn có thể xem và chuyển giai đoạn ngay tại đây.</p></div>
          </div>}
        </div>
      </>}
  </section>
}

export default PipelineBoard
