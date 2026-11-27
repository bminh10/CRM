import { useState } from 'react'
import { ClipboardList, Pencil, Search, Trash2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import type { RecordItem } from '@/types/crm'

interface Field { key: string; label: string }
interface Props {
  title: string
  description: string
  fields: Field[]
  columns: string[]
  items: RecordItem[]
  total: number
  loading: boolean
  editable?: boolean
  paginated?: boolean
  displayValue: (key: string, value: RecordItem[string]) => string
  onEdit: (item: RecordItem) => void
  onDelete: (item: RecordItem) => void
}

function itemKey(item: RecordItem, index: number) {
  return String(item.id ?? item.userId ?? index)
}

function initials(name: string) {
  return name.trim().split(/\s+/).slice(-2).map(part => part[0]).join('').toUpperCase() || '•'
}

function linkedValue(key: string, value: RecordItem[string], displayValue: Props['displayValue']) {
  const text = displayValue(key, value)
  const website = key === 'website' && typeof value === 'string' && value.trim()
    ? /^https?:\/\//i.test(value.trim()) ? value.trim() : `https://${value.trim()}`
    : ''
  if (website) {
    try {
      const url = new URL(website)
      if (url.protocol === 'http:' || url.protocol === 'https:')
        return <a href={url.href} target="_blank" rel="noreferrer" className="break-all text-[#3459b4] underline decoration-[#cad6f0] underline-offset-2 hover:text-[#243d9a]">{text}</a>
    } catch { /* Show invalid URLs as plain text. */ }
  }
  return text.split(/(https?:\/\/[^\s]+)/gi).map((part, index) => {
    try {
      const url = new URL(part)
      if (url.protocol === 'http:' || url.protocol === 'https:')
        return <a key={index} href={url.href} target="_blank" rel="noreferrer" className="break-all text-[#3459b4] underline decoration-[#cad6f0] underline-offset-2 hover:text-[#243d9a]">{part}</a>
    } catch { /* Keep malformed URL text unchanged. */ }
    return part
  })
}

export default function RecordWorkspace({ title, description, fields, columns, items, total, loading, editable, paginated, displayValue, onEdit, onDelete }: Props) {
  const [selectedKey, setSelectedKey] = useState('')
  const [query, setQuery] = useState('')
  const filtered = items.filter(item => columns.some(key => String(item[key] ?? '').toLocaleLowerCase('vi').includes(query.trim().toLocaleLowerCase('vi'))))
  const selected = filtered.find((item, index) => itemKey(item, index) === selectedKey) ?? filtered[0]
  const selectedName = selected ? displayValue(columns[0], selected[columns[0]]) : ''

  return <>
    <div className="mt-5 grid gap-4 md:grid-cols-[200px_minmax(0,1fr)] xl:grid-cols-[230px_minmax(0,1fr)]">
      <section className="rounded-2xl bg-white p-4 sm:p-5">
        <div className="flex items-center justify-between gap-2"><h2 className="text-sm font-semibold">{title}</h2><span className="text-xs text-[#9aa8bb]">{loading ? '—' : total}</span></div>
        <div className="relative mt-4"><Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-[#a2afc1]" /><Input aria-label={`Tìm ${title.toLowerCase()} trong trang`} placeholder="Tìm trong trang..." value={query} onChange={event => setQuery(event.target.value)} className="h-9 rounded-xl border-0 bg-[#f3f6fa] pl-9 text-xs shadow-none" /></div>
        {paginated && <p className="mt-2 text-[10px] text-[#9aa8bb]">Tìm trong trang hiện tại</p>}
        <div className="mt-4 max-h-[460px] space-y-1 overflow-y-auto">
          {loading ? <p className="p-3 text-xs text-[#8998ae]">Đang tải...</p> : filtered.length === 0 ? <p className="p-3 text-xs leading-5 text-[#8998ae]">{query ? 'Không có kết quả trong trang này.' : 'Chưa có dữ liệu.'}</p> : filtered.map((item, index) => {
            const name = displayValue(columns[0], item[columns[0]])
            const active = item === selected
            return <button type="button" key={itemKey(item, index)} onClick={() => setSelectedKey(itemKey(item, index))} aria-pressed={active} className={`flex w-full items-center gap-3 rounded-xl px-2.5 py-2.5 text-left transition-colors ${active ? 'bg-[#edf3ff] text-[#243d9a]' : 'text-[#42536e] hover:bg-[#f6f8fc]'}`}><span className={`grid size-9 shrink-0 place-items-center rounded-full text-xs font-semibold ${active ? 'bg-[#d5e2ff]' : 'bg-[#f0f3f9] text-[#7c8ca4]'}`}>{initials(name)}</span><span className="min-w-0 flex-1 truncate text-sm font-medium">{name}</span></button>
          })}
        </div>
      </section>
      <section className="min-h-[400px] rounded-2xl bg-white p-5 sm:p-7">
        {selected ? <>
          <div className="flex flex-wrap items-start gap-4"><span className="grid size-14 shrink-0 place-items-center rounded-full bg-[#dbe7ff] text-lg font-bold text-[#243d9a]">{initials(selectedName)}</span><div className="min-w-0 flex-1"><p className="text-[11px] font-semibold tracking-[.15em] text-[#8a9ab2]">CHI TIẾT {title.toUpperCase()}</p><h2 className="mt-1 break-words text-xl font-semibold text-[#101c43]">{selectedName}</h2><p className="mt-1 text-sm text-[#8998ae]">{columns[1] ? displayValue(columns[1], selected[columns[1]]) : description}</p></div>{editable && <div className="flex gap-1"><Button variant="outline" size="sm" onClick={() => onEdit(selected)}><Pencil /> Sửa</Button><Button variant="ghost" size="sm" onClick={() => onDelete(selected)} className="text-red-600"><Trash2 /> Xóa</Button></div>}</div>
          <div className="mt-6"><span className="rounded-full bg-[#243d9a] px-4 py-2 text-xs font-semibold text-white">Tổng quan</span></div>
          <div className="mt-7"><div className="flex items-center justify-between border-b border-[#edf1f7] pb-3"><h3 className="text-sm font-semibold">Thông tin {title.toLowerCase()}</h3><span className="text-xs text-[#a2afc1]">Dữ liệu workspace</span></div><div className="grid gap-x-8 sm:grid-cols-2">{fields.map(field => <div key={field.key} className="border-b border-[#f0f3f8] py-4"><p className="text-xs text-[#91a0b5]">{field.label}</p><p className="mt-1.5 break-words text-sm font-medium text-[#354760]">{linkedValue(field.key, selected[field.key], displayValue)}</p></div>)}</div></div>
        </> : <div className="grid min-h-[350px] place-items-center text-center"><div><span className="mx-auto grid size-14 place-items-center rounded-2xl bg-[#f0f5ff] text-[#8195be]"><ClipboardList className="size-6" /></span><h2 className="mt-4 font-semibold text-[#334766]">{loading ? 'Đang tải dữ liệu' : 'Chưa có thông tin để hiển thị'}</h2><p className="mt-2 text-sm text-[#98a7bb]">{query ? 'Thử từ khóa khác trong trang này.' : description}</p></div></div>}
      </section>
    </div>
    <div className="mt-4 grid grid-cols-2 gap-3"><div className="flex items-center gap-3 rounded-2xl bg-white p-4"><span className="grid size-10 place-items-center rounded-xl bg-[#eef4ff] text-[#5268b0]"><ClipboardList className="size-4" /></span><div><p className="text-xs text-[#8998ae]">Tổng {title.toLowerCase()}</p><p className="text-lg font-semibold">{loading ? '—' : total}</p></div></div><div className="flex items-center gap-3 rounded-2xl bg-white p-4"><span className="grid size-10 place-items-center rounded-xl bg-[#eef4ff] text-[#5268b0]"><Search className="size-4" /></span><div><p className="text-xs text-[#8998ae]">Đang hiển thị trong trang</p><p className="text-lg font-semibold">{loading ? '—' : filtered.length}</p></div></div></div>
  </>
}
