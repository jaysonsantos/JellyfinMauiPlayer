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

namespace Mpv.Maui.Platforms.MaciOS
{
    public sealed partial class MauiVideoPlayer : MTKView
    {
        private bool _disposed;

        public MauiVideoPlayer(
            Video virtualView,
            MpvClient mpvClient,
            ILogger<MauiVideoPlayer> logger
        )
        {
            BackgroundColor = UIColor.SystemBackground;
            Device = Metal.MTLDevice.SystemDefault;

            _video = virtualView;
            _mpvClient = mpvClient;
            _logger = logger;

            InitializeMpvCommon();

            // Subscribe to events
            _mpvClient.OnVideoReconfigure += OnVideoReconfigure;

            CAMetalLayer layer = new MetalLayer();
            layer.Frame = Frame;
            layer.FramebufferOnly = true;
            Layer.AddSublayer(layer);

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
}
