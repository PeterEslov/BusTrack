using BusTrack.Domain.Entities;
using BusTrack.Infrastructure;
using BusTrack.Infrastructure.GtfsRealtime;
using Microsoft.EntityFrameworkCore;

namespace BusTrack.Collector;

// MVP (Steg 1): pollar Skånetrafikens GTFS-RT VehiclePositions (via Trafiklab GTFS Sweden 3)
// var 15:e sekund och sparar senaste fordonspositionerna. I Steg 2 kompletteras/ersätts
// pollingen av en SignalR-push direkt från Collector till API/frontend.
public class Worker : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(15);

    private readonly ILogger<Worker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly GtfsRealtimeClient _gtfsClient;
    private readonly IConfiguration _configuration;
    private readonly VehicleUpdatePublisher _publisher;

    public Worker(
        ILogger<Worker> logger,
        IServiceScopeFactory scopeFactory,
        GtfsRealtimeClient gtfsClient,
        IConfiguration configuration,
        VehicleUpdatePublisher publisher)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _gtfsClient = gtfsClient;
        _configuration = configuration;
        _publisher = publisher;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var feedUrl = _configuration["Trafiklab:VehiclePositionsUrl"];
        if (string.IsNullOrWhiteSpace(feedUrl))
        {
            throw new InvalidOperationException(
                "Trafiklab:VehiclePositionsUrl saknas. Sätt den med dotnet user-secrets (se README).");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var feed = await _gtfsClient.GetFeedAsync(feedUrl, stoppingToken);

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<BusTrackDbContext>();

                var updates = new List<VehicleUpdateDto>();
                foreach (var entity in feed.Entities)
                {
                    if (entity.Vehicle is null)
                    {
                        continue;
                    }

                    var position = new VehiclePosition
                    {
                        VehicleId = entity.Vehicle.Vehicle?.Id ?? entity.Id,
                        TripId = entity.Vehicle.Trip?.TripId,
                        Lat = entity.Vehicle.Position.Latitude,
                        Lon = entity.Vehicle.Position.Longitude,
                        Bearing = entity.Vehicle.Position.Bearing,
                        Speed = entity.Vehicle.Position.Speed,
                        Timestamp = DateTimeOffset.FromUnixTimeSeconds((long)entity.Vehicle.Timestamp)
                    };
                    db.VehiclePositions.Add(position);

                    updates.Add(new VehicleUpdateDto(
                        position.VehicleId,
                        position.TripId,
                        position.Lat,
                        position.Lon,
                        position.Bearing,
                        position.Speed,
                        position.Timestamp));
                }

                await db.SaveChangesAsync(stoppingToken);
                _logger.LogInformation("Sparade {Count} fordonspositioner", updates.Count);

                await _publisher.PublishAsync(updates, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kunde inte hämta eller spara fordonspositioner");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }
}
