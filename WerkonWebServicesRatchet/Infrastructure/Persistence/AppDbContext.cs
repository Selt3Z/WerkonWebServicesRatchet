using Microsoft.EntityFrameworkCore;
using WerkonWebServicesRatchet.Domain.Entities;

namespace WerkonWebServicesRatchet.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Visit> Visits => Set<Visit>();
    public DbSet<VisitServiceItem> VisitServiceItems => Set<VisitServiceItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<VisitServiceItem>().ToTable("VisitServiceItems");
    }
}
