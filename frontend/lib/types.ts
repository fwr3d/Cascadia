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

export interface AffectedCounty {
  name: string
  state: string
  pop: number
  fips: string
}

export interface WaveRing {
  radiusKm: number
  etaMinutes: number
  affectedCounties: AffectedCounty[]
}

export type InfraState = 'safe' | 'warning' | 'destroyed'

export interface InfraItem {
  name: string
  type: 'hospital' | 'power' | 'port'
  lat: number
  lon: number
  distanceKm: number
  hitAtRingIndex: number
  gridCoverageRadiusKm: number
}

export interface CoastalInundation {
  lat: number
  lon: number
  name: string
  distanceFromEpicenterKm: number
  inundationKm: number
  runupM: number
  hitAtRingIndex: number
  affectedPopulation: number
}

export interface SimulateResponse {
  waveSpeedKmS: number
  energyJoules: string
  etaNearestCoastMin: number
  estimatedRunupM: number
  affectedPopulation: number
  rings: WaveRing[]
  infrastructureAtRisk: InfraItem[]
  coastalInundation: CoastalInundation[]
}
