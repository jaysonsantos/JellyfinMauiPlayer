using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Mpv.Maui.Controls;
using Mpv.Sys;
using Windows.Storage;
using WinRT;
using Grid = Microsoft.UI.Xaml.Controls.Grid;

namespace Mpv.Maui.Platforms.Windows;

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
    private bool _disposed;
    private EventHandler? _videoReconfigureHandler;
    private Microsoft.UI.Xaml.SizeChangedEventHandler? _sizeChangedHandler;

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

        // Store handler reference for proper cleanup
        _videoReconfigureHandler = (s, e) =>
        {
            IntPtr swapChain;
            try
            {
                swapChain = _mpvClient.GetPropertyPtr("display-swapchain");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get swapchain from mpv");
                return;
            }

            var native = swapChainPanel.As<ISwapChainPanelNative>();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    native.SetSwapChain(swapChain);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to set swap chain");
                }
            });

            _logger.LogInformation("Video reconfigured.");
        };
        _mpvClient.OnVideoReconfigure += _videoReconfigureHandler;

        _mpvClient.Initialize();

        // Store handler reference for proper cleanup
        _sizeChangedHandler = (_, e) =>
        {
            var s = e.NewSize;
            _mpvClient.SetOption("d3d11-composition-size", $"{s.Width}x{s.Height}");
        };
        SizeChanged += _sizeChangedHandler;

        Children.Add(swapChainPanel);
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
                _ = StorageFile
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

    private partial void UpdateSizePlatform()
    {
        // Update d3d11 composition size based on current grid size
        var width = ActualWidth;
        var height = ActualHeight;

        if (width > 0 && height > 0)
        {
            _mpvClient.SetOption("d3d11-composition-size", $"{width}x{height}");
            _logger.LogDebug(
                "Updated Windows d3d11 composition size to {Width}x{Height}",
                width,
                height
            );
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (!disposing)
            return;

        // Unsubscribe from common mpv events
        DisposeMpvCommon();

        // Unsubscribe from platform-specific events
        if (_videoReconfigureHandler != null)
        {
            _mpvClient.OnVideoReconfigure -= _videoReconfigureHandler;
            _videoReconfigureHandler = null;
        }

        if (_sizeChangedHandler != null)
        {
            SizeChanged -= _sizeChangedHandler;
            _sizeChangedHandler = null;
        }

        // Do not dispose the mpv client because it is a singleton
        // _mpvClient.Dispose();
    }
}
