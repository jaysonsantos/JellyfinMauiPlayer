using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;
using Mpv.Maui.Controls;
using Mpv.Maui.Platforms.Windows;
using Mpv.Sys;

namespace Mpv.Maui.Handlers
{
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
    }
}
