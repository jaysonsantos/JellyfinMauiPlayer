#nullable enable
using Microsoft.Maui.Handlers;
using Mpv.Maui.Controls;
#if ANDROID
using Mpv.Maui.Platforms.Android;
#elif IOS || MACCATALYST
using Mpv.Maui.Platforms.MaciOS;
#elif WINDOWS
using Mpv.Maui.Platforms.Windows;
#endif

namespace Mpv.Maui.Handlers
{
    public partial class VideoHandler : ViewHandler<Video, MauiVideoPlayer>
    {
        public static IPropertyMapper<Video, VideoHandler> PropertyMapper = new PropertyMapper<
            Video,
            VideoHandler
        >(ViewMapper)
        {
            [nameof(Video.AreTransportControlsEnabled)] = MapAreTransportControlsEnabled,
            [nameof(Video.Source)] = MapSource,
            [nameof(Video.IsLooping)] = MapIsLooping,
            [nameof(Video.Position)] = MapPosition,
        };

        public static CommandMapper<Video, VideoHandler> CommandMapper = new(ViewCommandMapper)
        {
            [nameof(Video.UpdateStatus)] = MapUpdateStatus,
            [nameof(Video.UpdateSize)] = MapUpdateSize,
            [nameof(Video.PlayRequested)] = MapPlayRequested,
            [nameof(Video.PauseRequested)] = MapPauseRequested,
            [nameof(Video.StopRequested)] = MapStopRequested,
            [nameof(Video.SeekRequested)] = MapSeekRequested,
        };

        private static void MapSeekRequested(VideoHandler handler, Video video, object? args)
        {
            if (args is VideoPositionEventArgs { Position: { } position })
            {
                handler.PlatformView?.SeekRequested(position);
            }
        }

        public VideoHandler()
            : base(PropertyMapper, CommandMapper) { }

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

        public static void MapUpdateSize(VideoHandler handler, Video video, object? args)
        {
            handler.PlatformView?.UpdateSize();
        }

        public static void MapPlayRequested(VideoHandler handler, Video video, object? args)
        {
            TimeSpan? position = (args as VideoPositionEventArgs)?.Position;
            handler.PlatformView?.PlayRequested(position);
        }

        public static void MapPauseRequested(VideoHandler handler, Video video, object? args)
        {
            if (args is not VideoPositionEventArgs { Position: { } position })
                return;

            handler.PlatformView?.PauseRequested(position);
        }

        public static void MapStopRequested(VideoHandler handler, Video video, object? args)
        {
            if (args is not VideoPositionEventArgs { Position: { } position })
                return;

            handler.PlatformView?.StopRequested(position);
        }
    }
}
