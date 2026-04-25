'use client'

import type { InfraItem, InfraState } from '@/lib/types'

const ICONS: Record<InfraItem['type'], string> = {
  hospital: 'H',
  power: 'P',
  port: 'O',
}

const STATE_COLORS: Record<InfraState, string> = {
  safe: '#22c55e',
  warning: '#eab308',
  destroyed: '#ef4444',
}

interface Props {
  item: InfraItem
  state: InfraState
}

export default function InfraMarker({ item, state }: Props) {
  const color = STATE_COLORS[state]

  return (
    <div
      title={item.name}
      className="flex h-7 w-7 items-center justify-center rounded-full border-2 text-[10px] font-bold shadow-lg"
      style={{ borderColor: color, backgroundColor: `${color}22`, color }}
    >
      {ICONS[item.type]}
    </div>
  )
}
