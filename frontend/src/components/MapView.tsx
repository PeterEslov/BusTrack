import { useEffect, useRef } from 'react'
import maplibregl, { Map, Marker } from 'maplibre-gl'
import * as signalR from '@microsoft/signalr'

// Centrerad över Skåne (Skånetrafiken är vald operatör för demot).
const INITIAL_CENTER: [number, number] = [13.55, 55.7]
const INITIAL_ZOOM = 9

// Om ett fordon inte fått en ny position pushad på så här lång tid antar vi
// att det gått ur trafik (Collector pollar var 15:e sekund, så 60s motsvarar
// ~4 missade pollningar) och tar bort markören från kartan.
const STALE_AFTER_MS = 60_000
const CLEANUP_INTERVAL_MS = 15_000

// GTFS-realtime-specen anger att "speed" är meter/sekund. Om det visar sig
// att Skånetrafikens feed avviker från specen (värdena ser orimliga ut i
// jämförelse med verkliga bussar), ta bort *3.6 nedan så visas rätalvärdet.
const METERS_PER_SECOND_TO_KMH = 3.6

// Fordon vars hastighet är under den här gränsen (km/h) räknas som stillastående
// och tonas ner visuellt, t.ex. vid en hållplats eller i en depå.
const IDLE_SPEED_KMH = 1

const COLOR_MOVING = '#0f766e'
const COLOR_IDLE = '#94a3b8'

interface VehiclePosition {
  vehicleId: string
  lat: number
  lon: number
  bearing?: number | null
  speed?: number | null
  timestamp: string
}

interface VehicleMarkerEntry {
  marker: Marker
  circleEl: SVGCircleElement
}

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'https://localhost:5001'

function createMarkerElement(): { element: HTMLDivElement; circleEl: SVGCircleElement } {
  const element = document.createElement('div')
  element.innerHTML = `
    <svg width="22" height="22" viewBox="0 0 22 22" xmlns="http://www.w3.org/2000/svg">
      <circle cx="11" cy="11" r="9" stroke="white" stroke-width="2"></circle>
      <path d="M11 4 L15 13 L11 10.5 L7 13 Z" fill="white"></path>
    </svg>
  `
  const circleEl = element.querySelector('circle') as SVGCircleElement
  return { element, circleEl }
}

function formatPopupHtml(vehicle: VehiclePosition): string {
  const speedKmh = vehicle.speed != null ? Math.round(vehicle.speed * METERS_PER_SECOND_TO_KMH) : null
  const updated = new Date(vehicle.timestamp).toLocaleTimeString('sv-SE')

  return `
    <div style="font-family: system-ui, sans-serif; font-size: 13px; line-height: 1.5;">
      <strong>Fordon ${vehicle.vehicleId}</strong><br/>
      ${speedKmh != null ? `Hastighet: ~${speedKmh} km/h<br/>` : ''}
      ${vehicle.bearing != null ? `Riktning: ${Math.round(vehicle.bearing)}°<br/>` : ''}
      Uppdaterad: ${updated}
    </div>
  `
}

// Steg 2: initial state hämtas en gång via REST (så kartan inte är tom
// medan SignalR-anslutningen upprättas), därefter tar SignalR-huben över
// och pushar nya positioner direkt när Collector sparat dem - ingen
// polling längre.
function MapView() {
  const mapContainerRef = useRef<HTMLDivElement | null>(null)
  const mapRef = useRef<Map | null>(null)
  const markersRef = useRef<globalThis.Map<string, VehicleMarkerEntry>>(new globalThis.Map())
  const lastSeenRef = useRef<globalThis.Map<string, number>>(new globalThis.Map())

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
        lastSeenRef.current.set(vehicle.vehicleId, Date.now())

        const speedKmh = vehicle.speed != null ? vehicle.speed * METERS_PER_SECOND_TO_KMH : null
        const color = speedKmh != null && speedKmh < IDLE_SPEED_KMH ? COLOR_IDLE : COLOR_MOVING

        const existing = markersRef.current.get(vehicle.vehicleId)
        if (existing) {
          existing.marker.setLngLat([vehicle.lon, vehicle.lat])
          if (vehicle.bearing != null) {
            existing.marker.setRotation(vehicle.bearing)
          }
          existing.circleEl.setAttribute('fill', color)
          existing.marker.getPopup()?.setHTML(formatPopupHtml(vehicle))
        } else {
          const { element, circleEl } = createMarkerElement()
          circleEl.setAttribute('fill', color)

          const popup = new maplibregl.Popup({ offset: 14 }).setHTML(formatPopupHtml(vehicle))
          const marker = new maplibregl.Marker({ element, rotationAlignment: 'map' })
            .setLngLat([vehicle.lon, vehicle.lat])
            .setPopup(popup)
            .addTo(mapRef.current)

          if (vehicle.bearing != null) {
            marker.setRotation(vehicle.bearing)
          }

          markersRef.current.set(vehicle.vehicleId, { marker, circleEl })
        }
      }
    }

    function removeStaleVehicles() {
      const now = Date.now()
      for (const [vehicleId, lastSeen] of lastSeenRef.current) {
        if (now - lastSeen > STALE_AFTER_MS) {
          markersRef.current.get(vehicleId)?.marker.remove()
          markersRef.current.delete(vehicleId)
          lastSeenRef.current.delete(vehicleId)
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

    const cleanupInterval = setInterval(removeStaleVehicles, CLEANUP_INTERVAL_MS)

    return () => {
      cancelled = true
      clearInterval(cleanupInterval)
      connection.stop()
    }
  }, [])

  return <div ref={mapContainerRef} style={{ height: '100%', width: '100%' }} />
}

export default MapView
