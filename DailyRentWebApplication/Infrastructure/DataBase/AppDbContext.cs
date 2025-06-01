using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.DataBase;

public class AppDbContext(IConfiguration configuration) : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Property> Properties { get; set; }
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<CompensationRequest> CompensationRequests { get; set; }
    public DbSet<Amenity> Amenities { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<PropertyImage> PropertyImages { get; set; }
    public DbSet<Location> Locations { get; set; }
    public DbSet<RefreshSession> RefreshSessions { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .UseSnakeCaseNamingConvention()
            .UseNpgsql(configuration.GetConnectionString("DbConnection"));
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}