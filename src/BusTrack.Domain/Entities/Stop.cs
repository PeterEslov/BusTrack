namespace BusTrack.Domain.Entities;

public class Stop
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public double Lat { get; set; }
    public double Lon { get; set; }
}
