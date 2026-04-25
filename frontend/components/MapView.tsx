'use client'

import { useRef, useState, useEffect } from 'react'
import Map, {
  Source,
  Layer,
  Marker,
  type MapMouseEvent,
  type MapRef,
} from 'react-map-gl/mapbox'
import 'mapbox-gl/dist/mapbox-gl.css'
import type { WaveRing, CoastalInundation } from '@/lib/types'
import { circleGeoJSON } from '@/lib/geo'

interface MapViewProps {
  epicenter: { lat: number; lon: number } | null
  waveRings: WaveRing[]
  coastalInundation: CoastalInundation[]
  animatedRadiusKm: number
  maxRingRadiusKm: number
  onMapClick: (lat: number, lon: number) => void
  onMouseMove?: (coords: { lat: number; lon: number } | null) => void
}

const RING_COLORS = ['#ef4444', '#f97316', '#eab308', '#22c55e', '#3b82f6', '#8b5cf6', '#ec4899']
const TOKEN = process.env.NEXT_PUBLIC_MAPBOX_TOKEN
const SAMPLE_DISTANCES_KM = [0.5, 1, 2, 3, 5, 8, 12, 15]

// Bearing from a coast point toward the interior of North America
function inlandBearing(lat: number, lon: number): number {
  const tLat = 47 * Math.PI / 180
  const tLon = -113 * Math.PI / 180
  const φ1 = lat * Math.PI / 180
  const dλ = tLon - lon * Math.PI / 180
  const y = Math.sin(dλ) * Math.cos(tLat)
  const x = Math.cos(φ1) * Math.sin(tLat) - Math.sin(φ1) * Math.cos(tLat) * Math.cos(dλ)
  return ((Math.atan2(y, x) * 180 / Math.PI) + 360) % 360
}

// Point at distKm along bearingDeg from (lat, lon)
function destinationPoint(lat: number, lon: number, bearingDeg: number, distKm: number): [number, number] {
  const R = 6371
  const d = distKm / R
  const b = bearingDeg * Math.PI / 180
  const φ1 = lat * Math.PI / 180
  const λ1 = lon * Math.PI / 180
  const φ2 = Math.asin(Math.sin(φ1) * Math.cos(d) + Math.cos(φ1) * Math.sin(d) * Math.cos(b))
  const λ2 = λ1 + Math.atan2(Math.sin(b) * Math.sin(d) * Math.cos(φ1), Math.cos(d) - Math.sin(φ1) * Math.sin(φ2))
  return [λ2 * 180 / Math.PI, φ2 * 180 / Math.PI]
}

