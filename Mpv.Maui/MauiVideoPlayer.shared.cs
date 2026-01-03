using System.Globalization;
using Microsoft.Extensions.Logging;
using Mpv.Maui.Controls;
using Mpv.Sys;
using Mpv.Sys.Internal;

#if ANDROID
namespace Mpv.Maui.Platforms.Android;

#elif IOS || MACCATALYST
namespace Mpv.Maui.Platforms.MaciOS;

#elif WINDOWS
namespace Mpv.Maui.Platforms.Windows;

#endif

public sealed partial class MauiVideoPlayer
{
    private Video? _video;
    private readonly MpvClient _mpvClient;
    private readonly ILogger<MauiVideoPlayer> _logger;

    // Cached property values from mpv property change events
    private bool _idleActive;
    private bool _pause;
    private bool _pausedForCache;
    private bool _coreIdle;
    private bool _eofReached;
    private double _duration;
    private double _timePos;

    // Property observation configuration
    private static readonly (
        ObservedProperty Property,
        string Name,
        MpvFormat Format
    )[] ObservedProperties =
    [
        (ObservedProperty.IdleActive, MpvPropertyNames.IdleActive, MpvFormat.Flag),
        (ObservedProperty.Pause, MpvPropertyNames.Pause, MpvFormat.Flag),
        (ObservedProperty.PausedForCache, MpvPropertyNames.PausedForCache, MpvFormat.Flag),
        (ObservedProperty.CoreIdle, MpvPropertyNames.CoreIdle, MpvFormat.Flag),
        (ObservedProperty.EofReached, MpvPropertyNames.EofReached, MpvFormat.Flag),
        (ObservedProperty.Duration, MpvPropertyNames.Duration, MpvFormat.Double),
        (ObservedProperty.TimePos, MpvPropertyNames.TimePos, MpvFormat.Double),
    ];

    /// <summary>
    /// Gets or sets the Video control this platform view is rendering.
    /// This is set during construction and should match the VirtualView from the handler.
    /// </summary>
    public Video? Video
    {
        get => _video;
        set => _video = value;
    }

    private void LogLines(object? sender, MpvLogMessage e)
    {
        // Map mpv log levels to appropriate .NET logging levels
        switch (e.LogLevel)
        {
            case MpvLogLevel.Fatal:
                _logger.LogCritical("[{Prefix}] {Text}", e.Prefix, e.Text);
                break;
            case MpvLogLevel.Error:
                _logger.LogError("[{Prefix}] {Text}", e.Prefix, e.Text);
                break;
            case MpvLogLevel.Warn:
                _logger.LogWarning("[{Prefix}] {Text}", e.Prefix, e.Text);
                break;
            case MpvLogLevel.Info:
                _logger.LogInformation("[{Prefix}] {Text}", e.Prefix, e.Text);
                break;
            case MpvLogLevel.Debug:
                _logger.LogDebug("[{Prefix}] {Text}", e.Prefix, e.Text);
                break;
            case MpvLogLevel.None:
            case MpvLogLevel.Trace:
            case MpvLogLevel.V:
            default:
                _logger.LogTrace("[{Prefix}] {Text}", e.Prefix, e.Text);
                break;
        }
    }

    private void OnMpvPropertyChange(object? sender, MpvPropertyChangeEventArgs e)
    {
        // Cache the property value from the event data
        switch (e.Property)
        {
            case ObservedProperty.IdleActive:
                _idleActive = e.UnwrapBool();
                UpdateStatus();
                break;

            case ObservedProperty.Pause:
                _pause = e.UnwrapBool();
                UpdateStatus();
                break;

            case ObservedProperty.PausedForCache:
                _pausedForCache = e.UnwrapBool();
                UpdateStatus();
                break;

            case ObservedProperty.CoreIdle:
                _coreIdle = e.UnwrapBool();
                UpdateStatus();
                break;

            case ObservedProperty.EofReached:
                _eofReached = e.UnwrapBool();
                UpdateStatus();
                break;

            case ObservedProperty.Duration:
                _duration = e.UnwrapDouble();
                // Always sync duration when it changes
                SyncDurationFromMpv();
                break;

            case ObservedProperty.TimePos:
                _timePos = e.UnwrapDouble();
                // Only sync position when actually playing
                if (_video != null && ((IVideoController)_video).Status == VideoStatus.Playing)
                {
                    SyncPositionFromMpv();
                }
                break;
        }
    }

    private void InitializeMpvCommon()
    {
        _mpvClient.OnLog += LogLines;
        _mpvClient.OnPropertyChange += OnMpvPropertyChange;

        _mpvClient.SetOption(MpvPropertyNames.InputMediaKeys, "yes");

        // Observe all properties
        foreach (var (property, name, format) in ObservedProperties)
        {
            _mpvClient.ObserveProperty((ulong)property, name, format);
        }
    }

