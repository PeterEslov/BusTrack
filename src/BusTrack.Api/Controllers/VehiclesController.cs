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

    // MVP (Steg 1): senaste kända position per fordon. Frontend pollar denna
    // tills SignalR-push kopplas in i Steg 2.
    [HttpGet]
    public async Task<IActionResult> GetLatestPositions(CancellationToken cancellationToken)
    {
        // OBS: GroupBy(...).Select(g => g.OrderBy(...).First()) gar INTE att
        // oversatta till SQL av EF Core (kastar InvalidOperationException:
        // "could not be translated" vid korning). Losningen ar att forst
        // hitta senaste tidsstampeln per fordon, och sedan joina tillbaka
        // for att fa hela raden - det har EF Core stod for.
        var latestTimestamps = _db.VehiclePositions
            .GroupBy(v => v.VehicleId)
            .Select(g => new { VehicleId = g.Key, MaxTimestamp = g.Max(v => v.Timestamp) });

        var latest = await _db.VehiclePositions
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
