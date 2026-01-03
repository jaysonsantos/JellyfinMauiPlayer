using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;
using Mpv.Maui.Controls;
using Mpv.Maui.Platforms.Android;
using Mpv.Sys;

namespace Mpv.Maui.Handlers;

public partial class VideoHandler
{
    protected override MauiVideoPlayer CreatePlatformView()
    {
        // Retrieve dependencies from MauiContext.Services instead of constructor injection.
        // MauiContext is not initialized during handler construction on Android, so we must
        // access the service provider here in CreatePlatformView() where MauiContext is available.
        var logger = this.GetRequiredService<ILogger<MauiVideoPlayer>>();
        var mpvClient = this.GetRequiredService<MpvClient>();
        return new(Context, VirtualView, mpvClient, logger);
    }

    protected override void ConnectHandler(MauiVideoPlayer platformView)
    {
        base.ConnectHandler(platformView);

        // Update source after handler is connected, in case it was set before handler creation
        if (VirtualView?.Source != null)
        {
            platformView.UpdateSource(VirtualView);
        }
    }

    protected override void DisconnectHandler(MauiVideoPlayer platformView)
    {
        // Dispose the virtual view first (stops timer and clears events)
        VirtualView?.Dispose();

        // Dispose the platform view
        platformView.Dispose();

        base.DisconnectHandler(platformView);
    }
}
