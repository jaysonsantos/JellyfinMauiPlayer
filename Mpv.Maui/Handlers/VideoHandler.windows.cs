#nullable enable
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;
using Mpv.Maui.Controls;
using Mpv.Maui.Platforms.Windows;

namespace Mpv.Maui.Handlers
{
    public partial class VideoHandler : ViewHandler<Video, MauiVideoPlayer>
    {
        protected override MauiVideoPlayer CreatePlatformView()
        {
            var logger = MauiContext!.Services.GetRequiredService<ILogger<MauiVideoPlayer>>();
            return new(VirtualView, _mpvClient, logger);
        }

        protected override void ConnectHandler(MauiVideoPlayer platformView)
        {
            base.ConnectHandler(platformView);

            // Perform any control setup here
        }

        protected override void DisconnectHandler(MauiVideoPlayer platformView)
        {
            platformView.Dispose();
            base.DisconnectHandler(platformView);
        }

        public static void MapAreTransportControlsEnabled(VideoHandler handler, Video video)
        {
            handler.PlatformView?.UpdateTransportControlsEnabled();
        }

        public static void MapSource(VideoHandler handler, Video video)
        {
            handler.PlatformView?.UpdateSource();
        }

        public static void MapIsLooping(VideoHandler handler, Video video)
        {
            handler.PlatformView?.UpdateIsLooping();
        }

        public static void MapPosition(VideoHandler handler, Video video)
        {
            handler.PlatformView?.UpdatePosition();
        }

        public static void MapUpdateStatus(VideoHandler handler, Video video, object? args)
        {
            handler.PlatformView?.UpdateStatus();
        }

        public static void MapPlayRequested(VideoHandler handler, Video video, object? args)
        {
            if (args is not VideoPositionEventArgs)
                return;

            TimeSpan position = ((VideoPositionEventArgs)args).Position;
            handler.PlatformView?.PlayRequested(position);
        }

        public static void MapPauseRequested(VideoHandler handler, Video video, object? args)
        {
            if (args is not VideoPositionEventArgs)
                return;

            TimeSpan position = ((VideoPositionEventArgs)args).Position;
            handler.PlatformView?.PauseRequested(position);
        }

        public static void MapStopRequested(VideoHandler handler, Video video, object? args)
        {
            if (args is not VideoPositionEventArgs)
                return;

            TimeSpan position = ((VideoPositionEventArgs)args).Position;
            handler.PlatformView?.StopRequested(position);
        }
    }
}
