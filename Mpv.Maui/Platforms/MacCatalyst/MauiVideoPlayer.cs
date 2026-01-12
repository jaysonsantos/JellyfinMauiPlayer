using System.Globalization;
using System.Runtime.InteropServices;
using CoreAnimation;
using CoreGraphics;
using MetalKit;
using Microsoft.Extensions.Logging;
using Mpv.Maui.Controls;
using Mpv.Sys;
using Mpv.Sys.Internal;
using UIKit;

namespace Mpv.Maui.Platforms.MaciOS;

public sealed partial class MauiVideoPlayer : MTKView
{
    private bool _disposed;
    private readonly CAMetalLayer _layer;

    public MauiVideoPlayer(Video virtualView, MpvClient mpvClient, ILogger<MauiVideoPlayer> logger)
    {
        BackgroundColor = UIColor.SystemBackground;
        Device = Metal.MTLDevice.SystemDefault;

        _video = virtualView;
        _mpvClient = mpvClient;
        _logger = logger;

        InitializeMpvCommon();

        // Subscribe to events
        _mpvClient.OnVideoReconfigure += OnVideoReconfigure;

        _layer = new MetalLayer();
        _layer.Frame = Frame;
        _layer.FramebufferOnly = true;
        Layer.AddSublayer(_layer);

        // This will break on 32-bit systems
        var ptr = Marshal.AllocHGlobal(sizeof(Int64));
        Marshal.WriteIntPtr(ptr, Layer.Handle.Handle);

        // Initialize mpv client
        _mpvClient.SetOption("vo", "gpu-next");
        // _mpvClient.SetOption("hwdec", "videotoolbox");
        _mpvClient.SetOption("gpu-context", "moltenvk");

        _mpvClient.SetOption("wid", 4, ptr);
        _mpvClient.Initialize();
    }

    private void OnVideoReconfigure(object? sender, EventArgs e)
    {
        _logger.LogInformation("Video reconfigured.");
    }

    private partial void UpdateSizePlatform()
    {
        if (_layer == null)
            return;

        // Update the Metal layer frame to match the view bounds
        _layer.Frame = Bounds;
        _logger.LogDebug(
            "Updated Metal layer size to {Width}x{Height}",
            Bounds.Width,
            Bounds.Height
        );
    }

    public override void LayoutSubviews()
    {
        base.LayoutSubviews();

        // Update Metal layer frame when view layout changes
        if (_layer != null)
        {
            _layer.Frame = Bounds;
        }
    }

    private partial void UpdateSourcePlatform()
    {
        if (_video == null)
            return;

        if (_video.Source is FileVideoSource fileSource)
        {
            if (!string.IsNullOrWhiteSpace(fileSource.File))
            {
                _mpvClient.Command("loadfile", fileSource.File);
            }
        }
        else if (_video.Source is ResourceVideoSource resourceSource)
        {
            if (!string.IsNullOrWhiteSpace(resourceSource.Path))
            {
                string? path = Foundation.NSBundle.MainBundle.PathForResource(
                    resourceSource.Path,
                    null
                );
                if (path != null)
                {
                    _mpvClient.Command("loadfile", path);
                }
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (disposing)
        {
            DisposeMpvCommon();
            _mpvClient.OnVideoReconfigure -= OnVideoReconfigure;
            // Do not dispose the mpv client because it is a singleton
            // _mpvClient.Dispose();
        }

        base.Dispose(disposing);
    }
}

public class MetalLayer : CAMetalLayer
{
    public override CGSize DrawableSize
    {
        get;
        set
        {
            if (value.Width > 1 && value.Height > 1)
            {
                field = value;
            }
        }
    }
}
