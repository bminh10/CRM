import { Check, ChevronDown, ChevronUp } from 'lucide-react'
import { Select as SelectPrimitive } from 'radix-ui'
import { cn } from 'cn'

interface SelectOption {
  value: string
  label: string
  disabled?: boolean
}

interface SelectProps {
  id?: string
  'aria-label'?: string
  value: string
  options: SelectOption[]
  onValueChange: (value: string) => void
  className?: string
  disabled?: boolean
  required?: boolean
}

const encode = (value: string) => `option:${value}`

function Select({ id, 'aria-label': ariaLabel, value, options, onValueChange, className, disabled, required }: SelectProps) {
  const placeholder = options.find(option => option.value === '')?.label ?? 'Chọn một mục'
  // Required selections are validated by the owning form, with a visible error message.
  return <SelectPrimitive.Root
    value={required && !value ? '' : encode(value)}
    onValueChange={next => onValueChange(next.slice('option:'.length))}
    disabled={disabled}
  >
    <SelectPrimitive.Trigger id={id} aria-label={ariaLabel} aria-required={required} className={cn(
      'flex h-10 w-full min-w-0 items-center justify-between gap-2 rounded-lg border border-[#dce4f2] bg-white px-3 text-left text-sm text-[#253452] shadow-sm outline-none transition-colors hover:border-[#b8c8ed] data-[state=open]:border-[#8fa6dc] data-[state=open]:ring-3 data-[state=open]:ring-[#e6edff] focus-visible:border-[#8fa6dc] focus-visible:ring-3 focus-visible:ring-[#e6edff] disabled:cursor-not-allowed disabled:opacity-50 [&>span]:truncate',
      className,
    )}>
      <SelectPrimitive.Value placeholder={placeholder} />
      <SelectPrimitive.Icon><ChevronDown className="size-4 shrink-0 text-[#5268b0]" /></SelectPrimitive.Icon>
    </SelectPrimitive.Trigger>
    <SelectPrimitive.Portal>
      <SelectPrimitive.Content position="popper" sideOffset={6} align="start" className="z-50 max-h-[min(20rem,var(--radix-select-content-available-height))] min-w-[var(--radix-select-trigger-width)] max-w-[var(--radix-select-content-available-width)] overflow-hidden rounded-xl border border-[#dfe7f5] bg-white text-[#253452] shadow-[0_16px_40px_-12px_rgba(16,28,67,0.28)]">
        <SelectPrimitive.ScrollUpButton className="flex items-center justify-center py-1 text-[#8a9ab2]"><ChevronUp className="size-4" /></SelectPrimitive.ScrollUpButton>
        <SelectPrimitive.Viewport className="p-1.5">
          {options.map(option => <SelectPrimitive.Item key={option.value} value={encode(option.value)} disabled={option.disabled} className="relative flex min-h-9 cursor-default select-none items-center rounded-lg py-2 pl-3 pr-9 text-sm outline-none data-[highlighted]:bg-[#eef3ff] data-[highlighted]:text-[#243d9a] data-[state=checked]:font-semibold data-[state=checked]:text-[#243d9a] data-[disabled]:pointer-events-none data-[disabled]:opacity-50 [&>span]:min-w-0 [&>span]:break-words">
            <SelectPrimitive.ItemText>{option.label}</SelectPrimitive.ItemText>
            <SelectPrimitive.ItemIndicator className="absolute right-3"><Check className="size-4" /></SelectPrimitive.ItemIndicator>
          </SelectPrimitive.Item>)}
        </SelectPrimitive.Viewport>
        <SelectPrimitive.ScrollDownButton className="flex items-center justify-center py-1 text-[#8a9ab2]"><ChevronDown className="size-4" /></SelectPrimitive.ScrollDownButton>
      </SelectPrimitive.Content>
    </SelectPrimitive.Portal>
  </SelectPrimitive.Root>
}

export { Select }
