using BusTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusTrack.Infrastructure;

public class BusTrackDbContext : DbContext
{
    public BusTrackDbContext(DbContextOptions<BusTrackDbContext> options) : base(options)
    {
    }

    public DbSet<Agency> Agencies => Set<Agency>();
    public DbSet<Route> Routes => Set<Route>();
    public DbSet<Stop> Stops => Set<Stop>();
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<VehiclePosition> VehiclePositions => Set<VehiclePosition>();
    public DbSet<TripUpdateRecord> TripUpdates => Set<TripUpdateRecord>();
    public DbSet<ServiceAlert> ServiceAlerts => Set<ServiceAlert>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Route>()
            .HasOne(r => r.Agency)
            .WithMany()
            .HasForeignKey(r => r.AgencyId);

        modelBuilder.Entity<Trip>()
            .HasOne(t => t.Route)
            .WithMany(r => r.Trips)
            .HasForeignKey(t => t.RouteId);

        // Senaste position per fordon slås upp ofta av API:et — indexera för snabba frågor.
        modelBuilder.Entity<VehiclePosition>()
            .HasIndex(v => new { v.VehicleId, v.Timestamp });

        modelBuilder.Entity<TripUpdateRecord>()
            .HasIndex(t => new { t.TripId, t.StopId });
    }
}
