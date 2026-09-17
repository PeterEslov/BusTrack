namespace BusTrack.Domain.Entities;

public class Trip
{
    public string Id { get; set; } = default!;
    public string RouteId { get; set; } = default!;
    public string Headsign { get; set; } = default!;
    public int DirectionId { get; set; }

    public Route? Route { get; set; }
}
