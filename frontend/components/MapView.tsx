'use client'

import { useRef, useState } from 'react'
import Map, {
  Source,
  Layer,
  Marker,
  type MapMouseEvent,
  type MapRef,
} from 'react-map-gl/mapbox'
import 'mapbox-gl/dist/mapbox-gl.css'
import type { WaveRing, InfraItem } from '@/lib/types'
import { circleGeoJSON } from '@/lib/geo'
import InfraMarker from './InfraMarker'
import BlackoutZone from './BlackoutZone'

interface MapViewProps {
  epicenter: { lat: number; lon: number } | null
  waveRings: WaveRing[]
  infraItems: InfraItem[]
  onMapClick: (lat: number, lon: number) => void
  onMouseMove?: (coords: { lat: number; lon: number } | null) => void
}

const RING_COLORS = ['#ef4444', '#f97316', '#eab308', '#22c55e', '#3b82f6']
const TOKEN = process.env.NEXT_PUBLIC_MAPBOX_TOKEN

export default function MapView({
  epicenter,
  waveRings,
  infraItems,
  onMapClick,
  onMouseMove,
}: MapViewProps) {
  const mapRef = useRef<MapRef>(null)
  const [landMsg, setLandMsg] = useState<{ x: number; y: number } | null>(null)
  const landTimer = useRef<ReturnType<typeof setTimeout> | null>(null)

  function handleClick(e: MapMouseEvent) {
    const features = mapRef.current
      ?.getMap()
      .queryRenderedFeatures(e.point, { layers: ['water'] })

    if (!features || features.length === 0) {
      if (landTimer.current) clearTimeout(landTimer.current)
      setLandMsg({ x: e.point.x, y: e.point.y })
      landTimer.current = setTimeout(() => setLandMsg(null), 1500)
      return
    }

    onMapClick(e.lngLat.lat, e.lngLat.lng)
  }

  if (!TOKEN || TOKEN === 'pk.your_token_here') {
    return (
      <div className="flex h-full w-full flex-col items-center justify-center gap-3 bg-[#0a1628]">
        <p className="text-sm text-slate-400">Mapbox token not configured</p>
        <p className="text-xs text-slate-600">
          Add <code className="rounded bg-white/5 px-1 text-slate-400">NEXT_PUBLIC_MAPBOX_TOKEN</code>{' '}
          to <code className="rounded bg-white/5 px-1 text-slate-400">.env.local</code>
        </p>
      </div>
    )
  }

  return (
    <div
      className="relative h-full w-full"
      style={{ cursor: 'crosshair' }}
      onMouseLeave={() => onMouseMove?.(null)}
    >
      <Map
        ref={mapRef}
        mapboxAccessToken={TOKEN}
        initialViewState={{ longitude: -145, latitude: 55, zoom: 3 }}
        style={{ width: '100%', height: '100%' }}
        mapStyle="mapbox://styles/mapbox/dark-v11"
        onClick={handleClick}
        onMouseMove={(e: MapMouseEvent) =>
          onMouseMove?.({ lat: e.lngLat.lat, lon: e.lngLat.lng })
        }
      >
        {epicenter &&
          waveRings.map((ring, i) => {
            const color = RING_COLORS[i % RING_COLORS.length]
            return (
              <Source
                key={`ring-${i}`}
                id={`ring-${i}`}
                type="geojson"
                data={circleGeoJSON(epicenter.lat, epicenter.lon, ring.radiusKm)}
              >
                <Layer
                  id={`ring-fill-${i}`}
                  type="fill"
                  paint={{ 'fill-color': color, 'fill-opacity': 0.07 }}
                />
                <Layer
                  id={`ring-line-${i}`}
                  type="line"
                  paint={{ 'line-color': color, 'line-width': 1.5, 'line-opacity': 0.75 }}
                />
              </Source>
            )
          })}

        {infraItems
          .filter((item) => item.hitAtRingIndex <= waveRings.length - 1)
          .map((item) => (
            <BlackoutZone
              key={`bz-${item.name}`}
              center={[item.lon, item.lat]}
              radiusKm={item.gridCoverageRadiusKm}
              id={item.name}
            />
          ))}

        {infraItems.map((item) => {
          const hit = item.hitAtRingIndex < waveRings.length
          const state = hit ? 'destroyed' : waveRings.length > 0 ? 'warning' : 'safe'
          return (
            <Marker key={item.name} longitude={item.lon} latitude={item.lat} anchor="center">
              <InfraMarker item={item} state={state} />
            </Marker>
          )
        })}

        {epicenter && (
          <Marker longitude={epicenter.lon} latitude={epicenter.lat} anchor="center">
            <div className="relative flex items-center justify-center">
              <div className="animate-wave-pulse absolute h-5 w-5 rounded-full bg-[#E24B4A] opacity-50" />
              <div className="h-3 w-3 rounded-full border-2 border-white bg-[#E24B4A] shadow-lg" />
            </div>
          </Marker>
        )}
      </Map>

      {landMsg && (
        <div
          className="pointer-events-none absolute z-10 -translate-x-1/2 rounded border border-[#E24B4A]/40 bg-[#0a1628]/90 px-2.5 py-1 font-mono text-xs text-[#E24B4A] backdrop-blur-sm"
          style={{ left: landMsg.x, top: landMsg.y - 36 }}
        >
          ocean only
        </div>
      )}
    </div>
  )
}
