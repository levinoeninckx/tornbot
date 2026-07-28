using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TornBot.Bot.Domain.Enums;
using TornBot.Bot.Domain.Models;

namespace TornBot.Bot.Infrastructure;

public class TornbotContext(DbContextOptions<TornbotContext> options) : DbContext(options)
{
    public DbSet<Faction> Factions { get; set; }
    public DbSet<ApiKey> ApiKeys { get; set; }
    public DbSet<ModuleConfig> ModuleConfigs { get; set; }
    public DbSet<FactionCrime> OrganizedCrimes { get; set; }
    public DbSet<RetalOpportunity> TrackedAttacks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApiKey>()
            .HasKey(x => x.Id);
        modelBuilder.Entity<ApiKey>()
            .Property(x => x.Key)
            .HasMaxLength(30)
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

public static class TornbotContextExtensions
{
    public static Task<Faction?> GetFactionByGuildIdAsync(this DbSet<Faction> factions, ulong guildId,
        bool includeApiKeys = false, bool includeModuleConfigs = false, bool includeTrackedAttacks = false)
    {
        var queryable = factions
            .AsQueryable();

        if (includeApiKeys)
        {
            queryable.Include(f => f.ApiKeys);
        }

        if (includeModuleConfigs)
        {
            queryable.Include(f => f.ModuleConfigs);
        }

        if (includeTrackedAttacks)
        {
            queryable.Include(f => f.TrackedAttacks);
        }

        return queryable.SingleOrDefaultAsync(f => f.GuildId == guildId);
    }

    public static async Task<bool> UpdateModuleConfig(this DbSet<Faction> factions, ulong guildId, Module module,
        JsonDocument config)
    {
        var faction = await factions
            .Include(f => f.ModuleConfigs)
            .SingleOrDefaultAsync(f => f.GuildId == guildId);
        if (faction == null) return false;

        foreach (var moduleConfig in faction.ModuleConfigs)
        {
            if (moduleConfig.Module == module)
            {
                moduleConfig.Config = config;
                return true;
            }
        }

        return false;
    }
}