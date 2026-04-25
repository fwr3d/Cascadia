'use client'

import { useEffect, useState } from 'react'
import type { Earthquake } from '@/lib/types'
import { fetchUSGSLive } from '@/lib/api'

interface Props {
  onSelect: (eq: Earthquake) => void
}

function MagBadge({ mag }: { mag: number }) {
  const color = mag >= 7 ? '#ef4444' : mag >= 6 ? '#f97316' : mag >= 5 ? '#eab308' : '#94a3b8'
  return (
    <span className="font-mono text-[10px] font-bold" style={{ color }}>
      M{mag.toFixed(1)}
    </span>
  )
}

export default function LiveTicker({ onSelect }: Props) {
  const [quakes, setQuakes] = useState<Earthquake[]>([])

  useEffect(() => {
    fetchUSGSLive().then(setQuakes)
    const id = setInterval(() => fetchUSGSLive().then(setQuakes), 60_000)
    return () => clearInterval(id)
  }, [])

  if (quakes.length === 0) {
    return (
      <div className="flex h-full items-center border-t border-white/[0.06] bg-[#0a1628]/95 px-4">
        <span className="font-mono text-[10px] text-slate-600">Loading USGS feed…</span>
      </div>
    )
  }

  const chips = [...quakes, ...quakes]

  return (
    <div className="flex h-full items-center overflow-hidden border-t border-white/[0.06] bg-[#0a1628]/95">
      <div className="ticker-track flex items-center gap-6 whitespace-nowrap px-4">
        {chips.map((eq, i) => (
          <button
            key={`${eq.id}-${i}`}
            onClick={() => onSelect(eq)}
            className="flex items-center gap-2 rounded px-2 py-0.5 transition-colors hover:bg-white/5"
          >
            <MagBadge mag={eq.magnitude} />
            <span className="font-mono text-[10px] text-slate-400">{eq.place}</span>
          </button>
        ))}
      </div>
    </div>
  )
}
