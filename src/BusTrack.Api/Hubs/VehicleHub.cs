using Microsoft.AspNetCore.SignalR;

namespace BusTrack.Api.Hubs;

// Steg 2: klienter prenumererar på en linje så de bara får uppdateringar
// som är relevanta för dem, istället för alla fordon i hela nätet.
public class VehicleHub : Hub
{
    public async Task SubscribeToRoute(string routeId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"route-{routeId}");
    }

    public async Task UnsubscribeFromRoute(string routeId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"route-{routeId}");
    }
}
