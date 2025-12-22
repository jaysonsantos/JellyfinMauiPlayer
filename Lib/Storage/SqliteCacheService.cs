using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace JellyfinPlayer.Lib.Storage;

public sealed class SqliteCacheService(
    IDbContextFactory<CacheDbContext> dbContextFactory,
    ILogger<SqliteCacheService> logger
) : IStorageService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var context = await dbContextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        await using (context.ConfigureAwait(false))
        {
            try
            {
                var entry = await context
                    .CacheEntries.FirstOrDefaultAsync(e => e.Key == key, cancellationToken)
                    .ConfigureAwait(false);

                if (entry is null)
                {
                    return null;
                }

                // Check expiration
                if (entry.ExpiresAt.HasValue && entry.ExpiresAt.Value < DateTime.UtcNow)
                {
                    // Entry expired, remove it
                    context.CacheEntries.Remove(entry);
                    await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    return null;
                }

                return JsonSerializer.Deserialize<T>(entry.Value, JsonOptions);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get cache entry for key {Key}", key);
                return null;
            }
        }
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default
    )
        where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);

        var context = await dbContextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        await using (context.ConfigureAwait(false))
        {
            try
            {
                var jsonValue = JsonSerializer.Serialize(value, JsonOptions);
                var expiresAt = expiration.HasValue
                    ? DateTime.UtcNow.Add(expiration.Value)
                    : (DateTime?)null;

                var existingEntry = await context
                    .CacheEntries.FirstOrDefaultAsync(e => e.Key == key, cancellationToken)
                    .ConfigureAwait(false);

                if (existingEntry is not null)
                {
                    existingEntry.Value = jsonValue;
                    existingEntry.ExpiresAt = expiresAt;
                }
                else
                {
                    var newEntry = new CacheEntry
                    {
                        Key = key,
                        Value = jsonValue,
                        ExpiresAt = expiresAt,
                    };
                    context.CacheEntries.Add(newEntry);
                }

                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to set cache entry for key {Key}", key);
                throw;
            }
        }
    }

    public async Task<bool> TryGetAsync<T>(
        string key,
        CancellationToken cancellationToken = default
    )
        where T : class
    {
        var result = await GetAsync<T>(key, cancellationToken).ConfigureAwait(false);
        return result is not null;
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var context = await dbContextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        await using (context.ConfigureAwait(false))
        {
            try
            {
                var entry = await context
                    .CacheEntries.FirstOrDefaultAsync(e => e.Key == key, cancellationToken)
                    .ConfigureAwait(false);

                if (entry is not null)
                {
                    context.CacheEntries.Remove(entry);
                    await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to remove cache entry for key {Key}", key);
            }
        }
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        var context = await dbContextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        await using (context.ConfigureAwait(false))
        {
            try
            {
                await context
                    .CacheEntries.ExecuteDeleteAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to clear cache");
                throw;
            }
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var context = await dbContextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        await using (context.ConfigureAwait(false))
        {
            try
            {
                var exists = await context
                    .CacheEntries.AnyAsync(
                        e =>
                            e.Key == key
                            && (!e.ExpiresAt.HasValue || e.ExpiresAt.Value >= DateTime.UtcNow),
                        cancellationToken
                    )
                    .ConfigureAwait(false);

                return exists;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to check cache entry existence for key {Key}", key);
                return false;
            }
        }
    }

    public async Task CleanupExpiredEntriesAsync(CancellationToken cancellationToken = default)
    {
        var context = await dbContextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        await using (context.ConfigureAwait(false))
        {
            try
            {
                var deletedCount = await context
                    .CacheEntries.Where(e =>
                        e.ExpiresAt.HasValue && e.ExpiresAt.Value < DateTime.UtcNow
                    )
                    .ExecuteDeleteAsync(cancellationToken)
                    .ConfigureAwait(false);

                if (deletedCount > 0)
                {
                    logger.LogInformation("Cleaned up {Count} expired cache entries", deletedCount);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to cleanup expired cache entries");
            }
        }
    }
}
