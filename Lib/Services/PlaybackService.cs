using System.Globalization;
using JellyfinPlayer.Lib.Api;
using JellyfinPlayer.Lib.Api.Models;
using JellyfinPlayer.Lib.Models;
using JellyfinPlayer.Lib.Storage;

namespace JellyfinPlayer.Lib.Services;

public interface IPlaybackService
{
    Task<PlaybackInfo?> GetPlaybackInfoAsync(
        string itemId,
        CancellationToken cancellationToken = default
    );
    Task<bool> ReportPlaybackStartAsync(
        string itemId,
        string sessionId,
        CancellationToken cancellationToken = default
    );
    Task<bool> ReportPlaybackProgressAsync(
        string itemId,
        string sessionId,
        long positionTicks,
        bool isPaused,
        CancellationToken cancellationToken = default
    );
    Task<bool> ReportPlaybackStoppedAsync(
        string itemId,
        string sessionId,
        long positionTicks,
        CancellationToken cancellationToken = default
    );
    Task<PlaybackState?> GetPlaybackStateAsync(
        string itemId,
        CancellationToken cancellationToken = default
    );
    Task SavePlaybackStateAsync(PlaybackState state, CancellationToken cancellationToken = default);
}

public sealed class PlaybackService(
    JellyfinApiClientFactory apiClientFactory,
    ISecureStorageService secureStorage,
    IStorageService? cacheService,
    ILogger<PlaybackService> logger
) : IPlaybackService
{
    private const string UserIdKey = "jellyfin_user_id";
    private const string SessionIdKey = "jellyfin_session_id";
    private const string ServerUrlKey = "jellyfin_server_url";
    private const string AccessTokenKey = "jellyfin_access_token";

    public async Task<PlaybackInfo?> GetPlaybackInfoAsync(
        string itemId,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);

        var apiClient = await apiClientFactory
            .CreateClientAsync(cancellationToken)
            .ConfigureAwait(false);
        if (apiClient is null)
        {
            logger.LogWarning("Cannot get playback info: API client not available");
            return null;
        }

        var serverUrl = await secureStorage
            .GetAsync(ServerUrlKey, cancellationToken)
            .ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(serverUrl))
        {
            logger.LogWarning("Cannot get playback info: Server URL not available");
            return null;
        }

        var userId = await GetUserIdAsync(cancellationToken).ConfigureAwait(false);
        if (userId is null)
        {
            logger.LogWarning("Cannot get playback info: User ID not available");
            return null;
        }

        try
        {
            var result = await FetchPlaybackInfoFromApiAsync(
                    apiClient,
                    itemId,
                    userId,
                    cancellationToken
                )
                .ConfigureAwait(false);

            if (result is null)
                return null;

            return await BuildPlaybackInfoAsync(result, itemId, serverUrl, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get playback info for item {ItemId}", itemId);
            return null;
        }
    }

    private async Task<PlaybackInfoResponse?> FetchPlaybackInfoFromApiAsync(
        JellyfinApiClient apiClient,
        string itemId,
        string userId,
        CancellationToken cancellationToken
    )
    {
        var playbackInfoDto = new PlaybackInfoDto
        {
            UserId = userId,
            EnableDirectPlay = true,
            EnableDirectStream = true,
            EnableTranscoding = true,
            MaxStreamingBitrate = 120_000_000,
            DeviceProfile = DeviceProfileBuilder.CreateDefaultProfile(),
        };

        return await apiClient
            .Items[itemId]
            .PlaybackInfo.PostAsync(playbackInfoDto, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<PlaybackInfo?> BuildPlaybackInfoAsync(
        PlaybackInfoResponse result,
        string itemId,
        string serverUrl,
        CancellationToken cancellationToken
    )
    {
        var sessionId = await GetOrCreateSessionIdAsync(cancellationToken).ConfigureAwait(false);
        var mediaSource = result.MediaSources?.FirstOrDefault();

        if (mediaSource is null)
        {
            logger.LogWarning("No media source available for item {ItemId}", itemId);
            return null;
        }

        var accessToken = await secureStorage
            .GetAsync(AccessTokenKey, cancellationToken)
            .ConfigureAwait(false);

        var streamUrl = GetStreamUrl(mediaSource, itemId, serverUrl, accessToken);
        if (string.IsNullOrWhiteSpace(streamUrl))
        {
            logger.LogWarning("Could not determine stream URL for item {ItemId}", itemId);
            return null;
        }

        return new PlaybackInfo(StreamUrl: streamUrl, ItemId: itemId)
        {
            MediaSourceId = mediaSource.Id ?? string.Empty,
            SessionId = sessionId,
            SubtitleTracks = GetSubtitleTracks(mediaSource),
            AudioTracks = GetAudioTracks(mediaSource),
            CanSeek =
                mediaSource.SupportsDirectPlay == true || mediaSource.SupportsDirectStream == true,
            TotalTicks = mediaSource.RunTimeTicks,
        };
    }

    private static AudioTrack[] GetAudioTracks(MediaSourceInfo mediaSource)
    {
        return mediaSource
                .MediaStreams?.Where(stream => stream.Type == MediaStream_Type.Audio)
                .Select(
                    (stream, index) =>
                        new AudioTrack(
                            Index: index,
                            Language: stream.Language,
                            DisplayTitle: stream.DisplayTitle
                        )
                        {
                            Id =
                                stream.Index?.ToString(CultureInfo.CurrentCulture)
                                ?? index.ToString(CultureInfo.CurrentCulture),
                            Channels = stream.Channels,
                            SampleRate = stream.SampleRate,
                            BitRate = stream.BitRate,
                        }
                )
                .ToArray()
            ?? [];
    }

    private static SubtitleTrack[] GetSubtitleTracks(MediaSourceInfo mediaSource)
    {
        return mediaSource
                .MediaStreams?.Where(stream => stream.Type == MediaStream_Type.Subtitle)
                .Select(
                    (stream, index) =>
                        new SubtitleTrack(
                            Index: index,
                            Language: stream.Language,
                            DisplayTitle: stream.DisplayTitle
                        )
                        {
                            Id =
                                stream.Index?.ToString(CultureInfo.CurrentCulture)
                                ?? index.ToString((CultureInfo.CurrentCulture)),
                            IsDefault = stream.IsDefault ?? false,
                            IsForced = stream.IsForced ?? false,
                        }
                )
                .ToArray()
            ?? [];
    }

    public async Task<bool> ReportPlaybackStartAsync(
        string itemId,
        string sessionId,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        var apiClient = await apiClientFactory
            .CreateClientAsync(cancellationToken)
            .ConfigureAwait(false);
        if (apiClient is null)
        {
            logger.LogWarning("Cannot report playback start: API client not available");
            return false;
        }

        try
        {
            var playbackStartInfo = new PlaybackStartInfo
            {
                ItemId = itemId,
                PlayMethod = PlaybackStartInfo_PlayMethod.DirectPlay,
            };

            await apiClient
                .Sessions.Playing.PostAsync(playbackStartInfo, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            logger.LogDebug(
                "Reported playback start for item {ItemId} in session {SessionId}",
                itemId,
                sessionId
            );
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to report playback start for item {ItemId}", itemId);
            return false;
        }
    }

    public async Task<bool> ReportPlaybackProgressAsync(
        string itemId,
        string sessionId,
        long positionTicks,
        bool isPaused,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        var apiClient = await apiClientFactory
            .CreateClientAsync(cancellationToken)
            .ConfigureAwait(false);
        if (apiClient is null)
        {
            logger.LogWarning("Cannot report playback progress: API client not available");
            return false;
        }

        try
        {
            var progressInfo = new PlaybackProgressInfo
            {
                ItemId = itemId,
                PositionTicks = positionTicks,
                IsPaused = isPaused,
                IsMuted = false,
                PlayMethod = PlaybackProgressInfo_PlayMethod.DirectPlay,
            };

            await apiClient
                .Sessions.Playing.Progress.PostAsync(
                    progressInfo,
                    cancellationToken: cancellationToken
                )
                .ConfigureAwait(false);

            // Also save state locally for resume functionality
            var state = new PlaybackState(
                ItemId: itemId,
                PositionTicks: positionTicks,
                IsPaused: isPaused
            )
            {
                SessionId = sessionId,
                LastUpdated = DateTime.UtcNow,
            };
            await SavePlaybackStateAsync(state, cancellationToken).ConfigureAwait(false);

            logger.LogDebug(
                "Reported playback progress for item {ItemId}: {PositionTicks} ticks, Paused: {IsPaused}",
                itemId,
                positionTicks,
                isPaused
            );
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to report playback progress for item {ItemId}", itemId);
            return false;
        }
    }

    public async Task<bool> ReportPlaybackStoppedAsync(
        string itemId,
        string sessionId,
        long positionTicks,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        var apiClient = await apiClientFactory
            .CreateClientAsync(cancellationToken)
            .ConfigureAwait(false);
        if (apiClient is null)
        {
            logger.LogWarning("Cannot report playback stopped: API client not available");
            return false;
        }

        try
        {
            var stopInfo = new PlaybackStopInfo { ItemId = itemId, PositionTicks = positionTicks };

            await apiClient
                .Sessions.Playing.Stopped.PostAsync(stopInfo, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            // Save final state
            var state = new PlaybackState(
                ItemId: itemId,
                PositionTicks: positionTicks,
                IsPaused: true
            )
            {
                SessionId = sessionId,
                LastUpdated = DateTime.UtcNow,
            };
            await SavePlaybackStateAsync(state, cancellationToken).ConfigureAwait(false);

            logger.LogDebug(
                "Reported playback stopped for item {ItemId} at position {PositionTicks}",
                itemId,
                positionTicks
            );
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to report playback stopped for item {ItemId}", itemId);
            return false;
        }
    }

    public async Task<PlaybackState?> GetPlaybackStateAsync(
        string itemId,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);

        if (cacheService is null)
        {
            return null;
        }

        try
        {
            var cacheKey = $"playback_state:{itemId}";
            var state = await cacheService
                .GetAsync<PlaybackState>(cacheKey, cancellationToken)
                .ConfigureAwait(false);
            return state;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get playback state for item {ItemId}", itemId);
            return null;
        }
    }

    public async Task SavePlaybackStateAsync(
        PlaybackState state,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(state);

        if (cacheService is null)
        {
            return;
        }

        try
        {
            var cacheKey = $"playback_state:{state.ItemId}";
            // Save playback state for 30 days
            await cacheService
                .SetAsync(cacheKey, state, TimeSpan.FromDays(30), cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save playback state for item {ItemId}", state.ItemId);
        }
    }

    private async Task<string?> GetUserIdAsync(CancellationToken cancellationToken = default)
    {
        return await secureStorage.GetAsync(UserIdKey, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> GetOrCreateSessionIdAsync(
        CancellationToken cancellationToken = default
    )
    {
        var existingSessionId = await secureStorage
            .GetAsync(SessionIdKey, cancellationToken)
            .ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(existingSessionId))
        {
            return existingSessionId;
        }

        // Generate a new session ID
        var newSessionId = Guid.NewGuid().ToString("N");
        await secureStorage
            .SetAsync(SessionIdKey, newSessionId, cancellationToken)
            .ConfigureAwait(false);
        return newSessionId;
    }

    private string? GetStreamUrl(
        MediaSourceInfo mediaSource,
        string itemId,
        string serverUrl,
        string? accessToken
    )
    {
        if (string.IsNullOrWhiteSpace(serverUrl))
        {
            return null;
        }

        var mediaStream = mediaSource.MediaStreams?.FirstOrDefault(stream =>
            stream.Type == MediaStream_Type.Video
        );
        var videoDeliveryUrl = mediaStream?.DeliveryUrl;

        logger.LogDebug(
            "GetStreamUrl - ItemId: {ItemId}, DeliveryUrl: {DeliveryUrl}, TranscodingUrl: {TranscodingUrl}, Path: {Path}, SupportsDirectPlay: {SupportsDirectPlay}",
            itemId,
            videoDeliveryUrl ?? "null",
            mediaSource.TranscodingUrl ?? "null",
            mediaSource.Path ?? "null",
            mediaSource.SupportsDirectPlay
        );

        if (!string.IsNullOrWhiteSpace(videoDeliveryUrl))
        {
            return BuildAbsoluteUrl(serverUrl, videoDeliveryUrl);
        }

        if (!string.IsNullOrWhiteSpace(mediaSource.TranscodingUrl))
        {
            return BuildAbsoluteUrl(serverUrl, mediaSource.TranscodingUrl);
        }

        if (
            mediaSource.SupportsDirectPlay == true
            && !string.IsNullOrWhiteSpace(mediaSource.Path)
            && Uri.TryCreate(mediaSource.Path, UriKind.Absolute, out var absolutePath)
            && (
                string.Equals(absolutePath.Scheme, Uri.UriSchemeHttp, StringComparison.Ordinal)
                || string.Equals(absolutePath.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal)
            )
        )
        {
            return absolutePath.ToString();
        }

        if (string.IsNullOrWhiteSpace(mediaSource.Id))
        {
            return null;
        }

        // Build download URL with API key for players that need it
        var downloadPath = $"/Items/{itemId}/Download?MediaSourceId={mediaSource.Id}";
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            downloadPath += $"&api_key={Uri.EscapeDataString(accessToken)}";
        }

        return BuildAbsoluteUrl(serverUrl, downloadPath);
    }

    private static string? BuildAbsoluteUrl(string serverUrl, string pathOrUrl)
    {
        if (string.IsNullOrWhiteSpace(pathOrUrl))
        {
            return null;
        }

        // Check if already an absolute HTTP/HTTPS URL (not file:// or other schemes)
        if (Uri.TryCreate(pathOrUrl, UriKind.Absolute, out var absoluteUri))
        {
            // Only accept HTTP/HTTPS absolute URLs, treat everything else as relative paths
            if (
                string.Equals(absoluteUri.Scheme, Uri.UriSchemeHttp, StringComparison.Ordinal)
                || string.Equals(absoluteUri.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal)
            )
            {
                return absoluteUri.ToString();
            }
        }

        // Treat as relative path - combine with server URL
        if (!Uri.TryCreate(serverUrl, UriKind.Absolute, out var baseUri))
        {
            return null;
        }

        if (Uri.TryCreate(baseUri, pathOrUrl.TrimStart('/'), out var combinedUri))
        {
            return combinedUri.ToString();
        }

        return null;
    }
}
