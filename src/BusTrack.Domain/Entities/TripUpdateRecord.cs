namespace BusTrack.Domain.Entities;

// Fas 2: förseningar/ETA per hållplats, från GTFS-RT TripUpdates.
public class TripUpdateRecord
{
    public long Id { get; set; }
    public string TripId { get; set; } = default!;
    public string StopId { get; set; } = default!;
    public int DelaySeconds { get; set; }
    public DateTimeOffset? PredictedArrival { get; set; }
    public DateTimeOffset RecordedAt { get; set; }
}
