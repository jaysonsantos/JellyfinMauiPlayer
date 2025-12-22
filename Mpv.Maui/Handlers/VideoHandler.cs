using Microsoft.Extensions.Logging;
using Mpv.Maui.Controls;
using Mpv.Sys;

namespace Mpv.Maui.Handlers
{
    public partial class VideoHandler
    {
        private MpvClient _mpvClient;
        private readonly ILogger<VideoHandler> _logger;

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

        public VideoHandler(MpvClient mpvClient, ILogger<VideoHandler> logger)
            : base(PropertyMapper, CommandMapper)
        {
            _mpvClient = mpvClient;
            _logger = logger;
            _logger.LogInformation($"{nameof(VideoHandler)} Is created");
        }
    }
}
