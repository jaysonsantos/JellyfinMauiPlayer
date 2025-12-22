using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using JellyfinPlayer.Lib.Api;
using Microsoft.Extensions.Logging;

namespace JellyfinPlayer.Lib.Services;

public interface IImageCacheService
{
    Task<string?> GetCachedImagePathAsync(
        string imageUrl,
        CancellationToken cancellationToken = default
    );
    Task<string?> DownloadAndCacheImageAsync(
        string imageUrl,
        CancellationToken cancellationToken = default
    );
    Task<bool> IsImageCachedAsync(string imageUrl, CancellationToken cancellationToken = default);
    Task ClearCacheAsync(CancellationToken cancellationToken = default);
    Task<long> GetCacheSizeAsync(CancellationToken cancellationToken = default);
}

public sealed class ImageCacheService(string cacheDirectory, ILogger<ImageCacheService> logger)
    : IImageCacheService
{
    private const int MaxCacheSizeBytes = 500 * 1024 * 1024; // 500 MB
    private const int BufferSize = 8192; // 8 KB buffer for efficient reads

    public async Task<string?> GetCachedImagePathAsync(
        string imageUrl,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(imageUrl);

        var cacheKey = GenerateCacheKey(imageUrl);
        var cachePath = GetCacheFilePath(cacheKey);

        if (File.Exists(cachePath))
        {
            return cachePath;
        }

        // If not cached, download it
        return await DownloadAndCacheImageAsync(imageUrl, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string?> DownloadAndCacheImageAsync(
        string imageUrl,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(imageUrl);

        try
        {
            var cacheKey = GenerateCacheKey(imageUrl);
            var cachePath = GetCacheFilePath(cacheKey);

            // Check if already cached
            if (File.Exists(cachePath))
            {
                return cachePath;
            }

            // Ensure cache directory exists
            Directory.CreateDirectory(cacheDirectory);

            // Download image using HttpClient
            var httpClient = await CreateHttpClientAsync(cancellationToken).ConfigureAwait(false);
            if (httpClient is null)
            {
                logger.LogWarning("Cannot download image: API client not available");
                return null;
            }

            using var response = await httpClient
                .GetAsync(imageUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            // Check content type
            var contentType = response.Content.Headers.ContentType?.MediaType;
            if (
                contentType is null
                || !contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
            )
            {
                logger.LogWarning(
                    "Invalid content type for image URL: {ImageUrl}, ContentType: {ContentType}",
                    imageUrl,
                    contentType
                );
                return null;
            }

            // Determine file extension from content type
            var extension = GetExtensionFromContentType(contentType);
            cachePath = Path.ChangeExtension(cachePath, extension);

            // Download with efficient buffer management using ArrayPool
            var fileStream = new FileStream(
                cachePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                BufferSize,
                useAsync: true
            );

            await using var stream = fileStream.ConfigureAwait(false);
            var contentStream = await response
                .Content.ReadAsStreamAsync(cancellationToken)
                .ConfigureAwait(false);
            await using (contentStream.ConfigureAwait(false))
            {
                // Use Span<T> and Memory<T> for efficient buffer operations (C# 14 first-class span support)
                var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
                try
                {
                    var memory = buffer.AsMemory(0, BufferSize);
                    int bytesRead;
                    while (
                        (
                            bytesRead = await contentStream
                                .ReadAsync(memory, cancellationToken)
                                .ConfigureAwait(false)
                        ) > 0
                    )
                    {
                        // Use Memory<T> slice to write only the bytes we read
                        var writeMemory = buffer.AsMemory(0, bytesRead);
                        await fileStream
                            .WriteAsync(writeMemory, cancellationToken)
                            .ConfigureAwait(false);
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }

                logger.LogDebug("Cached image: {ImageUrl} -> {CachePath}", imageUrl, cachePath);
                return cachePath;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to download and cache image: {ImageUrl}", imageUrl);
            return null;
        }
    }

    public Task<bool> IsImageCachedAsync(
        string imageUrl,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(imageUrl);

        var cacheKey = GenerateCacheKey(imageUrl);
        var cachePath = GetCacheFilePath(cacheKey);

        // Check common image extensions
        var extensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        foreach (var ext in extensions)
        {
            var pathWithExt = Path.ChangeExtension(cachePath, ext);
            if (File.Exists(pathWithExt))
            {
                return Task.FromResult(true);
            }
        }

        return Task.FromResult(false);
    }

    public async Task ClearCacheAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Directory.Exists(cacheDirectory))
            {
                return;
            }

            var files = await Task.Run(
                    () => Directory.GetFiles(cacheDirectory, "*", SearchOption.AllDirectories),
                    cancellationToken
                )
                .ConfigureAwait(false);
            foreach (var file in files)
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to delete cache file: {File}", file);
                }
            }

            logger.LogInformation("Cleared image cache: {Count} files deleted", files.Length);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to clear image cache");
            throw;
        }
    }

    public async Task<long> GetCacheSizeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Directory.Exists(cacheDirectory))
            {
                return 0L;
            }

            var files = await Task.Run(
                    () => Directory.GetFiles(cacheDirectory, "*", SearchOption.AllDirectories),
                    cancellationToken
                )
                .ConfigureAwait(false);
            long totalSize = 0;

            foreach (var file in files)
            {
                try
                {
                    var fileInfo = new FileInfo(file);
                    totalSize += fileInfo.Length;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to get file size: {File}", file);
                }
            }

            return totalSize;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to calculate cache size");
            return 0L;
        }
    }

    private string GenerateCacheKey(string imageUrl)
    {
        // Use SHA256 hash of URL as cache key for consistent naming
        var urlBytes = Encoding.UTF8.GetBytes(imageUrl);
        var hashBytes = SHA256.HashData(urlBytes);

        // Convert hash to hex string using Span<char> for efficiency
        return Convert.ToHexString(hashBytes);
    }

    private string GetCacheFilePath(string cacheKey)
    {
        // Use first 2 characters for directory structure to avoid too many files in one directory
        var subDir = cacheKey[..2];
        var subDirPath = Path.Combine(cacheDirectory, subDir);
        Directory.CreateDirectory(subDirPath);

        return Path.Combine(subDirPath, cacheKey);
    }

    private static string GetExtensionFromContentType(string contentType)
    {
        return contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            "image/bmp" => ".bmp",
            _ => ".jpg", // Default to jpg
        };
    }

    private async Task<HttpClient?> CreateHttpClientAsync(
        CancellationToken cancellationToken = default
    )
    {
        // Create HttpClient with authentication headers for Jellyfin
        // The imageUrl should be a full URL including the server base URL
        var httpClient = new HttpClient();

        // Set timeout
        httpClient.Timeout = TimeSpan.FromSeconds(30);

        return httpClient;
    }
}
