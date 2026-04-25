export interface Earthquake {
  id: string
  place: string
  magnitude: number
  lat: number
  lon: number
  depthKm: number
  time: number
}

export interface SimulateRequest {
  epicenterLat: number
  epicenterLon: number
  magnitude: number
  depthKm: number
}

export interface WaveRing {
  radiusKm: number
  etaMinutes: number
  waveHeightM: number
}

export interface County {
  name: string
  state: string
  population: number
  distanceKm: number
}

export type InfraState = 'safe' | 'warning' | 'destroyed'

export interface InfraItem {
  name: string
  type: 'hospital' | 'power' | 'port' | 'nuclear'
  lat: number
  lon: number
  distanceKm: number
  hitAtRingIndex: number
  gridCoverageRadiusKm: number
}

export interface SimulateResponse {
  magnitude: number
  epicenterLat: number
  epicenterLon: number
  depthKm: number
  energyJoules: number
  rings: WaveRing[]
  countiesAtRisk: County[]
  infrastructureAtRisk: InfraItem[]
  totalPopulationAtRisk: number
  maxWaveHeightM: number
}
