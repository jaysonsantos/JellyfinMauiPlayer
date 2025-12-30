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

    public partial class MauiVideoPlayer : Grid, IDisposable
    {
        bool _isMediaPlayerAttached;

        public MauiVideoPlayer(Video video, MpvClient mpvClient, ILogger<MauiVideoPlayer> logger)
        {
            SwapChainPanel swapChainPanel = new();

            _video = video;
            _mpvClient = mpvClient;
            _logger = logger;

            InitializeMpvCommon();

            // _mpvClient.SetOption("keep-open", "always");
            _mpvClient.SetOption("vo", "gpu-next");
            _mpvClient.SetOption("gpu-context", "d3d11");
            _mpvClient.SetOption("d3d11-output-mode", "composition");

            _mpvClient.SetOption("d3d11-composition-size", "320x160");
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

        private partial void UpdateSourcePlatform()
        {
            if (_video == null)
                return;
            if (_video.Source is FileVideoSource fileSource)
            {
                string filename = fileSource.File;
                if (!string.IsNullOrWhiteSpace(filename))
                {
                    StorageFile
                        .GetFileFromPathAsync(filename)
                        .AsTask()
                        .ContinueWith(t =>
                        {
                            if (t.IsCompletedSuccessfully)
                            {
                                _mpvClient.Command("loadfile", filename);
                            }
                            else
                            {
                                _logger.LogError(
                                    t.Exception,
                                    "Failed to load file {Filename}",
                                    filename
                                );
                            }
                        });
                }
            }
            else if (_video.Source is ResourceVideoSource resourceSource)
            {
                string path = "ms-appx:///" + resourceSource.Path;
                if (!string.IsNullOrWhiteSpace(path))
                {
                    _mpvClient.Command("loadfile", path);
                }
            }
        }

        public void Dispose()
        {
            _mpvClient.Dispose();
        }
    }
}
