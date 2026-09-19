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
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
