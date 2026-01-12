using Android.Graphics;
using Android.Views;

namespace Mpv.Maui.Platforms.Android;

class SurfaceHolderCallback(Action<ISurfaceHolder?> callback)
    : Java.Lang.Object,
        ISurfaceHolderCallback
{
    public void SurfaceChanged(ISurfaceHolder holder, Format format, int width, int height)
    {
        callback(holder);
    }

    public void SurfaceCreated(ISurfaceHolder holder)
    {
        // callback(holder);
    }

    public void SurfaceDestroyed(ISurfaceHolder holder)
    {
        callback(null);
    }
}
