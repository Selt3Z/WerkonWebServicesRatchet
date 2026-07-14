using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
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
    public DbSet<Reminder> Reminders => Set<Reminder>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<CatalogService> CatalogServices => Set<CatalogService>();
    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<VisitServiceItem>().ToTable("VisitServiceItems");

        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasIndex(x => x.PhoneNumber);
            entity.HasIndex(x => x.FullName);
            entity.HasIndex(x => x.IsArchived);
            entity.HasQueryFilter(x => !x.IsArchived);
        });

        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.HasIndex(x => x.LicensePlate);
            entity.HasIndex(x => x.Vin);
            entity.HasIndex(x => x.IsArchived);
            entity.HasQueryFilter(x => !x.IsArchived);
        });

        modelBuilder.Entity<Visit>(entity =>
        {
            entity.Property(x => x.Number)
                .ValueGeneratedOnAdd()
                .UseIdentityByDefaultColumn();
            entity.Property(x => x.Number).Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);

            entity.HasIndex(x => x.Number).IsUnique();
            entity.HasIndex(x => x.VisitedAtUtc);
            entity.HasIndex(x => x.AssignedMechanicUserId);
            entity.HasIndex(x => x.IsArchived);
            entity.HasQueryFilter(x => !x.IsArchived);

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.AssignedMechanicUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Reminder>(entity =>
        {
            entity.HasIndex(x => x.ReminderAtUtc);

            entity.HasOne(x => x.Vehicle)
                .WithMany(x => x.Reminders)
                .HasForeignKey(x => x.VehicleId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<AppSetting>(entity =>
        {
            entity.HasKey(x => x.Key);
            entity.Property(x => x.Key).HasMaxLength(128);
            entity.Property(x => x.Value).HasColumnType("text");
        });

        modelBuilder.Entity<AuditLogEntry>(entity =>
        {
            entity.HasIndex(x => x.OccurredAtUtc);
            entity.Property(x => x.UserDisplayName).HasMaxLength(200);
            entity.Property(x => x.Action).HasMaxLength(32);
            entity.Property(x => x.EntityType).HasMaxLength(64);
            entity.Property(x => x.Summary).HasMaxLength(500);
            entity.Property(x => x.EntityUrl).HasMaxLength(256);
        });

        modelBuilder.Entity<CatalogService>(entity =>
        {
            entity.HasIndex(x => x.Name);
            entity.HasIndex(x => x.Code);
            entity.HasIndex(x => x.Category);
            entity.Property(x => x.Name).HasMaxLength(200);
            entity.Property(x => x.Code).HasMaxLength(50);
            entity.Property(x => x.Category).HasMaxLength(100);
            entity.Property(x => x.DefaultUnit).HasMaxLength(20);
            entity.Property(x => x.Description).HasMaxLength(1000);
        });

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(x => x.DisplayName).HasMaxLength(200);
        });
    }
}
