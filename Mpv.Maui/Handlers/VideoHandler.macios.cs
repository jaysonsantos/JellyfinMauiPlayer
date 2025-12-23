using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;
using Mpv.Maui.Controls;
using Mpv.Maui.Platforms.MaciOS;
using Mpv.Sys;

namespace Mpv.Maui.Handlers;

public partial class VideoHandler : ViewHandler<Video, MauiVideoPlayer>
{
    protected override MauiVideoPlayer CreatePlatformView()
    {
        // Retrieve dependencies from MauiContext.Services instead of constructor injection.
        // MauiContext is not initialized during handler construction on Android, so we must
        // access the service provider here in CreatePlatformView() where MauiContext is available.
        var logger = MauiContext!.Services.GetRequiredService<ILogger<MauiVideoPlayer>>();
        var mpvClient = MauiContext!.Services.GetRequiredService<MpvClient>();
        return new(VirtualView, mpvClient, logger);
    }

    protected override void DisconnectHandler(MauiVideoPlayer platformView)
    {
        platformView.Dispose();
        base.DisconnectHandler(platformView);
    }

    public static void MapAreTransportControlsEnabled(VideoHandler? handler, Video video)
    {
        handler?.PlatformView.UpdateTransportControlsEnabled();
    }

    public static void MapSource(VideoHandler? handler, Video video)
    {
        handler?.PlatformView.UpdateSource();
    }

    public static void MapIsLooping(VideoHandler handler, Video video)
    {
        handler.PlatformView.UpdateIsLooping();
    }

    public static void MapPosition(VideoHandler? handler, Video video)
    {
        handler?.PlatformView.UpdatePosition();
    }

    public static void MapUpdateStatus(VideoHandler handler, Video video, object? args)
    {
        handler.PlatformView.UpdateStatus();
    }

    public static void MapPlayRequested(VideoHandler handler, Video video, object? args)
    {
        if (args is not VideoPositionEventArgs eventArgs)
            return;

        TimeSpan position = eventArgs.Position;
        handler.PlatformView.PlayRequested(position);
    }

    public static void MapPauseRequested(VideoHandler handler, Video video, object? args)
    {
        if (args is not VideoPositionEventArgs eventArgs)
            return;

        TimeSpan position = eventArgs.Position;
        handler.PlatformView.PauseRequested(position);
    }

    public static void MapStopRequested(VideoHandler handler, Video video, object? args)
    {
        if (args is not VideoPositionEventArgs eventArgs)
            return;

        TimeSpan position = eventArgs.Position;
        handler.PlatformView.StopRequested(position);
    }
}
