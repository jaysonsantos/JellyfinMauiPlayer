using Mpv.Maui.Controls;
using Mpv.Maui.Handlers;
using Mpv.Sys;

namespace Mpv.Maui;

public static class Extension
{
    public static MauiAppBuilder UseMpv(this MauiAppBuilder builder)
    {
        builder.Services.AddSingleton<MpvClient>();
        builder.ConfigureMauiHandlers(handlers =>
        {
            handlers.AddHandler<Video, VideoHandler>();
        });

        return builder;
    }
}
