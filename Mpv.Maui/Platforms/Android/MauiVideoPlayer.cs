using Android.Content;
using Android.Media;
using Android.Views;
using Android.Widget;
using AndroidX.CoordinatorLayout.Widget;
using Microsoft.Extensions.Logging;
using Mpv.Maui.Controls;
using Mpv.Sys;
using Mpv.Sys.Internal;
using Color = Android.Graphics.Color;
using Uri = Android.Net.Uri;

namespace Mpv.Maui.Platforms.Android;

public class MauiVideoPlayer : CoordinatorLayout, MediaPlayer.IOnPreparedListener
{
    VideoView _videoView;
    MediaController _mediaController;
    bool _isPrepared;
    Context _context;
    Video _video;
    private readonly MpvClient _mpvClient;
    private readonly ILogger<MauiVideoPlayer> _logger;

    public MauiVideoPlayer(
        Context context,
        Video video,
        MpvClient mpvClient,
        ILogger<MauiVideoPlayer> logger
    )
        : base(context)
    {
        _context = context;
        _video = video;
        _mpvClient = mpvClient;
        _logger = logger;

        _mpvClient.OnLog += LogLines;

        // Initialize mpv client
        _mpvClient.SetOption("input-media-keys", "yes");

        _mpvClient.SetOption("vo", "gpu-next");
        _mpvClient.SetOption("hwdec", "auto");
        _mpvClient.SetOption("gpu-context", "android");

        // _mpvClient.SetOption("wid", 4, ptr);
        _mpvClient.Initialize();

        SetBackgroundColor(Color.Black);

        // Create a RelativeLayout for sizing the video
        RelativeLayout relativeLayout = new RelativeLayout(_context)
        {
            LayoutParameters = new LayoutParams(LayoutParams.MatchParent, LayoutParams.MatchParent)
            {
                Gravity = (int)GravityFlags.Center,
            },
        };

        // Create a VideoView and position it in the RelativeLayout
        _videoView = new VideoView(context)
        {
            LayoutParameters = new RelativeLayout.LayoutParams(
                LayoutParams.MatchParent,
                LayoutParams.MatchParent
            ),
        };

        // Add to the layouts
        relativeLayout.AddView(_videoView);
        AddView(relativeLayout);

        // Handle events
        _videoView.Prepared += OnVideoViewPrepared;
    }

    private void LogLines(object? sender, MpvLogMessage e)
    {
        _logger.LogWarning("{Level} {Prefix} {Text}", e.Level, e.Prefix, e.Text);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _videoView.Prepared -= OnVideoViewPrepared;
            _videoView.Dispose();
            _videoView = null;
            _video = null;
        }

        base.Dispose(disposing);
    }

    public void UpdateTransportControlsEnabled()
    {
        if (_video.AreTransportControlsEnabled)
        {
            _mediaController = new MediaController(_context);
            _mediaController.SetMediaPlayer(_videoView);
            _videoView.SetMediaController(_mediaController);
        }
        else
        {
            _videoView.SetMediaController(null);
            if (_mediaController != null)
            {
                _mediaController.SetMediaPlayer(null);
                _mediaController = null;
            }
        }
    }

    public void UpdateSource()
    {
        _isPrepared = false;
        bool hasSetSource = false;

        if (_video.Source is UriVideoSource)
        {
            string uri = (_video.Source as UriVideoSource).Uri;
            if (!string.IsNullOrWhiteSpace(uri))
            {
                _mpvClient.Command("loadfile", uri);

                hasSetSource = true;
            }
        }
        else if (_video.Source is FileVideoSource)
        {
            string filename = (_video.Source as FileVideoSource).File;
            if (!string.IsNullOrWhiteSpace(filename))
            {
                _videoView.SetVideoPath(filename);
                hasSetSource = true;
            }
        }
        else if (_video.Source is ResourceVideoSource)
        {
            string package = Context.PackageName;
            string path = (_video.Source as ResourceVideoSource).Path;
            if (!string.IsNullOrWhiteSpace(path))
            {
                string assetFilePath = "content://" + package + "/" + path;
                _videoView.SetVideoPath(assetFilePath);
                hasSetSource = true;
            }
        }

        if (hasSetSource && _video.AutoPlay)
        {
            _videoView.Start();
        }
    }

    public void UpdateIsLooping()
    {
        if (_video.IsLooping)
        {
            _videoView.SetOnPreparedListener(this);
        }
        else
        {
            _videoView.SetOnPreparedListener(null);
        }
    }

    public void UpdatePosition()
    {
        if (Math.Abs(_videoView.CurrentPosition - _video.Position.TotalMilliseconds) > 1000)
        {
            _videoView.SeekTo((int)_video.Position.TotalMilliseconds);
        }
    }

    public void UpdateStatus()
    {
        VideoStatus status = VideoStatus.NotReady;

        if (_isPrepared)
        {
            status = _videoView.IsPlaying ? VideoStatus.Playing : VideoStatus.Paused;
        }

        ((IVideoController)_video).Status = status;

        // Set Position property
        TimeSpan timeSpan = TimeSpan.FromMilliseconds(_videoView.CurrentPosition);
        _video.Position = timeSpan;
    }

    public void PlayRequested(TimeSpan position)
    {
        _videoView.Start();
        System.Diagnostics.Debug.WriteLine(
            $"Video playback from {position.Hours:X2}:{position.Minutes:X2}:{position.Seconds:X2}."
        );
    }

    public void PauseRequested(TimeSpan position)
    {
        _videoView.Pause();
        System.Diagnostics.Debug.WriteLine(
            $"Video paused at {position.Hours:X2}:{position.Minutes:X2}:{position.Seconds:X2}."
        );
    }

    public void StopRequested(TimeSpan position)
    {
        // Stops and releases the media player
        _videoView.StopPlayback();
        System.Diagnostics.Debug.WriteLine(
            $"Video stopped at {position.Hours:X2}:{position.Minutes:X2}:{position.Seconds:X2}."
        );

        // Ensure the video can be played again
        _videoView.Resume();
    }

    void OnVideoViewPrepared(object sender, EventArgs args)
    {
        _isPrepared = true;
        ((IVideoController)_video).Duration = TimeSpan.FromMilliseconds(_videoView.Duration);
    }

    public void OnPrepared(MediaPlayer mp)
    {
        mp.Looping = _video.IsLooping;
    }
}
