'use client'

import { X, Share2 } from 'lucide-react'
import { useMemo, useState } from 'react'
import type { SimulateResponse } from '@/lib/types'

interface Props {
  response: SimulateResponse | null
  animatedRadiusKm: number
  onClear: () => void
}

function Stat({ label, value, highlight }: { label: string; value: string; highlight?: boolean }) {
  return (
    <div className="flex flex-col gap-1">
      <span className="font-mono text-[9px] tracking-widest text-slate-500">{label}</span>
      <span className={`font-mono text-sm font-bold ${highlight ? 'text-[#37C8DD]' : 'text-white'}`}>
        {value}
      </span>
    </div>
  )
}

function Divider() {
  return <div className="w-px self-stretch bg-white/[0.06]" />
}

export default function ImpactPanel({ response, animatedRadiusKm, onClear }: Props) {
  const [copied, setCopied] = useState(false)

  const simTimeStr = useMemo(() => {
    if (!response || animatedRadiusKm === 0) return 'T+00:00'
    const simSeconds = animatedRadiusKm / response.waveSpeedKmS
    const mins = Math.floor(simSeconds / 60)
    const secs = Math.floor(simSeconds % 60)
    return `T+${String(mins).padStart(2, '0')}:${String(secs).padStart(2, '0')}`
  }, [animatedRadiusKm, response])

  const livePop = useMemo(() => {
    if (!response || animatedRadiusKm === 0) return 0
    const passed = response.rings.filter(r => r.radiusKm > 0 && r.radiusKm <= animatedRadiusKm)
    return passed.at(-1)?.affectedCounties.reduce((s, c) => s + c.pop, 0) ?? 0
  }, [animatedRadiusKm, response])

  const liveCounties = useMemo(() => {
    if (!response || animatedRadiusKm === 0) return 0
    const passed = response.rings.filter(r => r.radiusKm > 0 && r.radiusKm <= animatedRadiusKm)
    return passed.at(-1)?.affectedCounties.length ?? 0
  }, [animatedRadiusKm, response])

  if (!response) return null

  function handleShare() {
    navigator.clipboard.writeText(window.location.href).then(() => {
      setCopied(true)
      setTimeout(() => setCopied(false), 2000)
    })
  }

  const totalCounties = response.rings.at(-1)?.affectedCounties.length ?? 0
  const runup = `${response.estimatedRunupM.toFixed(1)} m`
  const rings = response.rings.filter(r => r.radiusKm > 0).length
  const isAnimating = animatedRadiusKm > 0 && animatedRadiusKm < (response.rings.at(-1)?.radiusKm ?? 0)

  return (
    <div className="flex h-full flex-col border-t border-white/[0.08] bg-[#0a1628]/95 backdrop-blur-md">
      <div className="flex items-center justify-between border-b border-white/[0.08] px-4 py-2">
        <div className="flex items-center gap-3">
          <span className="font-mono text-[10px] tracking-widest text-slate-500">IMPACT ASSESSMENT</span>
          {isAnimating && (
            <span className="flex items-center gap-1.5">
              <span className="h-1.5 w-1.5 animate-pulse rounded-full bg-[#E24B4A]" />
              <span className="font-mono text-[9px] tracking-widest text-[#E24B4A]">LIVE</span>
            </span>
          )}
        </div>
        <div className="flex items-center gap-4">
          <span className="font-mono text-sm font-bold tabular-nums text-[#37C8DD]">{simTimeStr}</span>
          <button
            onClick={handleShare}
            className="flex items-center gap-1 font-mono text-[9px] tracking-widest text-slate-400 hover:text-white transition-colors"
            title="Copy share link"
          >
            <Share2 size={11} />
            {copied ? 'COPIED' : 'SHARE'}
          </button>
          <button onClick={onClear} className="text-slate-500 hover:text-white">
            <X size={12} />
          </button>
        </div>
      </div>

      <div className="flex flex-1 items-center gap-6 overflow-x-auto px-6">
        <div className="flex flex-col gap-1">
          <span className="font-mono text-[9px] tracking-widest text-slate-500">POPULATION AT RISK</span>
          <span className="font-mono text-sm font-bold tabular-nums text-[#E24B4A]">
            {livePop > 0 ? livePop.toLocaleString() : '—'}
          </span>
        </div>
        <Divider />
        <div className="flex flex-col gap-1">
          <span className="font-mono text-[9px] tracking-widest text-slate-500">COUNTIES HIT</span>
          <span className="font-mono text-sm font-bold tabular-nums text-[#E24B4A]">
            {liveCounties > 0 ? `${liveCounties} / ${totalCounties}` : `— / ${totalCounties}`}
          </span>
        </div>
        <Divider />
        <Stat label="NEAR-FIELD RUNUP" value={runup} />
        <Divider />
        <Stat label="ENERGY RELEASED" value={`${response.energyJoules} J`} />
        <Divider />
        <Stat label="WAVE SPEED" value={`${response.waveSpeedKmS.toFixed(2)} km/s`} />
        <Divider />
        <Stat label="WAVE FRONTS" value={`${rings}`} />
      </div>
    </div>
  )
}
