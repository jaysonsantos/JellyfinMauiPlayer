using System.Globalization;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JellyfinPlayer.Lib.Models;
using JellyfinPlayer.Lib.Services;
using Microsoft.Extensions.Logging;

namespace Player.ViewModels;

[QueryProperty(nameof(ItemId), "ItemId")]
[QueryProperty(nameof(ItemName), "ItemName")]
public sealed partial class VideoPlayerViewModel(
    IPlaybackService playbackService,
    ILogger<VideoPlayerViewModel> logger
) : ObservableObject
{
    [ObservableProperty]
    public partial string ItemId { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ItemName { get; set; } = "Video Player";

    [ObservableProperty]
    public partial string VideoUrl { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial bool IsPlaying { get; set; }

    [ObservableProperty]
    public partial bool ShowCustomControls { get; set; }

    [ObservableProperty]
    public partial string CurrentPositionText { get; set; } = "00:00";

    [ObservableProperty]
    public partial string DurationText { get; set; } = "00:00";

    private TimeSpan _currentPosition;
    private TimeSpan _duration;
    private PlaybackInfo? _playbackInfo;
    private bool _hasReportedStart;
    private DateTime _lastProgressReport = DateTime.MinValue;
    private const int ProgressReportIntervalSeconds = 10;

    public void HandleMediaStateChanged(MediaElementState newState)
    {
        logger.LogInformation("Media state changed to: {State}", newState);

        IsPlaying = newState == MediaElementState.Playing;
        IsLoading =
            newState == MediaElementState.Opening || newState == MediaElementState.Buffering;

        if (newState == MediaElementState.Failed)
        {
            ErrorMessage =
                "Failed to load video. Please check your network connection and try again.";
            logger.LogError("Media playback failed");
        }
        else
        {
            ErrorMessage = null;
        }

        // Report playback start when playing begins
        if (
            newState == MediaElementState.Playing
            && !_hasReportedStart
            && _playbackInfo is not null
        )
        {
            _hasReportedStart = true;
            _ = ReportPlaybackStartAsync();
        }
    }

    public void HandlePositionChanged(TimeSpan position)
    {
        _currentPosition = position;
        CurrentPositionText = FormatTimeSpan(position);

        // Report progress periodically
        var now = DateTime.UtcNow;
        if (
            _playbackInfo is not null
            && (now - _lastProgressReport).TotalSeconds >= ProgressReportIntervalSeconds
        )
        {
            _lastProgressReport = now;
            _ = ReportPlaybackProgressAsync();
        }
    }

    public void UpdateDuration(TimeSpan duration)
    {
        _duration = duration;
        DurationText = FormatTimeSpan(duration);
    }

    private static string FormatTimeSpan(TimeSpan timeSpan)
    {
        if (timeSpan.TotalHours >= 1)
        {
            return timeSpan.ToString(@"h\:mm\:ss", CultureInfo.InvariantCulture);
        }
        return timeSpan.ToString(@"mm\:ss", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Loads playback information and prepares the video URL
    /// </summary>
    [RelayCommand]
    private async Task LoadPlaybackInfoAsync()
    {
        if (string.IsNullOrWhiteSpace(ItemId))
            return;

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            _playbackInfo = await playbackService
                .GetPlaybackInfoAsync(ItemId, CancellationToken.None)
                .ConfigureAwait(false);

            if (_playbackInfo is null)
            {
                ErrorMessage = "Failed to get playback information from server.";
                logger.LogError("Could not get playback info for item {ItemId}", ItemId);
                return;
            }

            // Set the video URL - the stream URL from playback info should be a full URL
            VideoUrl = _playbackInfo.StreamUrl;
            logger.LogInformation(
                "Loaded playback info for item {ItemId}, URL: {Url}",
                ItemId,
                VideoUrl
            );

            // Try to load previous playback state for resume functionality
            var previousState = await playbackService
                .GetPlaybackStateAsync(ItemId, CancellationToken.None)
                .ConfigureAwait(false);

            if (previousState is not null && previousState.PositionTicks > 0)
            {
                // Will be used to seek when MediaElement is ready
                logger.LogInformation(
                    "Found previous playback position: {Ticks} ticks",
                    previousState.PositionTicks
                );
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load playback info for item {ItemId}", ItemId);
            ErrorMessage = $"Failed to load playback information: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ReportPlaybackStartAsync()
    {
        if (_playbackInfo is null)
            return;

        try
        {
            await playbackService
                .ReportPlaybackStartAsync(
                    _playbackInfo.ItemId,
                    _playbackInfo.SessionId,
                    CancellationToken.None
                )
                .ConfigureAwait(false);
            logger.LogInformation(
                "Reported playback start for item {ItemId}",
                _playbackInfo.ItemId
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to report playback start");
        }
    }

    private async Task ReportPlaybackProgressAsync()
    {
        if (_playbackInfo is null)
            return;

        try
        {
            var positionTicks = _currentPosition.Ticks;
            await playbackService
                .ReportPlaybackProgressAsync(
                    _playbackInfo.ItemId,
                    _playbackInfo.SessionId,
                    positionTicks,
                    !IsPlaying,
                    CancellationToken.None
                )
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to report playback progress");
        }
    }

    private async Task ReportPlaybackStoppedAsync()
    {
        if (_playbackInfo is null)
            return;

        try
        {
            var positionTicks = _currentPosition.Ticks;
            await playbackService
                .ReportPlaybackStoppedAsync(
                    _playbackInfo.ItemId,
                    _playbackInfo.SessionId,
                    positionTicks,
                    CancellationToken.None
                )
                .ConfigureAwait(false);
            logger.LogInformation(
                "Reported playback stopped for item {ItemId} at position {Ticks}",
                _playbackInfo.ItemId,
                positionTicks
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to report playback stopped");
        }
    }

    [RelayCommand]
    private void TogglePlayPause()
    {
        // This will be handled by the MediaElement's built-in controls
        // or can be extended with custom logic
        logger.LogInformation("Toggle play/pause requested");
    }

    [RelayCommand]
    private async Task CloseAsync()
    {
        // Report playback stopped before closing
        await ReportPlaybackStoppedAsync();

        // Navigate back - must be on UI thread
        await Shell.Current.GoToAsync("..", true);
    }

    partial void OnItemIdChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            LoadPlaybackInfoCommand.Execute(null);
        }
    }
}