    private void UninitializeMpvCommon()
    {
        _mpvClient.Command("stop");
        // Unobserve all properties
        foreach (var (property, _, _) in ObservedProperties)
        {
            _mpvClient.UnobserveProperty((ulong)property);
        }
    }

    public void UpdateStatus()
    {
        if (_video == null)
            return;

        VideoStatus status = VideoStatus.NotReady;

        // Use cached property values from mpv events - no need to query
        if (_idleActive && !_eofReached)
        {
            status = VideoStatus.Opening;
        }
        else if (_pausedForCache)
        {
            status = VideoStatus.Buffering;
        }
        else if (_pause || _coreIdle)
        {
            status = VideoStatus.Paused;
        }
        else if (!_idleActive)
        {
            status = VideoStatus.Playing;
        }
        else if (_eofReached)
        {
            status = VideoStatus.Paused; // Treat EOF as paused/finished
        }

        ((IVideoController)_video).Status = status;
    }

    public void UpdateSource(Video? video = null)
    {
        // Use the passed video parameter if provided, otherwise fall back to _video
        var sourceVideo = video ?? _video;

        if (sourceVideo == null)
        {
            _logger.LogWarning("UpdateSource called but video is null");
            return;
        }

        if (sourceVideo.Source is UriVideoSource uriSource)
        {
            string uri = uriSource.Uri;
            if (!string.IsNullOrWhiteSpace(uri))
            {
                _logger.LogInformation("Loading video from URI: {Uri}", uri);
                _mpvClient.Command("loadfile", uri);
            }
            else
            {
                _logger.LogWarning("UpdateSource called with UriVideoSource but URI is empty");
            }
        }
        else
        {
            _logger.LogInformation(
                "UpdateSource called with non-URI source, using platform-specific handler"
            );
            UpdateSourcePlatform();
        }
    }

    private partial void UpdateSourcePlatform();

    public void UpdateTransportControlsEnabled() { }

    private bool _isSyncingPosition;

    public void UpdatePosition(Video? video = null)
    {
        // Use the passed video parameter if provided, otherwise fall back to _video
        var sourceVideo = video ?? _video;

        if (_isSyncingPosition || sourceVideo == null)
            return;

        // If total seconds is 0, we can skip the seek command
        // This avoids issues during initial load where position might be 0
        if (sourceVideo.Position.TotalSeconds == 0)
            return;

        _mpvClient.Command(
            "seek",
            sourceVideo.Position.TotalSeconds.ToString(CultureInfo.InvariantCulture),
            "absolute"
        );
    }

    private void SyncDurationFromMpv()
    {
        if (_video == null)
            return;

        var duration = _duration;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            ((IVideoController)_video).Duration = TimeSpan.FromSeconds(duration);
        });
    }

    private void SyncPositionFromMpv()
    {
        if (_video == null)
            return;

        // Only sync position when actually playing
        if (((IVideoController)_video).Status != VideoStatus.Playing)
            return;

        // Use cached values from mpv events - no need to query
        var duration = _duration;
        var position = _timePos;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            _isSyncingPosition = true;
            try
            {
                ((IVideoController)_video).Duration = TimeSpan.FromSeconds(duration);
                _video.Position = TimeSpan.FromSeconds(position);
            }
            finally
            {
                _isSyncingPosition = false;
            }
        });
    }

    public void UpdateIsLooping(Video? video = null)
    {
        // Use the passed video parameter if provided, otherwise fall back to _video
        var sourceVideo = video ?? _video;

        _mpvClient.Command(
            "set",
            MpvPropertyNames.LoopFile,
            (sourceVideo?.IsLooping ?? false) ? "inf" : "no"
        );
    }

    private void DisposeMpvCommon()
    {
        // First unobserve all properties
        UninitializeMpvCommon();

        // Then unsubscribe from events
        _mpvClient.OnLog -= LogLines;
        _mpvClient.OnPropertyChange -= OnMpvPropertyChange;
    }

    public void PlayRequested(TimeSpan? position)
    {
        if (position != null)
            _mpvClient.Command(
                "seek",
                position.Value.TotalSeconds.ToString(CultureInfo.InvariantCulture),
                "absolute"
            );
        _mpvClient.SetOption(MpvPropertyNames.Pause, "no");
        _logger.LogDebug("Video playback from {Position}", position);
    }

    public void PauseRequested(TimeSpan position)
    {
        _mpvClient.SetOption(MpvPropertyNames.Pause, "yes");
        _logger.LogDebug("Video paused at {Position}", position);
    }

    public void StopRequested(TimeSpan position)
    {
        _mpvClient.Command("stop");
        _logger.LogDebug("Video stopped at {Position}", position);
    }

    public void UpdateSize()
    {
        UpdateSizePlatform();
    }

    private partial void UpdateSizePlatform();

    public void SeekRequested(TimeSpan position)
    {
        _mpvClient.Command(
            "seek",
            position.TotalSeconds.ToString(CultureInfo.InvariantCulture),
            "absolute"
        );
        _logger.LogDebug("Seek requested to {Position}", position);
    }
}
