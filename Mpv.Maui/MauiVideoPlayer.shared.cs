using System;
using System.Globalization;
using System.Runtime.InteropServices;
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

public partial class MauiVideoPlayer
{
    private Video? _video;
    private readonly MpvClient _mpvClient;
    private readonly ILogger<MauiVideoPlayer> _logger;

    private void LogLines(object? sender, MpvLogMessage e)
    {
#if IOS || MACCATALYST
        Console.WriteLine($"[{e.Level}] {e.Prefix}: {e.Text}");
#endif
        _logger.LogWarning("{Level} {Prefix} {Text}", e.Level, e.Prefix, e.Text);
    }

    private void OnMpvPropertyChange(object? sender, MpvEventProperty e)
    {
        UpdateStatus();
        string name = Marshal.PtrToStringUTF8(e.Name) ?? "";
        if (name == "time-pos" || name == "duration")
        {
            SyncPositionFromMpv();
        }
    }

    private void InitializeMpvCommon()
    {
        _mpvClient.OnLog += LogLines;
        _mpvClient.OnPropertyChange += OnMpvPropertyChange;

        _mpvClient.SetOption("input-media-keys", "yes");

        _mpvClient.ObserveProperty(0, "idle-active", MpvFormat.Flag);
        _mpvClient.ObserveProperty(0, "pause", MpvFormat.Flag);
        _mpvClient.ObserveProperty(0, "paused-for-cache", MpvFormat.Flag);
        _mpvClient.ObserveProperty(0, "core-idle", MpvFormat.Flag);
        _mpvClient.ObserveProperty(0, "eof-reached", MpvFormat.Flag);
        _mpvClient.ObserveProperty(0, "duration", MpvFormat.Double);
        _mpvClient.ObserveProperty(0, "time-pos", MpvFormat.Double);
    }

    public void UpdateStatus()
    {
        VideoStatus status = VideoStatus.NotReady;

        bool idleActive = GetPropertyBool("idle-active");
        bool pause = GetPropertyBool("pause");
        bool pausedForCache = GetPropertyBool("paused-for-cache");
        bool coreIdle = GetPropertyBool("core-idle");
        bool eofReached = GetPropertyBool("eof-reached");

        if (idleActive && !eofReached)
        {
            status = VideoStatus.Opening;
        }
        else if (pausedForCache)
        {
            status = VideoStatus.Buffering;
        }
        else if (pause || coreIdle)
        {
            status = VideoStatus.Paused;
        }
        else if (!idleActive)
        {
            status = VideoStatus.Playing;
        }
        else if (eofReached)
        {
            status = VideoStatus.Paused; // Treat EOF as paused/finished
        }

        // TODO: Detect failure state

        if (_video != null)
        {
            ((IVideoController)_video).Status = status;
        }
    }

    private bool GetPropertyBool(string property)
    {
        try
        {
            var ptr = _mpvClient.GetPropertyPtr(property);
            if (ptr == IntPtr.Zero)
                return false;
            var result = Marshal.ReadInt64(ptr) != 0;
            Marshal.FreeHGlobal(ptr);
            return result;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private double GetPropertyDouble(string property)
    {
        try
        {
            var ptr = _mpvClient.GetPropertyPtr(property);
            if (ptr == IntPtr.Zero)
                return 0;
            var result = Marshal.PtrToStructure<double>(ptr);
            Marshal.FreeHGlobal(ptr);
            return result;
        }
        catch (Exception)
        {
            return 0;
        }
    }

    public void UpdateSource()
    {
        if (_video?.Source is UriVideoSource uriSource)
        {
            string uri = uriSource.Uri;
            if (!string.IsNullOrWhiteSpace(uri))
            {
                _mpvClient.Command("loadfile", uri);
            }
        }
        else
        {
            UpdateSourcePlatform();
        }
    }

    private partial void UpdateSourcePlatform();

    public void UpdateTransportControlsEnabled() { }

    private bool _isSyncingPosition;

    public void UpdatePosition()
    {
        if (_isSyncingPosition || _video == null)
            return;

        // If total seconds is 0, we can skip the seek command
        // This avoids issues during initial load where position might be 0
        if (_video.Position.TotalSeconds == 0)
            return;

        _mpvClient.Command(
            "seek",
            _video.Position.TotalSeconds.ToString(CultureInfo.InvariantCulture),
            "absolute"
        );
    }

    private void SyncPositionFromMpv()
    {
        return;
        if (_video == null)
            return;

        var duration = GetPropertyDouble("duration");
        var position = GetPropertyDouble("time-pos");

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

    public void UpdateIsLooping()
    {
        _mpvClient.Command("set", "loop-file", (_video?.IsLooping ?? false) ? "inf" : "no");
    }

    private void DisposeMpvCommon()
    {
        _mpvClient.OnLog -= LogLines;
        _mpvClient.OnPropertyChange -= OnMpvPropertyChange;
    }

    public void PlayRequested(TimeSpan position)
    {
        _mpvClient.Command(
            "seek",
            position.TotalSeconds.ToString(CultureInfo.InvariantCulture),
            "absolute"
        );
        _mpvClient.SetOption("pause", "no");
        _logger.LogDebug("Video playback from {Position}.", position);
    }

    public void PauseRequested(TimeSpan position)
    {
        _mpvClient.SetOption("pause", "yes");
        _logger.LogDebug("Video paused at {Position}.", position);
    }

    public void StopRequested(TimeSpan position)
    {
        _mpvClient.Command("stop");
        _logger.LogDebug("Video stopped at {Position}.", position);
    }
}
