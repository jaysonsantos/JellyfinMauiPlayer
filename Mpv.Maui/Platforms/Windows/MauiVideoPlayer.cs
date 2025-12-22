using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Mpv.Maui.Controls;
using Mpv.Sys;
using Mpv.Sys.Internal;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using WinRT;
using Application = Microsoft.Maui.Controls.Application;
using Grid = Microsoft.UI.Xaml.Controls.Grid;

namespace Mpv.Maui.Platforms.Windows
{
    [
        ComImport,
        Guid("63aad0b8-7c24-40ff-85a8-640d944cc325"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown)
    ]
    interface ISwapChainPanelNative
    {
        void SetSwapChain(IntPtr swapChain);
    }

    public class MauiVideoPlayer : Grid, IDisposable
    {
        readonly Video _video;
        private readonly MpvClient _mpvClient;
        private readonly ILogger<MauiVideoPlayer> _logger;
        bool _isMediaPlayerAttached;

        public MauiVideoPlayer(Video video, MpvClient mpvClient, ILogger<MauiVideoPlayer> logger)
        {
            SwapChainPanel swapChainPanel = new();

            _video = video;
            _mpvClient = mpvClient;
            _logger = logger;
            // _mpvClient.SetOption("keep-open", "always");
            _mpvClient.SetOption("vo", "gpu-next");
            _mpvClient.SetOption("gpu-context", "d3d11");
            _mpvClient.SetOption("d3d11-output-mode", "composition");

            _mpvClient.SetOption("d3d11-composition-size", "320x160");
            _mpvClient.OnLog += LogLines;
            _mpvClient.OnVideoReconfigure += (s, e) =>
            {
                var swapChain = _mpvClient.GetPropertyPtr("display-swapchain");
                var native = swapChainPanel.As<ISwapChainPanelNative>();

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        native.SetSwapChain(swapChain);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError("Failed to set swap chain" + ex.Message);
                    }
                });

                _logger.LogInformation("Video reconfigured.");
            };

            _mpvClient.Initialize();
            SizeChanged += (_, e) =>
            {
                var s = e.NewSize;
                _mpvClient.SetOption("d3d11-composition-size", $"{s.Width}x{s.Height}");
            };
            this.Children.Add(swapChainPanel);
        }

        private void LogLines(object? sender, MpvLogMessage log)
        {
            _logger.LogWarning($"{log.Level} {log.Prefix} {log.Text}");
        }

        public void Dispose()
        {
            _mpvClient.Dispose();
        }

        public void UpdateTransportControlsEnabled()
        {
            return;
        }

        public async void UpdateSource()
        {
            bool hasSetSource = false;

            if (_video.Source is UriVideoSource)
            {
                string uri = (_video.Source as UriVideoSource).Uri;
                if (!string.IsNullOrWhiteSpace(uri))
                {
                    _mpvClient.Command("loadfile", uri);

                    // var swapChain = _mpvClient.GetPropertyPtr("display-swapchain");
                    //_mediaPlayerElement.Source = MediaSource.CreateFromUri(new Uri(uri));
                    hasSetSource = true;
                }
            }
            // else if (_video.Source is FileVideoSource)
            // {
            //     string filename = (_video.Source as FileVideoSource).File;
            //     if (!string.IsNullOrWhiteSpace(filename))
            //     {
            //         StorageFile storageFile = await StorageFile.GetFileFromPathAsync(filename);
            //         _mediaPlayerElement.Source = MediaSource.CreateFromStorageFile(storageFile);
            //         hasSetSource = true;
            //     }
            // }
            // else if (_video.Source is ResourceVideoSource)
            // {
            //     string path = "ms-appx:///" + (_video.Source as ResourceVideoSource).Path;
            //     if (!string.IsNullOrWhiteSpace(path))
            //     {
            //         _mediaPlayerElement.Source = MediaSource.CreateFromUri(new Uri(path));
            //         hasSetSource = true;
            //     }
            // }
            //
            // if (hasSetSource && !_isMediaPlayerAttached)
            // {
            //     //_isMediaPlayerAttached = true;
            //     //_mediaPlayerElement.MediaPlayer.MediaOpened += OnMediaPlayerMediaOpened;
            // }
            //
            // if (hasSetSource && _video.AutoPlay)
            // {
            //     // _mediaPlayerElement.AutoPlay = true;
            // }
        }

        public void UpdateIsLooping()
        {
            // if (_isMediaPlayerAttached)
            //     _mediaPlayerElement.MediaPlayer.IsLoopingEnabled = _video.IsLooping;
        }

        public void UpdatePosition()
        {
            // if (_isMediaPlayerAttached)
            // {
            //     if (Math.Abs((_mediaPlayerElement.MediaPlayer.Position - _video.Position).TotalSeconds) > 1)
            //     {
            //         _mediaPlayerElement.MediaPlayer.Position = _video.Position;
            //     }
            // }
        }

        public void UpdateStatus()
        {
            // if (_isMediaPlayerAttached)
            // {
            //     VideoStatus status = VideoStatus.NotReady;
            //
            //     switch (_mediaPlayerElement.MediaPlayer.CurrentState)
            //     {
            //         case MediaPlayerState.Playing:
            //             status = VideoStatus.Playing;
            //             break;
            //         case MediaPlayerState.Paused:
            //         case MediaPlayerState.Stopped:
            //             status = VideoStatus.Paused;
            //             break;
            //     }
            //
            //     ((IVideoController)_video).Status = status;
            //     _video.Position = _mediaPlayerElement.MediaPlayer.Position;
            // }
        }

        public void PlayRequested(TimeSpan position)
        {
            // if (_isMediaPlayerAttached)
            // {
            //     _mediaPlayerElement.MediaPlayer.Play();
            //     System.Diagnostics.Debug.WriteLine(
            //         $"Video playback from {position.Hours:X2}:{position.Minutes:X2}:{position.Seconds:X2}.");
            // }
        }

        public void PauseRequested(TimeSpan position)
        {
            // if (_isMediaPlayerAttached)
            // {
            //     _mediaPlayerElement.MediaPlayer.Pause();
            //     System.Diagnostics.Debug.WriteLine(
            //         $"Video paused at {position.Hours:X2}:{position.Minutes:X2}:{position.Seconds:X2}.");
            // }
        }

        public void StopRequested(TimeSpan position)
        {
            // if (_isMediaPlayerAttached)
            // {
            //     // There's no Stop method so pause the video and reset its position
            //     _mediaPlayerElement.MediaPlayer.Pause();
            //     _mediaPlayerElement.MediaPlayer.Position = TimeSpan.Zero;
            //     System.Diagnostics.Debug.WriteLine(
            //         $"Video stopped at {position.Hours:X2}:{position.Minutes:X2}:{position.Seconds:X2}.");
            // }
        }

        void OnMediaPlayerMediaOpened(MediaPlayer sender, object args)
        {
            return;
            // MainThread.BeginInvokeOnMainThread(() =>
            // {
            //     ((IVideoController)_video).Duration = _mediaPlayerElement.MediaPlayer.NaturalDuration;
            // });
        }
    }
}
