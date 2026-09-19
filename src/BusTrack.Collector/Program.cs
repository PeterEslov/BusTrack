using BusTrack.Collector;
using BusTrack.Infrastructure;
using BusTrack.Infrastructure.GtfsRealtime;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

// Ladda user-secrets oavsett miljö (annars läses de bara i "Development",
// och som Worker Service startar appen i "Production" om inget annat sägs).
builder.Configuration.AddUserSecrets<Program>();

builder.Services.AddDbContext<BusTrackDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("BusTrackDb")));

builder.Services.AddHttpClient<GtfsRealtimeClient>()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        // Trafiklabs GTFS-RT-API kräver att klienten stödjer gzip (Accept-Encoding),
        // annars svarar den 406 Not Acceptable.
        AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
    });

// Steg 2: Collector publicerar nya positioner till Api:et över HTTP, som i sin
// tur broadcastar dem till webbläsarna via SignalR (se VehicleUpdatePublisher).
builder.Services.AddHttpClient<VehicleUpdatePublisher>((sp, client) =>
{
    var baseUrl = sp.GetRequiredService<IConfiguration>()["Api:BaseUrl"] ?? "https://localhost:63364";
    client.BaseAddress = new Uri(baseUrl.EndsWith("/") ? baseUrl : baseUrl + "/");
});

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
