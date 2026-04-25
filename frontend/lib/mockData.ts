import type { SimulateResponse } from './types'

export const MOCK_RESPONSE: SimulateResponse = {
  magnitude: 9.2,
  epicenterLat: 60.91,
  epicenterLon: -147.34,
  depthKm: 25,
  energyJoules: 1.99e18,
  rings: [
    { radiusKm: 150,  etaMinutes: 22,  waveHeightM: 18.4 },
    { radiusKm: 450,  etaMinutes: 67,  waveHeightM: 8.2  },
    { radiusKm: 1000, etaMinutes: 149, waveHeightM: 4.1  },
    { radiusKm: 2000, etaMinutes: 298, waveHeightM: 1.8  },
    { radiusKm: 3500, etaMinutes: 521, waveHeightM: 0.6  },
  ],
  countiesAtRisk: [
    { name: 'Valdez-Cordova', state: 'AK', population: 9202,  distanceKm: 142 },
    { name: 'Kodiak Island',  state: 'AK', population: 13592, distanceKm: 390 },
    { name: 'Kenai Peninsula', state: 'AK', population: 55400, distanceKm: 210 },
  ],
  infrastructureAtRisk: [
    { name: 'Valdez Marine Terminal', type: 'port',     lat: 61.12, lon: -146.35, distanceKm: 98,  hitAtRingIndex: 0, gridCoverageRadiusKm: 12 },
    { name: 'Providence Valdez MC',   type: 'hospital', lat: 61.13, lon: -146.35, distanceKm: 100, hitAtRingIndex: 0, gridCoverageRadiusKm: 8  },
    { name: 'Kodiak Station',         type: 'port',     lat: 57.79, lon: -152.40, distanceKm: 390, hitAtRingIndex: 1, gridCoverageRadiusKm: 10 },
    { name: 'Providence Kodiak MC',   type: 'hospital', lat: 57.80, lon: -152.39, distanceKm: 392, hitAtRingIndex: 1, gridCoverageRadiusKm: 8  },
    { name: 'Soldotna Regional',      type: 'hospital', lat: 60.48, lon: -151.05, distanceKm: 215, hitAtRingIndex: 1, gridCoverageRadiusKm: 8  },
    { name: 'Bradley Lake Plant',     type: 'power',    lat: 59.73, lon: -150.75, distanceKm: 280, hitAtRingIndex: 1, gridCoverageRadiusKm: 6  },
    { name: 'Port of Anchorage',      type: 'port',     lat: 61.24, lon: -149.89, distanceKm: 140, hitAtRingIndex: 0, gridCoverageRadiusKm: 14 },
    { name: 'Alaska Regional Hospital', type: 'hospital', lat: 61.19, lon: -149.83, distanceKm: 145, hitAtRingIndex: 0, gridCoverageRadiusKm: 8 },
  ],
  totalPopulationAtRisk: 78194,
  maxWaveHeightM: 18.4,
}
