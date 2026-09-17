namespace BusTrack.Domain.Entities;

// Tidsserie: en rad per mottagen positionsuppdatering för ett fordon.
public class VehiclePosition
{
    public long Id { get; set; }
    public string VehicleId { get; set; } = default!;
    public string? TripId { get; set; }
    public double Lat { get; set; }
    public double Lon { get; set; }
    public float? Bearing { get; set; }
    public float? Speed { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
