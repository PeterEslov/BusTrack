import { useEffect, useRef } from 'react'
import maplibregl, { Map, Marker } from 'maplibre-gl'
import * as signalR from '@microsoft/signalr'

// Centrerad över Skåne (Skånetrafiken är vald operatör för demot).
const INITIAL_CENTER: [number, number] = [13.55, 55.7]
const INITIAL_ZOOM = 9

interface VehiclePosition {
  vehicleId: string
  lat: number
  lon: number
}

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'https://localhost:5001'

// Steg 2: initial state hämtas en gång via REST (så kartan inte är tom
// medan SignalR-anslutningen upprättas), därefter tar SignalR-huben över
// och pushar nya positioner direkt när Collector sparat dem - ingen
// polling längre.
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

    function upsertVehicles(vehicles: VehiclePosition[]) {
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
    }

    async function loadInitialPositions() {
      try {
        const response = await fetch(`${API_BASE_URL}/api/vehicles`)
        const vehicles: VehiclePosition[] = await response.json()
        upsertVehicles(vehicles)
      } catch (error) {
        console.error('Kunde inte hämta initiala fordonspositioner', error)
      }
    }

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_BASE_URL}/hubs/vehicles`)
      .withAutomaticReconnect()
      .build()

    connection.on('vehiclePositionsUpdated', (vehicles: VehiclePosition[]) => {
      upsertVehicles(vehicles)
    })

    loadInitialPositions()
      .then(() => connection.start())
      .catch(error => console.error('Kunde inte ansluta till SignalR-huben', error))

    return () => {
      cancelled = true
      connection.stop()
    }
  }, [])

  return <div ref={mapContainerRef} style={{ height: '100%', width: '100%' }} />
}

export default MapView
