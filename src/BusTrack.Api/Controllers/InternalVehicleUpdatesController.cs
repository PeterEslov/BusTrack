using BusTrack.Api.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace BusTrack.Api.Controllers;

// Internt endpoint - anropas bara av Collector-processen, aldrig av webbläsaren.
// Skyddas med en delad hemlighet i headern (samma värde satt via user-secrets i
// både Api och Collector) eftersom det inte finns någon användarautentisering
// här. Utan det skulle vem som helst som hittar URL:en kunna posta påhittade
// fordonspositioner till alla uppkopplade klienter.
[ApiController]
[Route("internal/vehicle-updates")]
public class InternalVehicleUpdatesController : ControllerBase
{
    private readonly IHubContext<VehicleHub> _hubContext;
    private readonly IConfiguration _configuration;

    public InternalVehicleUpdatesController(IHubContext<VehicleHub> hubContext, IConfiguration configuration)
    {
        _hubContext = hubContext;
        _configuration = configuration;
    }

    public record VehicleUpdateDto(
        string VehicleId,
        string? TripId,
        double Lat,
        double Lon,
        float? Bearing,
        float? Speed,
        DateTimeOffset Timestamp);

    [HttpPost]
    public async Task<IActionResult> Post(
        [FromBody] List<VehicleUpdateDto> updates,
        [FromHeader(Name = "X-Internal-Key")] string? key,
        CancellationToken cancellationToken)
    {
        var expectedKey = _configuration["Internal:SharedKey"];
        if (string.IsNullOrEmpty(expectedKey) || key != expectedKey)
        {
            return Unauthorized();
        }

        if (updates.Count > 0)
        {
            await _hubContext.Clients.All.SendAsync("vehiclePositionsUpdated", updates, cancellationToken);
        }

        return Ok();
    }
}
