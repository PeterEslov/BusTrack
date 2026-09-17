using ProtoBuf;
using TransitRealtime;

namespace BusTrack.Infrastructure.GtfsRealtime;

// Hämtar och parsar GTFS-realtime-flödet (protobuf) från Trafiklab GTFS Sweden 3.
// Se: https://www.trafiklab.se/api/gtfs-datasets/gtfs-sweden/realtime-specification/
//
// OBS: exakta egenskapsnamn på FeedMessage/FeedEntity nedan bör verifieras mot
// GtfsRealtimeBindings-paketet efter `dotnet restore`, då de genereras direkt
// från protobuf-specifikationen och kan skilja sig något mellan versioner.
public class GtfsRealtimeClient
{
    private readonly HttpClient _httpClient;

    public GtfsRealtimeClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<FeedMessage> GetFeedAsync(string feedUrl, CancellationToken cancellationToken = default)
    {
        await using var stream = await _httpClient.GetStreamAsync(feedUrl, cancellationToken);
        return Serializer.Deserialize<FeedMessage>(stream);
    }
}
