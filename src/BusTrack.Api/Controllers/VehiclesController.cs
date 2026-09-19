using BusTrack.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BusTrack.Api.Controllers;

[ApiController]
[Route("api/vehicles")]
public class VehiclesController : ControllerBase
{
    private readonly BusTrackDbContext _db;

    public VehiclesController(BusTrackDbContext db)
    {
        _db = db;
    }

    // Fordon som inte synts till i feeden pa detta antal minuter rakas bort -
    // annars vaxer "senaste positionen" for ett fordon som gatt ur trafik kvar
    // for alltid, och kartan fylls med spokbussar som star still pa samma
    // koordinat i evighet. Collector pollar var 15:e sekund, sa 5 minuter ger
    // gott om marginal for enstaka missade pollningar utan att kanslan av
    // "live" gar forlorad.
    private static readonly TimeSpan StaleAfter = TimeSpan.FromMinutes(5);

    // MVP (Steg 1): senaste kanda position per fordon (inom StaleAfter).
    // Steg 2: anvands numera bara for forsta laddningen - SignalR tar over
    // efter det.
    [HttpGet]
    public async Task<IActionResult> GetLatestPositions(CancellationToken cancellationToken)
    {
        var staleThreshold = DateTimeOffset.UtcNow - StaleAfter;
        var recentPositions = _db.VehiclePositions.Where(v => v.Timestamp >= staleThreshold);

        // OBS: GroupBy(...).Select(g => g.OrderBy(...).First()) gar INTE att
        // oversatta till SQL av EF Core (kastar InvalidOperationException:
        // "could not be translated" vid korning). Losningen ar att forst
        // hitta senaste tidsstampeln per fordon, och sedan joina tillbaka
        // for att fa hela raden - det har EF Core stod for.
        var latestTimestamps = recentPositions
            .GroupBy(v => v.VehicleId)
            .Select(g => new { VehicleId = g.Key, MaxTimestamp = g.Max(v => v.Timestamp) });

        var latest = await recentPositions
            .Join(latestTimestamps,
                v => new { v.VehicleId, Timestamp = v.Timestamp },
                lt => new { lt.VehicleId, Timestamp = lt.MaxTimestamp },
                (v, lt) => v)
            .ToListAsync(cancellationToken);

        return Ok(latest);
    }

    [HttpGet("{vehicleId}")]
    public async Task<IActionResult> GetVehicle(string vehicleId, CancellationToken cancellationToken)
    {
        var position = await _db.VehiclePositions
            .Where(v => v.VehicleId == vehicleId)
            .OrderByDescending(v => v.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);

        return position is null ? NotFound() : Ok(position);
    }
}
