using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace JellyfinPlayer.Lib.Storage;

/// <summary>
/// Factory for creating CacheDbContext instances.
/// This should be registered in the Player project with the appropriate database path.
/// </summary>
public sealed class CacheDbContextFactory(string dbPath) : IDbContextFactory<CacheDbContext>
{
    public CacheDbContext CreateDbContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<CacheDbContext>();
        optionsBuilder.UseSqlite($"Data Source={dbPath}");

        return new CacheDbContext(optionsBuilder.Options);
    }
}

/// <summary>
/// Design-time factory for Entity Framework migrations.
/// </summary>
public sealed class CacheDbContextDesignTimeFactory : IDesignTimeDbContextFactory<CacheDbContext>
{
    public CacheDbContext CreateDbContext(string[] args)
    {
        // Use a temporary path for design-time migrations
        var dbPath = Path.Combine(Path.GetTempPath(), "jellyfin_cache.db");

        var optionsBuilder = new DbContextOptionsBuilder<CacheDbContext>();
        optionsBuilder.UseSqlite($"Data Source={dbPath}");

        return new CacheDbContext(optionsBuilder.Options);
    }
}
