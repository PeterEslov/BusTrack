namespace BusTrack.Domain.Entities;

// Fas 3: trafikstörningar, från GTFS-RT Alerts.
public class ServiceAlert
{
    public string Id { get; set; } = default!;
    public string? AffectedRouteId { get; set; }
    public string Cause { get; set; } = default!;
    public string Effect { get; set; } = default!;
    public string HeaderText { get; set; } = default!;
    public DateTimeOffset? ActiveFrom { get; set; }
    public DateTimeOffset? ActiveTo { get; set; }
}
