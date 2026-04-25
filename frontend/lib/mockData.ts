import type { SimulateResponse } from './types'

export const MOCK_RESPONSE: SimulateResponse = {
  waveSpeedKmS: 0.224,
  energyJoules: '1.99e+24',
  etaNearestCoastMin: 0,
  estimatedRunupM: 18.4,
  affectedPopulation: 291247,
  rings: [
    { radiusKm: 0,    etaMinutes: 0,  affectedCounties: [] },
    { radiusKm: 67,   etaMinutes: 5,  affectedCounties: [] },
    { radiusKm: 134,  etaMinutes: 10, affectedCounties: [{ name: 'Anchorage Municipality', state: 'AK', pop: 291247, fips: '02020' }] },
    { radiusKm: 269,  etaMinutes: 20, affectedCounties: [] },
    { radiusKm: 403,  etaMinutes: 30, affectedCounties: [] },
    { radiusKm: 605,  etaMinutes: 45, affectedCounties: [] },
    { radiusKm: 806,  etaMinutes: 60, affectedCounties: [] },
  ],
  infrastructureAtRisk: [],
}
