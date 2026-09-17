import { useEffect, useRef } from 'react'
import maplibregl, { Map, Marker } from 'maplibre-gl'

// Centrerad över Skåne (Skånetrafiken är vald operatör för demot).
const INITIAL_CENTER: [number, number] = [13.55, 55.7]
const INITIAL_ZOOM = 9
const POLL_INTERVAL_MS = 15000

interface VehiclePosition {
  vehicleId: string
  lat: number
  lon: number
}

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'https://localhost:5001'

// MVP (Steg 1): hämtar senaste fordonspositioner via REST och pollar var 15:e
// sekund. Steg 2 byter ut pollingen mot en SignalR-prenumeration istället.
function MapView() {
  const mapContainerRef = useRef<HTMLDivElement | null>(null)
  const mapRef = useRef<Map | null>(null)
  const markersRef = useRef<globalThis.Map<string, Marker>>(new globalThis.Map())

  useEffect(() => {
    if (!mapContainerRef.current) return

    mapRef.current = new maplibregl.Map({
      container: mapContainerRef.current,
      style: 'https://demotiles.maplibre.org/style.json',
      center: INITIAL_CENTER,
      zoom: INITIAL_ZOOM
    })

    return () => mapRef.current?.remove()
  }, [])

  useEffect(() => {
    let cancelled = false

    async function pollVehicles() {
      try {
        const response = await fetch(`${API_BASE_URL}/api/vehicles`)
        const vehicles: VehiclePosition[] = await response.json()
        if (cancelled || !mapRef.current) return

        for (const vehicle of vehicles) {
          const existing = markersRef.current.get(vehicle.vehicleId)
          if (existing) {
            existing.setLngLat([vehicle.lon, vehicle.lat])
          } else {
            const marker = new maplibregl.Marker()
              .setLngLat([vehicle.lon, vehicle.lat])
              .addTo(mapRef.current)
            markersRef.current.set(vehicle.vehicleId, marker)
          }
        }
      } catch (error) {
        console.error('Kunde inte hämta fordonspositioner', error)
      }
    }

    pollVehicles()
    const interval = setInterval(pollVehicles, POLL_INTERVAL_MS)

    return () => {
      cancelled = true
      clearInterval(interval)
    }
  }, [])

  return <div ref={mapContainerRef} style={{ height: '100%', width: '100%' }} />
}

export default MapView