export default function MapView({
  epicenter,
  waveRings,
  coastalInundation,
  animatedRadiusKm,
  maxRingRadiusKm,
  onMapClick,
  onMouseMove,
}: MapViewProps) {
  const mapRef = useRef<MapRef>(null)
  const [landMsg, setLandMsg] = useState<{ x: number; y: number } | null>(null)
  const landTimer = useRef<ReturnType<typeof setTimeout> | null>(null)
  const [mapReady, setMapReady] = useState(false)
  // terrain-sampled inundation distances per zone index
  const [terrainInundation, setTerrainInundation] = useState<Record<number, number>>({})

  useEffect(() => {
    if (!epicenter || !mapRef.current) return
    mapRef.current.flyTo({
      center: [epicenter.lon, epicenter.lat],
      zoom: 4,
      duration: 1400,
      essential: true,
    })
  }, [epicenter])

  function handleMapLoad() {
    const mapInstance = mapRef.current?.getMap()
    if (!mapInstance) return
    const map = mapInstance
    // Enable 3-D terrain — required for queryTerrainElevation to return real values
    map.setTerrain({ source: 'mapbox-dem', exaggeration: 1.2 })
    setMapReady(true)
  }

  // Walk an inland elevation transect for each zone when simulation results arrive
  useEffect(() => {
    if (!coastalInundation.length || !mapReady) return
    const map = mapRef.current?.getMap()
    if (!map) return

    function sample() {
      const updated: Record<number, number> = {}

      for (let i = 0; i < coastalInundation.length; i++) {
        const zone = coastalInundation[i]
        const bearing = inlandBearing(zone.lat, zone.lon)
        // Physics-based value is the fallback when terrain tiles aren't loaded yet
        let reachKm = zone.inundationKm

        for (const d of SAMPLE_DISTANCES_KM) {
          const [lng, lat] = destinationPoint(zone.lat, zone.lon, bearing, d)
          const elev = map!.queryTerrainElevation([lng, lat], { exaggerated: false })
          if (elev == null) {
            // Tile not loaded — keep physics fallback for this zone
            break
          }
          if (elev >= zone.runupM) {
            // Terrain blocks the wave at this distance
            reachKm = d
            break
          }
          // Wave can travel at least this far inland
          reachKm = d
        }

        updated[i] = Math.max(0.1, Math.min(reachKm, 15))
      }

      setTerrainInundation(updated)
    }

    if (map!.areTilesLoaded()) {
      sample()
    } else {
      const onIdle = () => { sample(); map!.off('idle', onIdle) }
      map!.on('idle', onIdle)
      return () => { map!.off('idle', onIdle) }
    }
  }, [coastalInundation, mapReady])

  function handleClick(e: MapMouseEvent) {
    try {
      const map = mapRef.current?.getMap()
      if (!map) { onMapClick(e.lngLat.lat, e.lngLat.lng); return }

      const candidates = ['land', 'landuse', 'national-park', 'landuse-overlay']
      const existingLayers = candidates.filter(id => map.getLayer(id))

      if (existingLayers.length > 0) {
        const features = map.queryRenderedFeatures(e.point, { layers: existingLayers })
        if (features && features.length > 0) {
          if (landTimer.current) clearTimeout(landTimer.current)
          setLandMsg({ x: e.point.x, y: e.point.y })
          landTimer.current = setTimeout(() => setLandMsg(null), 1500)
          return
        }
      }
    } catch {
      // layer query failed — allow the click
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
        initialViewState={{ longitude: -155, latitude: 50, zoom: 3 }}
        style={{ width: '100%', height: '100%' }}
        mapStyle="mapbox://styles/mapbox/dark-v11"
        onClick={handleClick}
        onLoad={handleMapLoad}
        onMouseMove={(e: MapMouseEvent) =>
          onMouseMove?.({ lat: e.lngLat.lat, lon: e.lngLat.lng })
        }
      >
        {/* Terrain DEM — enables queryTerrainElevation + hillshading */}
        <Source
          id="mapbox-dem"
          type="raster-dem"
          url="mapbox://mapbox.mapbox-terrain-dem-v1"
          tileSize={512}
          maxzoom={14}
        >
          <Layer
            id="hillshading"
            type="hillshade"
            paint={{
              'hillshade-exaggeration': 0.4,
              'hillshade-shadow-color': '#000814',
              'hillshade-highlight-color': '#1e3a5f',
              'hillshade-illumination-direction': 315,
            }}
          />
        </Source>

        {/* Target rings — capped at maxRingRadiusKm, opacity fades inner→outer */}
        {epicenter && (() => {
          const nonZero = waveRings.filter(r => r.radiusKm > 0)
          const total = nonZero.length
          return waveRings.map((ring, i) => {
            if (ring.radiusKm === 0) return null
            // Cap radius so rings stop at the coast + buffer
            const cappedRadius = maxRingRadiusKm > 0
              ? Math.min(ring.radiusKm, maxRingRadiusKm)
              : ring.radiusKm
            const isPassed = cappedRadius <= animatedRadiusKm
            // Position among non-zero rings: 0 = innermost, 1 = outermost
            const pos = nonZero.findIndex(r => r === ring)
            const t = total > 1 ? pos / (total - 1) : 0
            // Inner rings bright, outer rings fade to 0.15 max
            const lineOpacity = isPassed
              ? 0.7 - 0.55 * t          // 0.70 → 0.15
              : 0.22 - 0.17 * t         // 0.22 → 0.05
            const fillOpacity = isPassed
              ? 0.07 - 0.055 * t        // 0.070 → 0.015
              : 0.02 - 0.015 * t        // 0.020 → 0.005
            const lineWidth = isPassed
              ? 1.8 - 1.0 * t           // 1.8 → 0.8
              : 0.8 - 0.4 * t           // 0.8 → 0.4
            const color = RING_COLORS[i % RING_COLORS.length]
            return (
              <Source
                key={`ring-${i}`}
                id={`ring-${i}`}
                type="geojson"
                data={circleGeoJSON(epicenter.lat, epicenter.lon, cappedRadius)}
              >
                <Layer
                  id={`ring-fill-${i}`}
                  type="fill"
                  paint={{ 'fill-color': color, 'fill-opacity': fillOpacity }}
                />
                <Layer
                  id={`ring-line-${i}`}
                  type="line"
                  paint={{ 'line-color': color, 'line-width': lineWidth, 'line-opacity': lineOpacity }}
                />
              </Source>
            )
          })
        })()}

        {/* Animated wave front — capped at maxRingRadiusKm */}
        {epicenter && animatedRadiusKm > 0 && (() => {
          const displayRadius = maxRingRadiusKm > 0
            ? Math.min(animatedRadiusKm, maxRingRadiusKm)
            : animatedRadiusKm
          return (
          <Source
            key="wave-front"
            id="wave-front"
            type="geojson"
            data={circleGeoJSON(epicenter.lat, epicenter.lon, displayRadius)}
          >
            <Layer
              id="wave-front-fill"
              type="fill"
              paint={{ 'fill-color': '#37C8DD', 'fill-opacity': 0.04 }}
            />
            <Layer
              id="wave-front-glow"
              type="line"
              paint={{ 'line-color': '#37C8DD', 'line-width': 18, 'line-opacity': 0.12, 'line-blur': 12 }}
            />
            <Layer
              id="wave-front-line"
              type="line"
              paint={{ 'line-color': '#37C8DD', 'line-width': 2.5, 'line-opacity': 1 }}
            />
          </Source>
          )
        })()}

        {/* Coastal inundation — radius comes from real terrain elevation sampling */}
        {epicenter &&
          coastalInundation.map((zone, i) => {
            const isHit = waveRings[zone.hitAtRingIndex]?.radiusKm <= animatedRadiusKm
            if (!isHit) return null
            const radius = terrainInundation[i] ?? zone.inundationKm
            const opacity = Math.min(0.6, 0.15 + zone.runupM / 35)
            return (
              <Source
                key={`inundation-${i}`}
                id={`inundation-${i}`}
                type="geojson"
                data={circleGeoJSON(zone.lat, zone.lon, radius)}
              >
                <Layer
                  id={`inundation-fill-${i}`}
                  type="fill"
                  paint={{ 'fill-color': '#37C8DD', 'fill-opacity': opacity }}
                />
                <Layer
                  id={`inundation-line-${i}`}
                  type="line"
                  paint={{ 'line-color': '#37C8DD', 'line-width': 1, 'line-opacity': 0.7 }}
                />
              </Source>
            )
          })}

        {/* Epicenter marker */}
        {epicenter && (
          <Marker longitude={epicenter.lon} latitude={epicenter.lat} anchor="center">
            <div className="relative flex items-center justify-center" style={{ width: 32, height: 32 }}>
              <div className="animate-wave-pulse absolute h-8 w-8 rounded-full bg-[#E24B4A] opacity-60" />
              <div className="absolute h-4 w-4 rounded-full border-2 border-white bg-[#E24B4A] shadow-[0_0_12px_rgba(226,75,74,0.8)]" />
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
