using BusTrack.Api.Hubs;
using BusTrack.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<BusTrackDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("BusTrackDb")));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var signalRBuilder = builder.Services.AddSignalR();
var azureSignalRConnectionString = builder.Configuration["Azure:SignalR:ConnectionString"];
if (!string.IsNullOrWhiteSpace(azureSignalRConnectionString))
{
    // I produktion (Azure) skalas SignalR via Azure SignalR Service.
    // Lokalt, utan connection string, körs SignalR i-process precis som vanligt.
    signalRBuilder.AddAzureSignalR(azureSignalRConnectionString);
}

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:5173"])
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.MapControllers();
app.MapHub<VehicleHub>("/hubs/vehicles");

app.Run();
