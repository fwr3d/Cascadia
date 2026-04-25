'use client'

import { X } from 'lucide-react'
import type { SimulateResponse } from '@/lib/types'

interface Props {
  response: SimulateResponse | null
  onClear: () => void
}

function Stat({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex flex-col gap-1">
      <span className="font-mono text-[9px] tracking-widest text-slate-500">{label}</span>
      <span className="font-mono text-sm font-bold text-white">{value}</span>
    </div>
  )
}

function Divider() {
  return <div className="w-px self-stretch bg-white/[0.06]" />
}

export default function ImpactPanel({ response, onClear }: Props) {
  if (!response) return null

  const energy = response.energyJoules.toExponential(2)
  const pop = response.totalPopulationAtRisk.toLocaleString()
  const wave = `${response.maxWaveHeightM.toFixed(1)} m`
  const rings = response.rings.length

  return (
    <div className="flex h-full flex-col border-t border-white/[0.08] bg-[#0a1628]/95 backdrop-blur-md">
      <div className="flex items-center justify-between border-b border-white/[0.08] px-4 py-2">
        <span className="font-mono text-[10px] tracking-widest text-slate-500">IMPACT ASSESSMENT</span>
        <button onClick={onClear} className="text-slate-500 hover:text-white">
          <X size={12} />
        </button>
      </div>

      <div className="flex flex-1 items-center gap-6 overflow-x-auto px-6">
        <Stat label="MAGNITUDE" value={`M${response.magnitude.toFixed(1)}`} />
        <Divider />
        <Stat label="MAX WAVE HEIGHT" value={wave} />
        <Divider />
        <Stat label="POPULATION AT RISK" value={pop} />
        <Divider />
        <Stat label="ENERGY RELEASED" value={`${energy} J`} />
        <Divider />
        <Stat label="WAVE RINGS" value={`${rings}`} />
        <Divider />
        <div className="flex flex-col gap-1">
          <span className="font-mono text-[9px] tracking-widest text-slate-500">ETA RANGE</span>
          <span className="font-mono text-sm font-bold text-white">
            {response.rings[0].etaMinutes} – {response.rings[rings - 1].etaMinutes} min
          </span>
        </div>
      </div>
    </div>
  )
}
