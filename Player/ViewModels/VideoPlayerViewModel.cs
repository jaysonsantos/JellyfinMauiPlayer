using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JellyfinPlayer.Lib.Models;
using JellyfinPlayer.Lib.Services;
using Microsoft.Extensions.Logging;
using Mpv.Maui.Controls;
using Player.Models;

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
    public partial VideoSource? VideoSource { get; set; }

    partial void OnVideoUrlChanged(string value)
    {
        logger.LogInformation("[VideoPlayerViewModel] VideoUrl changed to: {Url}", value);

        // Convert string URL to VideoSource when URL changes
        if (!string.IsNullOrWhiteSpace(value))
        {
            VideoSource = VideoSource.FromUri(value);
            logger.LogInformation("[VideoPlayerViewModel] VideoSource created from URL");
        }
        else
        {
            VideoSource = null;
            logger.LogInformation("[VideoPlayerViewModel] VideoSource set to null (empty URL)");
        }
    }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial bool IsPlaying { get; set; }

    [ObservableProperty]
    public partial double CurrentPositionSeconds { get; set; }

    [ObservableProperty]
    public partial double DurationSeconds { get; set; } = 1.0;

    [ObservableProperty]
    public partial string CurrentPositionText { get; set; } = "00:00";

    [ObservableProperty]
    public partial string DurationText { get; set; } = "00:00";

    [ObservableProperty]
    public partial IReadOnlyList<TrackInfo> AudioTracks { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<TrackInfo> SubtitleTracks { get; set; } = [];

    [ObservableProperty]
    public partial int? CurrentAudioTrackId { get; set; }

    [ObservableProperty]
    public partial int? CurrentSubtitleTrackId { get; set; }

    [ObservableProperty]
    public partial bool ShowAudioTrackSelector { get; set; }

    [ObservableProperty]
    public partial bool ShowSubtitleTrackSelector { get; set; }

    public event EventHandler<TrackSelectedEventArgs>? AudioTrackSelected;
    public event EventHandler<TrackSelectedEventArgs>? SubtitleTrackSelected;

    private TimeSpan _currentPosition;
    private TimeSpan _duration;
    private PlaybackInfo? _playbackInfo;
    private bool _hasReportedStart;
    private DateTime _lastProgressReport = DateTime.MinValue;
    private const int ProgressReportIntervalSeconds = 10;
    private const long MinResumeThresholdTicks = 300_000_000; // 30 seconds in ticks

    public event EventHandler<TimeSpan>? ResumePromptRequested;

    public void HandleMediaStateChanged(VideoStatus newState)
    {
        logger.LogInformation("Media state changed to: {State}", newState);

        IsPlaying = newState == VideoStatus.Playing;
        IsLoading = newState == VideoStatus.Opening || newState == VideoStatus.Buffering;

        if (newState == VideoStatus.Failed)
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
        if (newState == VideoStatus.Playing && !_hasReportedStart && _playbackInfo is not null)
        {
            _hasReportedStart = true;
            _ = ReportPlaybackStartAsync();
        }
    }

    public void HandlePositionChanged(TimeSpan position)
    {
        _currentPosition = position;
        CurrentPositionText = FormatTimeSpan(position);
        CurrentPositionSeconds = position.TotalSeconds;

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
        DurationSeconds = duration.TotalSeconds > 0 ? duration.TotalSeconds : 1.0;
    }

    private static string FormatTimeSpan(TimeSpan timeSpan)
    {
        if (timeSpan.TotalHours >= 1)
        {
            return timeSpan.ToString(@"h\:mm\:ss", CultureInfo.InvariantCulture);
        }
        return timeSpan.ToString(@"mm\:ss", CultureInfo.InvariantCulture);
    }

    public static string FormatTimeSpanForDisplay(TimeSpan timeSpan)
    {
        return FormatTimeSpan(timeSpan);
    }

    /// <summary>
    /// Loads playback information and prepares the video URL
    /// </summary>
    [RelayCommand]
    private async Task LoadPlaybackInfoAsync()
    {
        logger.LogInformation(
            "[VideoPlayerViewModel] LoadPlaybackInfoAsync started for ItemId: {ItemId}",
            ItemId
        );

        if (string.IsNullOrWhiteSpace(ItemId))
        {
            logger.LogWarning(
                "[VideoPlayerViewModel] ItemId is null or empty, cannot load playback info"
            );
            return;
        }

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            await LoadPlaybackDataAsync();
            await LoadPreviousPlaybackStateAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load playback info for item {ItemId}", ItemId);
            ErrorMessage = $"Failed to load playback information: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            logger.LogInformation("[VideoPlayerViewModel] LoadPlaybackInfoAsync completed");
        }
    }

    private async Task LoadPlaybackDataAsync()
    {
        logger.LogInformation("[VideoPlayerViewModel] Requesting playback info from service...");
        _playbackInfo = await playbackService
            .GetPlaybackInfoAsync(ItemId, CancellationToken.None)
            .ConfigureAwait(false);

        if (_playbackInfo is null)
        {
            ErrorMessage = "Failed to get playback information from server.";
            logger.LogError("Could not get playback info for item {ItemId}", ItemId);
            return;
        }

        logger.LogInformation(
            "[VideoPlayerViewModel] Setting VideoUrl to: {Url}",
            _playbackInfo.StreamUrl
        );
        VideoUrl = _playbackInfo.StreamUrl;
        logger.LogInformation(
            "[VideoPlayerViewModel] VideoUrl has been set. Current value: {Url}",
            VideoUrl
        );
    }

    private async Task LoadPreviousPlaybackStateAsync()
    {
        var previousState = await playbackService
            .GetPlaybackStateAsync(ItemId, CancellationToken.None)
            .ConfigureAwait(false);

        if (previousState is not null && previousState.PositionTicks > MinResumeThresholdTicks)
        {
            logger.LogInformation(
                "Found previous playback position: {Ticks} ticks ({Seconds} seconds)",
                previousState.PositionTicks,
                TimeSpan.FromTicks(previousState.PositionTicks).TotalSeconds
            );

            // Request resume prompt from the page
            var resumePosition = TimeSpan.FromTicks(previousState.PositionTicks);
            ResumePromptRequested?.Invoke(this, resumePosition);
        }
        else if (previousState is not null)
        {
            logger.LogInformation(
                "Previous position {Ticks} ticks is below resume threshold, starting from beginning",
                previousState.PositionTicks
            );
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

    [RelayCommand]
    private void ToggleAudioTrackSelector()
    {
        ShowAudioTrackSelector = !ShowAudioTrackSelector;
        if (ShowAudioTrackSelector)
        {
            ShowSubtitleTrackSelector = false;
        }
    }

    [RelayCommand]
    private void ToggleSubtitleTrackSelector()
    {
        ShowSubtitleTrackSelector = !ShowSubtitleTrackSelector;
        if (ShowSubtitleTrackSelector)
        {
            ShowAudioTrackSelector = false;
        }
    }

    [RelayCommand]
    private void SelectAudioTrack(int trackId)
    {
        CurrentAudioTrackId = trackId;
        ShowAudioTrackSelector = false;
        AudioTrackSelected?.Invoke(this, new TrackSelectedEventArgs(trackId));
        logger.LogInformation("Audio track selected: {TrackId}", trackId);
    }

    [RelayCommand]
    private void SelectSubtitleTrack(int trackId)
    {
        CurrentSubtitleTrackId = trackId;
        ShowSubtitleTrackSelector = false;
        SubtitleTrackSelected?.Invoke(this, new TrackSelectedEventArgs(trackId));
        logger.LogInformation("Subtitle track selected: {TrackId}", trackId);
    }

    public void LoadTracks(
        IReadOnlyList<Mpv.Sys.TrackInfo> audioTracks,
        IReadOnlyList<Mpv.Sys.TrackInfo> subtitleTracks
    )
    {
        ArgumentNullException.ThrowIfNull(audioTracks);
        ArgumentNullException.ThrowIfNull(subtitleTracks);

        // Convert MPV tracks to Player models
        AudioTracks = audioTracks.Select(TrackInfo.FromMpvTrackInfo).ToList();
        SubtitleTracks = subtitleTracks.Select(TrackInfo.FromMpvTrackInfo).ToList();

        logger.LogInformation(
            "Loaded {AudioCount} audio tracks and {SubtitleCount} subtitle tracks",
            AudioTracks.Count,
            SubtitleTracks.Count
        );
    }

    public void UpdateCurrentTracks(int? audioTrackId, int? subtitleTrackId)
    {
        CurrentAudioTrackId = audioTrackId;
        CurrentSubtitleTrackId = subtitleTrackId;
        logger.LogInformation(
            "Current tracks updated - Audio: {AudioId}, Subtitle: {SubtitleId}",
            audioTrackId,
            subtitleTrackId
        );
    }

    partial void OnItemIdChanged(string value)
    {
        logger.LogInformation(
            "[VideoPlayerViewModel] OnItemIdChanged called with: {ItemId}",
            value
        );
        if (!string.IsNullOrWhiteSpace(value))
        {
            logger.LogInformation("[VideoPlayerViewModel] Executing LoadPlaybackInfoCommand");
            LoadPlaybackInfoCommand.Execute(null);
        }
    }
}
