using Microsoft.EntityFrameworkCore;
using TornBot.Infrastructure.Persistence.Entities;

namespace TornBot.Infrastructure.Persistence;

public class TornbotContext(DbContextOptions<TornbotContext> options) : DbContext(options)
{
    public DbSet<FactionEntity> Factions { get; set; }
    public DbSet<ApiKeyEntity> ApiKeys { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApiKeyEntity>()
            .ToTable("ApiKeys");
        modelBuilder.Entity<ApiKeyEntity>()
            .HasKey(x => x.Id);
        modelBuilder.Entity<ApiKeyEntity>()
            .Property(x => x.Key)
            .HasMaxLength(30)
            .IsRequired();
        modelBuilder.Entity<ApiKeyEntity>()
            .Property(x => x.CreatedAt)
            .ValueGeneratedOnAdd()
            .IsRequired();
        modelBuilder.Entity<ApiKeyEntity>()
            .Property(x => x.AccessLevel)
            .IsRequired();
        modelBuilder.Entity<ApiKeyEntity>()
            .Property(x => x.TornPlayerId)
            .IsRequired();
        modelBuilder.Entity<ApiKeyEntity>()
            .Property(x => x.UsageCount)
            .HasDefaultValue(0)
            .IsRequired();
    }
}