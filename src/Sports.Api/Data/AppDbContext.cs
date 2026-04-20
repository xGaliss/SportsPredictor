using Microsoft.EntityFrameworkCore;
using Sports.Domain.Entities;

namespace Sports.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Game> Games => Set<Game>();
    public DbSet<TeamGameStat> TeamGameStats => Set<TeamGameStat>();
    public DbSet<PlayerGameStat> PlayerGameStats => Set<PlayerGameStat>();
    public DbSet<Prediction> Predictions => Set<Prediction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Team>().HasIndex(x => x.ExternalId).IsUnique();
        modelBuilder.Entity<Player>().HasIndex(x => x.ExternalId).IsUnique();
        modelBuilder.Entity<Game>().HasIndex(x => x.ExternalId).IsUnique();
        modelBuilder.Entity<Prediction>().HasIndex(x => x.GameId).IsUnique();
        base.OnModelCreating(modelBuilder);
    }
}
