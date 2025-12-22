using JellyfinPlayer.Lib.Api;
using JellyfinPlayer.Lib.Services;
using JellyfinPlayer.Lib.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lib.Tests;

public class PlaybackIntegrationTests
{
    private readonly ServiceProvider _serviceProvider;
    private readonly AuthenticationService _authService;
    private readonly PlaybackService _playbackService;

    // Test configuration - update these with your server details
    private const string TestServerUrl = "http://localhost:8096"; // Update with your server URL
    private const string TestUsername = "guest";
    private const string TestPassword = "guest";
    private const string TestMovieId = "a615972e53fb47210d2b454172d2cbe6";

    public PlaybackIntegrationTests()
    {
        // Setup dependency injection
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        // Add HttpClient
        services.AddHttpClient();

        // Register services
        services.AddSingleton<ISecureStorageService, InMemorySecureStorage>();
        services.AddSingleton<IStorageService, InMemoryStorageService>();
        services.AddSingleton<DeviceInfoService>();
        services.AddSingleton<JellyfinApiClientFactory>();
        services.AddSingleton<AuthenticationService>();
        services.AddSingleton<PlaybackService>();

        _serviceProvider = services.BuildServiceProvider();

        _serviceProvider.GetRequiredService<ISecureStorageService>();
        _authService = _serviceProvider.GetRequiredService<AuthenticationService>();
        _playbackService = _serviceProvider.GetRequiredService<PlaybackService>();
    }

    [Fact]
    public async Task AuthenticateAndGetPlaybackInfo_WithGuestCredentials_ShouldReturnPlaybackInfo()
    {
        // Arrange
        var logger = _serviceProvider.GetRequiredService<ILogger<PlaybackIntegrationTests>>();
        logger.LogInformation("Starting authentication test with {Username}", TestUsername);

        // Act - Authenticate
        var authResult = await _authService.AuthenticateAsync(
            TestServerUrl,
            TestUsername,
            TestPassword,
            CancellationToken.None
        );

        // Assert - Authentication
        Assert.NotNull(authResult);
        Assert.NotNull(authResult.AccessToken);
        Assert.NotNull(authResult.UserId);
        logger.LogInformation(
            "Authentication successful - UserId: {UserId}, Token: {Token}",
            authResult.UserId,
            authResult.AccessToken.Substring(0, Math.Min(20, authResult.AccessToken.Length)) + "..."
        );

        // Act - Get Playback Info
        logger.LogInformation("Fetching playback info for movie {MovieId}", TestMovieId);
        var playbackInfo = await _playbackService.GetPlaybackInfoAsync(
            TestMovieId,
            CancellationToken.None
        );

        // Assert - Playback Info
        Assert.NotNull(playbackInfo);
        Assert.NotNull(playbackInfo.StreamUrl);
        Assert.NotEmpty(playbackInfo.StreamUrl);

        logger.LogInformation("Playback info received:");
        logger.LogInformation("  StreamUrl: {StreamUrl}", playbackInfo.StreamUrl);
        logger.LogInformation("  SessionId: {SessionId}", playbackInfo.SessionId);
        logger.LogInformation("  MediaSourceId: {MediaSourceId}", playbackInfo.MediaSourceId);
        logger.LogInformation("  CanSeek: {CanSeek}", playbackInfo.CanSeek);
        logger.LogInformation("  SubtitleTracks: {Count}", playbackInfo.SubtitleTracks.Count);
        logger.LogInformation("  AudioTracks: {Count}", playbackInfo.AudioTracks.Count);

        // Additional assertions
        Assert.Equal(TestMovieId, playbackInfo.ItemId);
        Assert.NotNull(playbackInfo.SessionId);
        Assert.NotEmpty(playbackInfo.SessionId);

        // Act - Verify stream URL is downloadable
        logger.LogInformation("Testing if stream URL is downloadable...");
        var httpClientFactory = _serviceProvider.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, playbackInfo.StreamUrl);
        request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(0, 1023); // Request first 1KB

