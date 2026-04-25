'use client'

import dynamic from 'next/dynamic'
import { useState, useCallback } from 'react'
import QuickScenarios from '@/components/QuickScenarios'
import LiveTicker from '@/components/LiveTicker'
import WaveControls from '@/components/WaveControls'
import ImpactPanel from '@/components/ImpactPanel'
import type { Earthquake, SimulateResponse } from '@/lib/types'
import { simulate } from '@/lib/api'

const MapView = dynamic(() => import('@/components/MapView'), { ssr: false })

function formatCoord(coords: { lat: number; lon: number } | null): string {
  if (!coords) return '—°  —°'
  const lat = `${Math.abs(coords.lat).toFixed(4)}° ${coords.lat >= 0 ? 'N' : 'S'}`
  const lon = `${Math.abs(coords.lon).toFixed(4)}° ${coords.lon >= 0 ? 'E' : 'W'}`
  return `${lat}  ${lon}`
}

export default function SimulatorPage() {
  const [epicenter, setEpicenter] = useState<{ lat: number; lon: number } | null>(null)
  const [simResponse, setSimResponse] = useState<SimulateResponse | null>(null)
  const [isLoading, setIsLoading] = useState(false)
  const [cursor, setCursor] = useState<{ lat: number; lon: number } | null>(null)

  const handleMouseMove = useCallback(
    (coords: { lat: number; lon: number } | null) => setCursor(coords),
    [],
  )

  function handleMapClick(lat: number, lon: number) {
    setEpicenter({ lat, lon })
    setSimResponse(null)
  }

  function handleScenarioSelect(eq: Earthquake) {
    setEpicenter({ lat: eq.lat, lon: eq.lon })
    setSimResponse(null)
  }

  async function handleSimulate(magnitude: number, depthKm: number) {
    if (!epicenter) return
    setIsLoading(true)
    try {
      const result = await simulate({
        epicenterLat: epicenter.lat,
        epicenterLon: epicenter.lon,
        magnitude,
        depthKm,
      })
      setSimResponse(result)
    } finally {
      setIsLoading(false)
    }
  }

  function handleClear() {
    setSimResponse(null)
    setEpicenter(null)
  }

  return (
    <div className="relative h-screen w-screen overflow-hidden bg-[#0a1628] text-white">

      {/* Top bar */}
      <header className="absolute inset-x-0 top-0 z-20 flex h-12 items-center justify-between border-b border-white/[0.08] bg-[#0a1628]/90 px-5 backdrop-blur-md">
        <span className="font-mono text-sm font-bold tracking-[0.2em] text-white">CASCADIA</span>
        <div className="flex items-center gap-2">
          <span className="relative flex h-2 w-2">
            <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-emerald-400 opacity-75" />
            <span className="relative inline-flex h-2 w-2 rounded-full bg-emerald-500" />
          </span>
          <span className="text-xs text-slate-400">Live USGS data connected</span>
        </div>
        <span className="min-w-[200px] text-right font-mono text-xs tabular-nums text-slate-500">
          {formatCoord(cursor)}
        </span>
      </header>

      {/* Full-bleed map */}
      <div className="absolute inset-0">
        <MapView
          epicenter={epicenter}
          waveRings={simResponse?.rings ?? []}
          infraItems={simResponse?.infrastructureAtRisk ?? []}
          onMapClick={handleMapClick}
          onMouseMove={handleMouseMove}
        />
      </div>

      {/* Quick Scenarios */}
      <div className="absolute left-4 top-16 z-10">
        <QuickScenarios onSelect={handleScenarioSelect} />
      </div>

      {/* Wave controls — slides in from right */}
      <div
        className="absolute bottom-10 right-0 top-12 z-10 w-80 transition-transform duration-200 ease-out"
        style={{ transform: epicenter ? 'translateX(0)' : 'translateX(100%)' }}
      >
        <WaveControls
          epicenter={epicenter}
          onSimulate={handleSimulate}
          onCancel={handleClear}
          isLoading={isLoading}
        />
      </div>

      {/* Impact panel — slides up from bottom */}
      <div
        className="absolute left-0 z-10 h-[180px]"
        style={{
          bottom: '40px',
          right: epicenter ? '320px' : '0',
          transform: simResponse ? 'translateY(0)' : 'translateY(100%)',
          transition: 'transform 200ms ease-out, right 200ms ease-out',
        }}
      >
        <ImpactPanel response={simResponse} onClear={handleClear} />
      </div>

      {/* Live ticker */}
      <div className="absolute inset-x-0 bottom-0 z-20 h-10">
        <LiveTicker onSelect={handleScenarioSelect} />
      </div>
    </div>
  )
}
