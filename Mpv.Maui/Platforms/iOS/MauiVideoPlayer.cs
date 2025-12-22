using System.Globalization;
using System.Runtime.InteropServices;
using CoreAnimation;
using CoreGraphics;
using MetalKit;
using Microsoft.Extensions.Logging;
using Mpv.Maui.Controls;
using Mpv.Sys;
using Mpv.Sys.Internal;

namespace Mpv.Maui.Platforms.MaciOS
{
    public class MauiVideoPlayer : MTKView
    {
        private readonly Video _video;
        private readonly MpvClient _mpvClient;
        private readonly ILogger<MauiVideoPlayer> _logger;
        private bool _disposed;
        private readonly CAMetalLayer _layer;

        public MauiVideoPlayer(
            Video virtualView,
            MpvClient mpvClient,
            ILogger<MauiVideoPlayer> logger
        )
        {
            _video = virtualView;
            _mpvClient = mpvClient;
            _logger = logger;

            // Subscribe to events
            _mpvClient.OnLog += LogLines;
            _mpvClient.OnVideoReconfigure += OnVideoReconfigure;
            Device = Metal.MTLDevice.SystemDefault;

            _layer = new MetalLayer();
            _layer.Frame = Frame;
            _layer.FramebufferOnly = true;
            Layer.AddSublayer(_layer);

            // This will break on 32-bit systems
            var ptr = Marshal.AllocHGlobal(sizeof(Int64));
            Marshal.WriteIntPtr(ptr, Layer.Handle.Handle);

            // Initialize mpv client
            _mpvClient.SetOption("input-media-keys", "yes");

            _mpvClient.SetOption("vo", "gpu-next");
            _mpvClient.SetOption("hwdec", "videotoolbox");
            _mpvClient.SetOption("gpu-context", "moltenvk");

            _mpvClient.SetOption("wid", 4, ptr);
            _mpvClient.Initialize();
        }

        private void LogLines(object? sender, MpvLogMessage log)
        {
            Console.WriteLine($"[{log.Level}] {log.Prefix}: {log.Text}");
            _logger.LogWarning("{Level} {Prefix} {Text}", log.Level, log.Prefix, log.Text);
        }

        private void OnVideoReconfigure(object? sender, EventArgs e)
        {
            _logger.LogInformation("Video reconfigured.");
        }

        public void UpdateTransportControlsEnabled()
        {
            // Transport controls are handled externally in MAUI
        }

        public void UpdateSource()
        {
            if (_video.Source is not UriVideoSource uriSource)
            {
                return;
            }

            string uri = uriSource.Uri;
            if (!string.IsNullOrWhiteSpace(uri))
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _mpvClient.Command("loadfile", uri);
                });
            }
        }

        public void UpdateIsLooping()
        {
            // Looping can be set via mpv property "loop-file"
            _mpvClient.Command("set", "loop-file", _video.IsLooping ? "inf" : "no");
        }

        public void UpdatePosition()
        {
            // Position updates are handled via mpv properties
        }

        public void UpdateStatus()
        {
            // Status updates are handled via mpv events
        }

        public void PlayRequested(TimeSpan position)
        {
            _mpvClient.Command(
                "seek",
                position.TotalSeconds.ToString(CultureInfo.InvariantCulture),
                "absolute"
            );
            _mpvClient.Command("set", "pause", "no");
            _logger.LogDebug("Video playback from {Position}.", position);
        }

        public void PauseRequested(TimeSpan position)
        {
            _mpvClient.Command("set", "pause", "yes");
            _logger.LogDebug("Video paused at {Position}.", position);
        }

        public void StopRequested(TimeSpan position)
        {
            _mpvClient.Command("stop");
            _logger.LogDebug("Video stopped at {Position}.", position);
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
                _mpvClient.OnLog -= LogLines;
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
