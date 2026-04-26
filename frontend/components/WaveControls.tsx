'use client'

import { X, Zap } from 'lucide-react'

interface Props {
  epicenter: { lat: number; lon: number } | null
  magnitude: number
  depth: number
  onMagnitudeChange: (v: number) => void
  onDepthChange: (v: number) => void
  onSimulate: (magnitude: number, depthKm: number) => void
  onCancel: () => void
  isLoading: boolean
  errorMessage?: string | null
}

export default function WaveControls({ epicenter, magnitude, depth, onMagnitudeChange, onDepthChange, onSimulate, onCancel, isLoading, errorMessage }: Props) {

  if (!epicenter) return null

  const lat = `${Math.abs(epicenter.lat).toFixed(3)}° ${epicenter.lat >= 0 ? 'N' : 'S'}`
  const lon = `${Math.abs(epicenter.lon).toFixed(3)}° ${epicenter.lon >= 0 ? 'E' : 'W'}`

  return (
    <div className="flex h-full flex-col border-l border-white/[0.08] bg-[#0a1628]/95 backdrop-blur-md">
      <div className="flex items-center justify-between border-b border-white/[0.08] px-4 py-3">
        <span className="font-mono text-xs font-bold tracking-widest text-slate-400">PARAMETERS</span>
        <button onClick={onCancel} className="text-slate-500 hover:text-white">
          <X size={14} />
        </button>
      </div>

      <div className="flex flex-1 flex-col gap-6 overflow-y-auto p-4">
        {errorMessage ? (
          <div className="rounded-lg border border-red-400/25 bg-red-500/10 px-3 py-2 text-xs text-red-200">
            {errorMessage}
          </div>
        ) : null}

        <div>
          <p className="mb-0.5 font-mono text-[10px] tracking-widest text-slate-500">EPICENTER</p>
          <p className="font-mono text-xs text-[#37C8DD]">{lat}</p>
          <p className="font-mono text-xs text-[#37C8DD]">{lon}</p>
        </div>

        <div>
          <div className="mb-2 flex items-center justify-between">
            <label className="font-mono text-[10px] tracking-widest text-slate-500">MAGNITUDE</label>
            <span className="font-mono text-sm font-bold text-white">M{magnitude.toFixed(1)}</span>
          </div>
          <input
            type="range" min={5} max={9.5} step={0.1} value={magnitude}
            onChange={(e) => onMagnitudeChange(Number(e.target.value))}
            className="w-full accent-[#37C8DD]"
          />
          <div className="mt-1 flex justify-between font-mono text-[9px] text-slate-600">
            <span>5.0</span><span>9.5</span>
          </div>
        </div>

        <div>
          <div className="mb-2 flex items-center justify-between">
            <label className="font-mono text-[10px] tracking-widest text-slate-500">DEPTH</label>
            <span className="font-mono text-sm font-bold text-white">{depth} km</span>
          </div>
          <input
            type="range" min={5} max={100} step={5} value={depth}
            onChange={(e) => onDepthChange(Number(e.target.value))}
            className="w-full accent-[#37C8DD]"
          />
          <div className="mt-1 flex justify-between font-mono text-[9px] text-slate-600">
            <span>5 km</span><span>100 km</span>
          </div>
        </div>
      </div>

      <div className="border-t border-white/[0.08] p-4">
        <button
          onClick={() => onSimulate(magnitude, depth)}
          disabled={isLoading}
          className="flex w-full items-center justify-center gap-2 rounded-lg bg-[#37C8DD] py-3 font-mono text-sm font-bold text-[#0a1628] transition-all hover:bg-[#55d4e6] disabled:opacity-50 active:scale-[0.98]"
        >
          {isLoading ? (
            <span className="animate-pulse">Simulating…</span>
          ) : (
            <><Zap size={14} /> Simulate</>
          )}
        </button>
      </div>
    </div>
  )
}
