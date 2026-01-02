using System.Runtime.InteropServices;
using Android.Content;
using Android.Media;
using Android.Views;
using Android.Widget;
using AndroidX.CoordinatorLayout.Widget;
using Java.Interop;
using Java.Util.Concurrent.Atomic;
using Microsoft.Extensions.Logging;
using Mpv.Maui.Controls;
using Mpv.Sys;
using Mpv.Sys.Internal;
using Color = Android.Graphics.Color;

namespace Mpv.Maui.Platforms.Android;

public partial class MauiVideoPlayer : CoordinatorLayout, MediaPlayer.IOnPreparedListener
{
    readonly Context _context;

    private static readonly AtomicBoolean JvmSet = new(false);

    public MauiVideoPlayer(
        Context context,
        Video video,
        MpvClient mpvClient,
        ILogger<MauiVideoPlayer> logger
    )
        : base(context)
    {
        EnsureJvmIsSet();
        _context = context;
        _video = video;
        _mpvClient = mpvClient;
        _logger = logger;

        InitializeMpvCommon();

        _mpvClient.SetOption("vo", "gpu-next");
        _mpvClient.SetOption("hwdec", "mediacodec");
        _mpvClient.SetOption("gpu-context", "android");
        var surface = new SurfaceView(_context);

        // TODO: Get the right size for pointer on platform
        var ptr = Marshal.AllocHGlobal(8);
        Marshal.WriteInt64(ptr, surface.Holder!.Surface!.Handle.ToInt64());

        _mpvClient.Initialize();
        _mpvClient.SetOption("wid", 4, ptr);
        Marshal.FreeHGlobal(ptr);

        _mpvClient.Initialize();

        SetBackgroundColor(Color.Black);

        // Create a RelativeLayout for sizing the video
        RelativeLayout relativeLayout = new(_context)
        {
            LayoutParameters = new LayoutParams(LayoutParams.MatchParent, LayoutParams.MatchParent)
            {
                Gravity = (int)GravityFlags.Center,
            },
        };

        // Add to the layouts
        relativeLayout.AddView(surface);
        AddView(relativeLayout);
    }

    private partial void UpdateSourcePlatform()
    {
        if (_video == null)
            return;

        if (_video.Source is FileVideoSource fileSource)
        {
            string filename = fileSource.File;
            if (!string.IsNullOrWhiteSpace(filename))
            {
                _mpvClient.Command("loadfile", filename);
            }
        }
        else if (_video.Source is ResourceVideoSource resourceSource)
        {
            string package = _context.PackageName!;
            string path = resourceSource.Path;
            if (!string.IsNullOrWhiteSpace(path))
            {
                string assetFilePath = "content://" + package + "/" + path;
                _mpvClient.Command("loadfile", assetFilePath);
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _video = null;
            DisposeMpvCommon();
            _mpvClient.Dispose();
        }

        base.Dispose(disposing);
    }

    public void OnPrepared(MediaPlayer? mp)
    {
        if (mp != null && _video != null)
        {
            mp.Looping = _video.IsLooping;
        }
    }

    private static void EnsureJvmIsSet()
    {
        if (JvmSet.Get())
            return;

        var jvm = JniEnvironment.Runtime.InvocationPointer;
        var returnCode = FfmpegLibs.SetJavaVm(jvm, IntPtr.Zero);
        if (returnCode != 0)
        {
            var errorMsg = Marshal.AllocHGlobal(1000);
            var error = FfmpegLibs.MakeErrorString(returnCode, errorMsg, 1000);
            if (error != 0)
                throw new Exception(
                    $"Failed to make error string for FFmpeg Java VM set failure: {error} original error {returnCode}"
                );
            var msg = Marshal.PtrToStringAnsi(errorMsg);
            Marshal.FreeHGlobal(errorMsg);
            throw new Exception($"Failed to set Java VM for FFmpeg: {returnCode} {msg}");
        }

        JvmSet.Set(true);
    }
}
