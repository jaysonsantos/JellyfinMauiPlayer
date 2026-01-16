namespace JellyfinPlayer.Lib.Storage;

public interface ISecureStorageService
{
    Task<string?> GetAsync(string key, CancellationToken cancellationToken = default);
    Task SetAsync(string key, string value, CancellationToken cancellationToken = default);
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        where T : class;
    Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default)
        where T : class;
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
}
