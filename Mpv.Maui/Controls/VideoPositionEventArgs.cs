namespace Mpv.Maui.Controls;

public class VideoPositionEventArgs(TimeSpan? position = null) : EventArgs
{
    public TimeSpan? Position { get; } = position;
}