        var response = await httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            CancellationToken.None
        );

        // Assert - Stream is accessible
        Assert.True(
            response.IsSuccessStatusCode,
            $"Stream URL returned {response.StatusCode}: {await response.Content.ReadAsStringAsync()}"
        );

        var contentBytes = await response.Content.ReadAsByteArrayAsync();
        Assert.NotNull(contentBytes);
        Assert.True(contentBytes.Length > 0, "No bytes were downloaded from stream URL");

        logger.LogInformation(
            "Successfully downloaded {ByteCount} bytes from stream URL",
            contentBytes.Length
        );
        logger.LogInformation(
            "First 16 bytes (hex): {Bytes}",
            BitConverter.ToString(contentBytes.Take(16).ToArray())
        );

        // Verify it looks like a valid video file (common video file signatures)
        var isValidVideoFile =
            IsMatroskaFile(contentBytes)
            || IsMp4File(contentBytes)
            || IsAviFile(contentBytes)
            || IsWebMFile(contentBytes);

        Assert.True(
            isValidVideoFile,
            $"Downloaded content doesn't appear to be a valid video file. First bytes: {BitConverter.ToString(contentBytes.Take(16).ToArray())}"
        );

        logger.LogInformation("Stream URL verification complete - valid video file detected");
    }

    // Helper methods to check file signatures
    private static bool IsMatroskaFile(byte[] bytes)
    {
        // MKV/WebM starts with EBML header: 0x1A 0x45 0xDF 0xA3
        return bytes.Length >= 4
            && bytes[0] == 0x1A
            && bytes[1] == 0x45
            && bytes[2] == 0xDF
            && bytes[3] == 0xA3;
    }

    private static bool IsMp4File(byte[] bytes)
    {
        // MP4 has 'ftyp' at bytes 4-7
        return bytes.Length >= 8
            && bytes[4] == 0x66
            && bytes[5] == 0x74
            && bytes[6] == 0x79
            && bytes[7] == 0x70; // 'ftyp'
    }

    private static bool IsAviFile(byte[] bytes)
    {
        // AVI starts with 'RIFF' and has 'AVI ' at bytes 8-11
        return bytes.Length >= 12
            && bytes[0] == 0x52
            && bytes[1] == 0x49
            && bytes[2] == 0x46
            && bytes[3] == 0x46 // 'RIFF'
            && bytes[8] == 0x41
            && bytes[9] == 0x56
            && bytes[10] == 0x49
            && bytes[11] == 0x20; // 'AVI '
    }

    private static bool IsWebMFile(byte[] bytes)
    {
        // WebM is Matroska-based, so same EBML header
        return IsMatroskaFile(bytes);
    }

    [Fact]
    public async Task GetPlaybackInfo_WithoutAuthentication_ShouldReturnNull()
    {
        // Arrange - No authentication

        // Act
        var playbackInfo = await _playbackService.GetPlaybackInfoAsync(
            TestMovieId,
            CancellationToken.None
        );

        // Assert
        Assert.Null(playbackInfo);
    }
}

/// <summary>
/// In-memory implementation of ISecureStorageService for testing.
/// </summary>
public class InMemorySecureStorage : ISecureStorageService
{
    private readonly Dictionary<string, string> _storage = new();

    public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        _storage.TryGetValue(key, out var value);
        return Task.FromResult(value);
    }

    public Task SetAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        _storage[key] = value;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _storage.Remove(key);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_storage.ContainsKey(key));
    }
}

/// <summary>
/// In-memory implementation of IStorageService for testing.
/// </summary>
public class InMemoryStorageService : IStorageService
{
    private readonly Dictionary<string, object> _cache = new();

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        where T : class
    {
        _cache.TryGetValue(key, out var value);
        return Task.FromResult(value as T);
    }

    public Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default
    )
        where T : class
    {
        _cache[key] = value;
        return Task.CompletedTask;
    }

    public Task<bool> TryGetAsync<T>(string key, CancellationToken cancellationToken = default)
        where T : class
    {
        return Task.FromResult(_cache.ContainsKey(key));
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _cache.Remove(key);
        return Task.CompletedTask;
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        _cache.Clear();
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_cache.ContainsKey(key));
    }
}
