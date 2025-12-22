using JellyfinPlayer.Lib.Storage;

namespace Player.Services;

public sealed class PlatformSecureStorageService : ISecureStorageService
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
