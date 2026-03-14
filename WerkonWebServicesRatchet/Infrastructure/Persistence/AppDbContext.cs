using Microsoft.EntityFrameworkCore;
using WerkonWebServicesRatchet.Domain.Entities;

namespace WerkonWebServicesRatchet.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Client> Clients => Set<Client>();
}
