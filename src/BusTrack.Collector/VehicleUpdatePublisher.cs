using System.Net.Http.Json;

namespace BusTrack.Collector;

public record VehicleUpdateDto(
    string VehicleId,
    string? TripId,
    double Lat,
    double Lon,
    float? Bearing,
    float? Speed,
    DateTimeOffset Timestamp);

// Skickar nyss hämtade fordonspositioner till Api:et, som i sin tur
// broadcastar dem till alla uppkopplade webbläsare via SignalR-huben.
// Collector pratar bara HTTP hit - den vet inget om SignalR eller vilka
// klienter som lyssnar.
public class VehicleUpdatePublisher
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<VehicleUpdatePublisher> _logger;
    private readonly string? _sharedKey;

    public VehicleUpdatePublisher(HttpClient httpClient, IConfiguration configuration, ILogger<VehicleUpdatePublisher> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _sharedKey = configuration["Internal:SharedKey"];
    }

    public async Task PublishAsync(IReadOnlyList<VehicleUpdateDto> updates, CancellationToken cancellationToken)
    {
        if (updates.Count == 0)
        {
            return;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "internal/vehicle-updates")
        {
            Content = JsonContent.Create(updates)
        };

        if (!string.IsNullOrEmpty(_sharedKey))
        {
            request.Headers.Add("X-Internal-Key", _sharedKey);
        }

        try
        {
            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Kunde inte publicera fordonsuppdateringar till Api:et, status {StatusCode}",
                    response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            // Api:et kanske inte körs just nu - inte kritiskt, positionerna ligger
            // redan sparade i databasen och hämtas ändå via REST vid nästa sidladdning.
            _logger.LogWarning(ex, "Kunde inte nå Api:et för att publicera fordonsuppdateringar");
        }
    }
}
