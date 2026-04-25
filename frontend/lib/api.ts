import type { Earthquake, SimulateRequest, SimulateResponse } from './types'
import { MOCK_RESPONSE } from './mockData'

const USE_MOCK = false
const API_BASE = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5055'

export async function simulate(req: SimulateRequest): Promise<SimulateResponse> {
  if (USE_MOCK) {
    await new Promise((r) => setTimeout(r, 800))
    return MOCK_RESPONSE
  }
  const res = await fetch(`${API_BASE}/api/simulate`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(req),
  })
  if (!res.ok) throw new Error(`Simulate failed: ${res.status}`)
  return res.json()
}

export async function fetchUSGSLive(): Promise<Earthquake[]> {
  const res = await fetch(
    'https://earthquake.usgs.gov/earthquakes/feed/v1.0/summary/4.5_day.geojson',
    { cache: 'no-store' },
  )
  if (!res.ok) return []
  const data = await res.json()
  return (data.features as any[]).map((f) => ({
    id: f.id,
    place: f.properties.place,
    magnitude: f.properties.mag,
    lat: f.geometry.coordinates[1],
    lon: f.geometry.coordinates[0],
    depthKm: f.geometry.coordinates[2],
    time: f.properties.time,
  }))
}
