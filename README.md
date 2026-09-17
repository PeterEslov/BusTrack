# BusTrack

Realtidsövervakning av kollektivtrafik i Skåne (Skånetrafiken), byggt som portföljprojekt i C#/.NET, Entity Framework Core, SQL, React och Azure.

Full specifikation (arkitektur, datamodell, API-kontrakt och byggplan) finns i projektdokumentet "BusTrack — Teknisk specifikation & arkitektur".

## Struktur

- `src/BusTrack.Domain` — entiteter, inga beroenden till andra lager
- `src/BusTrack.Infrastructure` — EF Core, `BusTrackDbContext`, GTFS-realtime-klient
- `src/BusTrack.Collector` — .NET Worker Service som pollar Skånetrafikens GTFS-RT-flöde och skriver till databasen
- `src/BusTrack.Api` — ASP.NET Core Web API + SignalR-hub (`/hubs/vehicles`)
- `frontend/` — React + TypeScript + Vite + MapLibre GL JS

## Kom igång — backend

1. Installera [.NET 10 SDK](https://dotnet.microsoft.com/download).
2. `dotnet restore` i repo-roten.
3. Skaffa en gratis API-nyckel för [Trafiklab GTFS Sweden 3](https://www.trafiklab.se/api/gtfs-datasets/gtfs-sweden/) och hämta URL:en för Skånetrafikens VehiclePositions-flöde från ditt Trafiklab-konto.
4. Sätt hemligheter lokalt med .NET user-secrets (kör i respektive projektmapp):

   ```
   cd src/BusTrack.Collector
   dotnet user-secrets init
   dotnet user-secrets set "ConnectionStrings:BusTrackDb" "<din Azure SQL-connection string>"
   dotnet user-secrets set "Trafiklab:ApiKey" "<din Trafiklab API-nyckel>"
   dotnet user-secrets set "Trafiklab:VehiclePositionsUrl" "<VehiclePositions-URL från Trafiklab>"
   ```

   Upprepa för `src/BusTrack.Api` med samma connection string (och senare `Azure:SignalR:ConnectionString`).

5. Kör Collector: `dotnet run --project src/BusTrack.Collector`
6. Kör API: `dotnet run --project src/BusTrack.Api`

## Kom igång — frontend

```
cd frontend
npm install
npm run dev
```

Frontend förväntar sig API:et på `VITE_API_BASE_URL` (se `.env.example`), annars `https://localhost:5001`.

## Status

**Steg 0 — repo-scaffold: klart.** Solution, projektstruktur och ett körbart skelett för alla fyra .NET-projekt samt frontend finns på plats.

Nästa steg enligt byggplanen: **Steg 1 (MVP)** — Collector hämtar riktiga fordonspositioner från Skånetrafiken, API:et exponerar dem via `/api/vehicles`, och frontend visar dem som rörliga markörer på kartan.

## Datakälla

[Trafiklab GTFS Sweden 3](https://www.trafiklab.se/api/gtfs-datasets/gtfs-sweden/), CC0-licens. Skånetrafiken är en av de ~57 operatörer som ingår i flödet.
