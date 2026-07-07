using Microsoft.EntityFrameworkCore;
using TornBot.Bot.Domain.Models;

namespace TornBot.Bot.Infrastructure;

public class TornbotContext(DbContextOptions<TornbotContext> options) : DbContext(options)
{
    public DbSet<Faction> Factions { get; set; }
    public DbSet<ApiKey> ApiKeys { get; set; }
    public DbSet<ModuleConfig> ModuleConfigs { get; set; }
    public DbSet<OrganizedCrime> OrganizedCrimes { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApiKey>()
            .HasKey(x => x.Id);
        modelBuilder.Entity<ApiKey>()
            .Property(x => x.Key)
            .HasMaxLength(16)
            .IsRequired();
        modelBuilder.Entity<ApiKey>()
            .Property(x => x.CreatedAt)
            .ValueGeneratedOnAdd()
            .IsRequired();
        modelBuilder.Entity<ApiKey>()
            .Property(x => x.AccessLevel)
            .IsRequired();
        modelBuilder.Entity<ApiKey>()
            .Property(x => x.TornPlayerId)
            .IsRequired();
        modelBuilder.Entity<ApiKey>()
            .Property(x => x.UsageCount)
            .HasDefaultValue(0)
            .IsRequired();
    }
}