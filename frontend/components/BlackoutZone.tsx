'use client'

import { Source, Layer } from 'react-map-gl/mapbox'
import { circleGeoJSON } from '@/lib/geo'

interface Props {
  center: [number, number]
  radiusKm: number
  id: string
}

export default function BlackoutZone({ center, radiusKm, id }: Props) {
  const safeId = id.replace(/\s+/g, '-').toLowerCase()
  return (
    <Source
      id={`bz-${safeId}`}
      type="geojson"
      data={circleGeoJSON(center[1], center[0], radiusKm)}
    >
      <Layer
        id={`bz-fill-${safeId}`}
        type="fill"
        paint={{ 'fill-color': '#000000', 'fill-opacity': 0.45 }}
      />
    </Source>
  )
}
