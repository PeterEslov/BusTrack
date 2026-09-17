namespace BusTrack.Domain.Entities;

public class Route
{
    public string Id { get; set; } = default!;
    public string AgencyId { get; set; } = default!;
    public string ShortName { get; set; } = default!;
    public string LongName { get; set; } = default!;

    // GTFS route_type, t.ex. 3 = buss.
    public int Type { get; set; }

    public Agency? Agency { get; set; }
    public ICollection<Trip> Trips { get; set; } = new List<Trip>();
}
