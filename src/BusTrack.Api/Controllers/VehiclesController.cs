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
        var latest = await _db.VehiclePositions
            .GroupBy(v => v.VehicleId)
            .Select(g => g.OrderByDescending(v => v.Timestamp).First())
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
