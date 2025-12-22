using Microsoft.EntityFrameworkCore;

namespace JellyfinPlayer.Lib.Storage;

public sealed class CacheDbContext(DbContextOptions<CacheDbContext> options) : DbContext(options)
{
    public DbSet<CacheEntry> CacheEntries { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CacheEntry>(entity =>
        {
            entity.HasKey(e => e.Key);
            entity.HasIndex(e => e.ExpiresAt);
            entity.Property(e => e.Key).HasMaxLength(500);
            entity.Property(e => e.Value).IsRequired();
        });
    }
}
