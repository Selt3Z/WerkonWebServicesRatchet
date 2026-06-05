using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WerkonWebServicesRatchet.Domain.Entities;
using WerkonWebServicesRatchet.Infrastructure.Identity;

namespace WerkonWebServicesRatchet.Infrastructure.Persistence;

public sealed class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
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
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<VisitServiceItem>().ToTable("VisitServiceItems");
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(x => x.DisplayName).HasMaxLength(200);
        });
    }
}
