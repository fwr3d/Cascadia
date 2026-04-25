'use client'

import { useState, useRef, useEffect } from 'react'
import { ChevronDown } from 'lucide-react'
import type { Earthquake } from '@/lib/types'

const SCENARIOS: Earthquake[] = [
  { id: 'ak1964', place: '1964 Alaska Good Friday', magnitude: 9.2, lat: 60.91, lon: -147.34, depthKm: 25, time: -184106400000 },
  { id: 'jp2011', place: '2011 Tōhoku, Japan',      magnitude: 9.1, lat: 38.32, lon: 142.37, depthKm: 29, time: 1299824291000 },
  { id: 'cl2010', place: '2010 Chile Maule',         magnitude: 8.8, lat: -35.91, lon: -72.73, depthKm: 22, time: 1267062840000 },
  { id: 'ca1700', place: '1700 Cascadia Subduction', magnitude: 9.0, lat: 46.50, lon: -124.50, depthKm: 20, time: -8521747200000 },
]

interface Props {
  onSelect: (eq: Earthquake) => void
}

export default function QuickScenarios({ onSelect }: Props) {
  const [open, setOpen] = useState(false)
  const ref = useRef<HTMLDivElement>(null)

  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false)
    }
    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [])

  return (
    <div ref={ref} className="relative">
      <button
        onClick={() => setOpen((v) => !v)}
        className="flex items-center gap-2 rounded-lg border border-white/10 bg-[#0a1628]/90 px-3 py-2 font-mono text-xs text-slate-300 backdrop-blur-md transition-colors hover:border-white/20 hover:text-white"
      >
        Historic Scenarios
        <ChevronDown size={12} className={`transition-transform ${open ? 'rotate-180' : ''}`} />
      </button>

      <div
        className="absolute left-0 top-full mt-1 w-56 origin-top overflow-hidden rounded-lg border border-white/10 bg-[#0d1f3c]/95 backdrop-blur-md transition-all duration-150"
        style={{ transform: open ? 'scaleY(1)' : 'scaleY(0)', opacity: open ? 1 : 0 }}
      >
        {SCENARIOS.map((eq) => (
          <button
            key={eq.id}
            onClick={() => { onSelect(eq); setOpen(false) }}
            className="flex w-full flex-col gap-0.5 px-3 py-2.5 text-left transition-colors hover:bg-white/5"
          >
            <span className="font-mono text-xs text-white">{eq.place}</span>
            <span className="font-mono text-[10px] text-[#37C8DD]">M{eq.magnitude}</span>
          </button>
        ))}
      </div>
    </div>
  )
}
