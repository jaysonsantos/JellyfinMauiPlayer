using Mpv.Maui.Controls;

namespace Mpv.Maui.Handlers
{
    public partial class VideoHandler
    {
        // NOTE: MauiContext is not initialized during handler construction on Android.
        // Dependencies must be retrieved from MauiContext.Services in CreatePlatformView() instead of constructor injection.
        // See platform-specific implementations (VideoHandler.android.cs, VideoHandler.macios.cs, VideoHandler.windows.cs).

        private static readonly IPropertyMapper<Video, VideoHandler> PropertyMapper =
            new PropertyMapper<Video, VideoHandler>(ViewMapper)
            {
                [nameof(Video.AreTransportControlsEnabled)] = MapAreTransportControlsEnabled,
                [nameof(Video.Source)] = MapSource,
                [nameof(Video.IsLooping)] = MapIsLooping,
                [nameof(Video.Position)] = MapPosition,
            };

        private static readonly CommandMapper<Video, VideoHandler> CommandMapper = new(
            ViewCommandMapper
        )
        {
            [nameof(Video.UpdateStatus)] = MapUpdateStatus,
            [nameof(Video.PlayRequested)] = MapPlayRequested,
            [nameof(Video.PauseRequested)] = MapPauseRequested,
            [nameof(Video.StopRequested)] = MapStopRequested,
        };

        public VideoHandler()
            : base(PropertyMapper, CommandMapper) { }
    }
}
