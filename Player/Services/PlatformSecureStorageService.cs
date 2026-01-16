using System.Text.Json;
using JellyfinPlayer.Lib.Storage;
using Microsoft.Extensions.Logging;

namespace Player.Services;

public sealed class PlatformSecureStorageService(ILogger<PlatformSecureStorageService> logger)
    : ISecureStorageService
{
    public async Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            return await SecureStorage.Default.GetAsync(key);
        }
        catch (Exception)
        {
            // SecureStorage may throw if key doesn't exist or on some platforms
            return null;
        }
    }

    public async Task SetAsync(
        string key,
        string value,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);

        await SecureStorage.Default.SetAsync(key, value);
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            var json = await SecureStorage.Default.GetAsync(key);
            if (string.IsNullOrWhiteSpace(json))
                return null;

            return JsonSerializer.Deserialize<T>(json);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize stored value for key '{Key}'", key);
            return null;
        }
        catch (Exception)
        {
            // SecureStorage may throw if key doesn't exist - this is expected
            return null;
        }
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        CancellationToken cancellationToken = default
    )
        where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);

        var json = JsonSerializer.Serialize(value);
        await SecureStorage.Default.SetAsync(key, json);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            SecureStorage.Default.Remove(key);
            await Task.CompletedTask;
        }
        catch (Exception)
        {
            // Key may not exist, ignore
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            var value = await SecureStorage.Default.GetAsync(key);
            return value is not null;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
