using BusTrack.Collector;
using BusTrack.Infrastructure;
using BusTrack.Infrastructure.GtfsRealtime;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<BusTrackDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("BusTrackDb")));

builder.Services.AddHttpClient<GtfsRealtimeClient>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
